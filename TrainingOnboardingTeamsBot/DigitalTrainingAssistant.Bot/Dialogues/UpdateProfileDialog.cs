using DigitalTrainingAssistant.Bot.Cards;
using DigitalTrainingAssistant.Bot.Helpers;
using DigitalTrainingAssistant.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
                await _botHelper.HandleCardResponse(stepContext, stepContext.Context.Activity.Value?.ToString(), cancellationToken);
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
                }
                catch (ErrorResponseException ex)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Couldn't update the team - {ex.Message}"), cancellationToken);
                }
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

    }

}
