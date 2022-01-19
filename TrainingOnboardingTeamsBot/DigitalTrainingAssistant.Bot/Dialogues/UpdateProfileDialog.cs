using DigitalTrainingAssistant.Bot.Cards;
using DigitalTrainingAssistant.Bot.Helpers;
using DigitalTrainingAssistant.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        private BotHelper _botHelper;
        public UpdateProfileDialog(BotHelper botHelper, BotConfig botConfig, ILogger<UpdateProfileDialog> logger)
            : base(nameof(UpdateProfileDialog), botConfig.BotOAuthConnectionName)
        {
            Logger = logger;
            _botHelper = botHelper;
            _botConfig = botConfig;

            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = botConfig.BotOAuthConnectionName,
                    Text = "Please Sign In to Office 365 so I can send an introduction card",
                    Title = "Sign In",
                    Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
                }));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
                SaveProfileAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var courseAttendance = (CourseAttendance)stepContext.Options;
            if (courseAttendance == null)
            {
                throw new ArgumentNullException(nameof(courseAttendance));
            }

            // Send the profile input card if not done already
            if (!courseAttendance.IntroductionDone)
            {
                var opts = new PromptOptions
                {
                    Prompt = new Activity
                    {
                        Attachments = new List<Attachment>() { new AttendeeFixedQuestionsInputCard(courseAttendance).GetCard() },
                        Type = ActivityTypes.Message,
                        Text = "waiting for user input...", // You can comment this out if you don't want to display any text. Still works.
                    }
                };
                // Display a Text Prompt and wait for input
                return await stepContext.PromptAsync(nameof(TextPrompt), opts);
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        private async Task<DialogTurnResult> SaveProfileAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
                var updatedAttendenceInfo = await HandleCardResponse(stepContext, stepContext.Context.Activity.Value?.ToString(), cancellationToken, _botConfig);
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
                var message = MessageFactory.Attachment(new AttendeeFixedQuestionsPublicationCard(courseAttendance).GetCard());

                var conversationParameters = new ConversationParameters
                {
                    IsGroup = true,
                    ChannelData = new { channel = new { id = courseAttendance.ParentCourse.TeamChannelId } },
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


        static async Task<CourseAttendance> HandleCardResponse(WaterfallStepContext stepContext, string submitJson, CancellationToken cancellationToken, BotConfig _configuration)
        {
            // Form action
            var action = AdaptiveCardUtils.GetAdaptiveCardAction(submitJson, stepContext.Context.Activity.From.AadObjectId);

            // Figure out what was done
            if (action is IntroduceYourselfResponse)
            {
                var introductionData = (IntroduceYourselfResponse)action;

                var token = await AuthHelper.GetToken(stepContext.Context.Activity.Conversation.TenantId, _configuration.MicrosoftAppId, _configuration.MicrosoftAppPassword);
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


                    // Send back to user for now
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Saved. Now let's introduce you to the Team..."));

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
