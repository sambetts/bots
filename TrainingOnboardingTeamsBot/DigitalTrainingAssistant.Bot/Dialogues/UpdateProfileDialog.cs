using DigitalTrainingAssistant.Bot.Cards;
using DigitalTrainingAssistant.Bot.Helpers;
using DigitalTrainingAssistant.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Dialogues
{
    /// <summary>
    /// User hasn't filled out the personal info for a course. This dialogue gets those details.
    /// </summary>
    public class UpdateProfileDialog : LogoutDialog
    {
        protected readonly ILogger Logger;

        private BotConfig _botConfig;
        private BotActionsHelper _botHelper;
        public UpdateProfileDialog(BotActionsHelper botHelper, BotConfig botConfig, ILogger<UpdateProfileDialog> logger)
            : base(nameof(UpdateProfileDialog), botConfig.BotOAuthConnectionName)
        {
            Logger = logger;
            _botHelper = botHelper;
            _botConfig = botConfig;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                SendLearnerQuestionsAsync,
                SaveProfileAsyncAndConfirmSend,
                ConfirmIntroCardAsync,
                PostTraineeIntroAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> SendLearnerQuestionsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var courseAttendance = (CourseAttendance)stepContext.Options;
            if (courseAttendance == null)
            {
                throw new ArgumentNullException(nameof(courseAttendance));
            }
            if (courseAttendance.IntroductionDone)
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Attachments = new List<Attachment>() { new AttendeeFixedQuestionsInputCard(courseAttendance).GetCardAttachment() },
                    Type = ActivityTypes.Message,
                    Text = "Please fill out all the fields below", 
                }
            };
            // Display a Text Prompt and wait for input
            return await stepContext.PromptAsync(nameof(TextPrompt), opts);
        }

        private async Task<DialogTurnResult> SaveProfileAsyncAndConfirmSend(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Profile saving done in BotHelper.HandleCardResponse
            var courseAttendance = (CourseAttendance)stepContext.Options;
            if (courseAttendance == null)
            {
                throw new ArgumentNullException(nameof(courseAttendance));
            }
            if (!courseAttendance.IntroductionDone)
            {
                // Update questionnaire
                var updatedAttendenceInfo = await HandleProfileUpdateCardResponse(stepContext, stepContext.Context.Activity.Value?.ToString(), cancellationToken, _botConfig);
                if (!courseAttendance.IntroductionDone && updatedAttendenceInfo == null)
                {
                    // We're not sure what the last user response was
                    return await CommonDialogues.ReplyWithNoIdeaAndEndDiag(stepContext, cancellationToken);
                }
                else
                {
                    // Replace dialogue with updated attendance. 
                    return await stepContext.ReplaceDialogAsync(nameof(UpdateProfileDialog), updatedAttendenceInfo, cancellationToken);
                }
            }

            // Send intro preview

            // DUP CLIENT
            var token = await AuthHelper.GetToken(_botConfig.TenantId, _botConfig.MicrosoftAppId, 
                _botConfig.MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            var previewCard = new AttendeeFixedQuestionsPublicationCard(courseAttendance);
            await previewCard.LoadProfileImage(graphClient, stepContext.Context.Activity.From.AadObjectId);

            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(
                        previewCard.GetCardAttachment()
                        ), cancellationToken);

            // Check if user is OK with preview
            return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = stepContext.Context.Activity.CreateReply("Saved your responses. " +
                            "How about we introduce you like this card above to the others in the course?"),
                        Choices = new[] { new Choice { Value = "Send" }, new Choice { Value = "Refresh" }, new Choice { Value = "Don't Send" } }.ToList(),
                        RetryPrompt = stepContext.Context.Activity.CreateReply("Sorry, I did not understand that. Please choose/click on any one of the options displayed in below list to proceed"),
                    });
        }

        private async Task<DialogTurnResult> ConfirmIntroCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Profile saving done in BotHelper.HandleCardResponse
            var courseAttendance = (CourseAttendance)stepContext.Options;
            
            if (courseAttendance == null)
            {
                throw new ArgumentNullException(nameof(courseAttendance));
            }

            var response = (FoundChoice)stepContext.Result;
            if (response.Value == "Don't Send")
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Ok; we'll skip that then, mysterious stranger. " +
                    $"Enjoy the course!"), cancellationToken);

            }
            else if (response.Value == "Refresh")
            {
                // Go around again
                return await stepContext.ReplaceDialogAsync(nameof(UpdateProfileDialog), courseAttendance, cancellationToken);
            }
            else if (response.Value == "Send")
            {
                if (courseAttendance.ParentCourse.HasValidTeamsSettings)
                {
                    var credentials = new MicrosoftAppCredentials(_botConfig.MicrosoftAppId, _botConfig.MicrosoftAppPassword);


                    // DUP CLIENT
                    var token = await AuthHelper.GetToken(_botConfig.TenantId, _botConfig.MicrosoftAppId,
                        _botConfig.MicrosoftAppPassword);
                    var graphClient = AuthHelper.GetAuthenticatedClient(token);

                    var previewCard = new AttendeeFixedQuestionsPublicationCard(courseAttendance);
                    await previewCard.LoadProfileImage(graphClient, stepContext.Context.Activity.From.AadObjectId);

                    var message = MessageFactory.Attachment(previewCard.GetCardAttachment());

                    var conversationParameters = new ConversationParameters
                    {
                        IsGroup = true,
                        ChannelData = new { channel = new { id = courseAttendance.ParentCourse.PostToTeamChannelId } }, 
                        Activity = (Activity)message,
                    };

                    ConversationReference conversationReference = null;

                    var success = false;
                    try
                    {
                        await stepContext.Context.Adapter.CreateConversationAsync(
                            _botConfig.MicrosoftAppId,
                            courseAttendance.ParentCourse.TeamId,
                            stepContext.Context.Activity.ServiceUrl,
                            credentials.OAuthScope,
                            conversationParameters,
                            (t, ct) =>
                            {
                                conversationReference = t.Activity.GetConversationReference();
                                return Task.CompletedTask;
                            },
                            cancellationToken);
                        success = true;
                    }
                    catch (ErrorResponseException ex)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Couldn't update the team - {ex.Message}"), cancellationToken);
                    }

                    if (success)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Done. Thanks for letting us know a bit about yourself!"), cancellationToken);
                    }
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text($"As it turns out, this course has no valid Teams settings right now. Sorry about all that."), cancellationToken);
                }
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


        private async Task<DialogTurnResult> PostTraineeIntroAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Profile saving done in BotHelper.HandleCardResponse
            var courseAttendance = (CourseAttendance)stepContext.Options;
            if (courseAttendance == null)
            {
                throw new ArgumentNullException(nameof(courseAttendance));
            }
            if (!courseAttendance.IntroductionDone)
            {
                // Update questionnaire
                var updatedAttendenceInfo = await HandleProfileUpdateCardResponse(stepContext, stepContext.Context.Activity.Value?.ToString(), cancellationToken, _botConfig);
                if (updatedAttendenceInfo == null)
                {
                    // We're not sure what the last user response was
                    return await CommonDialogues.ReplyWithNoIdeaAndEndDiag(stepContext, cancellationToken);
                }
                else
                {
                    courseAttendance = updatedAttendenceInfo;
                }
            }

            if (courseAttendance.ParentCourse.HasValidTeamsSettings)
            {
                var credentials = new MicrosoftAppCredentials(_botConfig.MicrosoftAppId, _botConfig.MicrosoftAppPassword);
                var message = MessageFactory.Attachment(new AttendeeFixedQuestionsPublicationCard(courseAttendance).GetCardAttachment());

                var conversationParameters = new ConversationParameters
                {
                    IsGroup = true,
                    ChannelData = new { channel = new { id = courseAttendance.ParentCourse.GeneralTeamChannelId } },
                    Activity = (Activity)message,
                };

                ConversationReference conversationReference = null;

                var success = false;
                try
                {
                    await stepContext.Context.Adapter.CreateConversationAsync(
                        _botConfig.MicrosoftAppId,
                        courseAttendance.ParentCourse.TeamId,
                        stepContext.Context.Activity.ServiceUrl,
                        credentials.OAuthScope,
                        conversationParameters,
                        (t, ct) =>
                        {
                            conversationReference = t.Activity.GetConversationReference();
                            return Task.CompletedTask;
                        },
                        cancellationToken);
                    success = true;
                }
                catch (ErrorResponseException ex)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Couldn't update the team - {ex.Message}"), cancellationToken);
                }

                if (success)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Done. Thanks for letting us know a bit about yourself!"), cancellationToken);
                }
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


        async Task<CourseAttendance> HandleProfileUpdateCardResponse(WaterfallStepContext stepContext, string submitJson, CancellationToken cancellationToken, BotConfig _configuration)
        {
            // Form action
            var action = AdaptiveCardUtils.GetAdaptiveCardAction(submitJson, stepContext.Context.Activity.From.AadObjectId);

            // Figure out what was done
            if (action is IntroduceYourselfResponse)
            {
                var introductionData = (IntroduceYourselfResponse)action;

                var token = await AuthHelper.GetToken(_configuration.TenantId, _configuration.MicrosoftAppId, _configuration.MicrosoftAppPassword);
                var graphClient = AuthHelper.GetAuthenticatedClient(token);

                var attendanceInfo = await CourseAttendance.LoadById(graphClient, _configuration.SharePointSiteId, introductionData.SPID);
                if (introductionData.IsValid)
                {
                    // Save intro data
                    attendanceInfo.QACountry = introductionData.Country;
                    attendanceInfo.QAMobilePhoneNumber = introductionData.MobilePhoneNumber;
                    attendanceInfo.QAOrg = introductionData.Org;
                    attendanceInfo.QARole = introductionData.Role;
                    attendanceInfo.QASpareTimeActivities = introductionData.SpareTimeActivities;
                    attendanceInfo.IntroductionDone = true;

#if !DEBUG
                    await attendanceInfo.SaveChanges(graphClient, _configuration.SharePointSiteId);
#endif

                    return attendanceInfo;
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                        $"Oops, that doesn't seem right - check the values & try again?"
                        ), cancellationToken);
                }
            }
            return null;        // Invalid user response. No updated attendence info
        }
    }
}
