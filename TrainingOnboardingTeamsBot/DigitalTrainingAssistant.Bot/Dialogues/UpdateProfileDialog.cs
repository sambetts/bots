using DigitalTrainingAssistant.Bot.Cards;
using DigitalTrainingAssistant.Bot.Helpers;
using DigitalTrainingAssistant.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
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
                SaveProfileAsync,
                LoginStepAsync,
                TryAgainStepAsync
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

            // Create the text prompt
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

            // Start OAuth login
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var courseAttendance = (CourseAttendance)stepContext.Options;

            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse?.Token != null)
            {

                var graphClient = new Microsoft.Graph.GraphServiceClient(new Microsoft.Graph.DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", tokenResponse.Token);
                        await Task.FromResult(0);
                    })
                );

                var adaptiveCard = new AttendeeFixedQuestionsPublicationCard(courseAttendance).GetChatMessageAttachment();
                adaptiveCard.Id = Guid.NewGuid().ToString();
                var msg = new Microsoft.Graph.ChatMessage 
                { 
                    Attachments = new List<Microsoft.Graph.ChatMessageAttachment>() { adaptiveCard },
                    Body = new Microsoft.Graph.ItemBody 
                    {
                        ContentType = Microsoft.Graph.BodyType.Html,
                        Content = $"<attachment id=\"{adaptiveCard.Id}\"></attachment>"
                    },
                    Subject = "Welcome New Trainee!"
                };

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                };
                var payloadin = System.Text.Json.JsonSerializer.Serialize(msg, options);


                await graphClient
                    .Teams[courseAttendance.ParentCourse.TeamId]
                    .Channels[courseAttendance.ParentCourse.TeamChannelId]
                    .Messages.Request().AddAsync(msg);

                await stepContext.Context.SendActivityAsync($"I've posted your introduction to the course Team!");

                // We're done either way
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
            else
            {

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Login was not successful; I didn't get a token to do things I need to with, for some reason."), cancellationToken);

                string msg = $"Try that again?";
                var promptMessage = MessageFactory.Text(msg, msg, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions 
                { 
                    Prompt = promptMessage, Choices = new List<Choice>() {
                        new Choice() { Value = "Yes", Synonyms = new List<string>() { "Yes", "Do it", "Try again" } },
                        new Choice() { Value = "No", Synonyms = new List<string>() { "No", "Stop", "Exit" } }
                    }
                }, cancellationToken);

            }
        }

        private async Task<DialogTurnResult> TryAgainStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var response = (FoundChoice)stepContext.Result;
            if (response.Value == "Yes")
            {
                var courseAttendance = (CourseAttendance)stepContext.Options;

                var token = await AuthHelper.GetToken(stepContext.Context.Activity.Conversation.TenantId, _botConfig.MicrosoftAppId, _botConfig.MicrosoftAppPassword);
                var graphClient = AuthHelper.GetAuthenticatedClient(token);

                var updatedAttendanceInfo = await CourseAttendance.LoadById(graphClient, _botConfig.SharePointSiteId, courseAttendance.ID);

                // Go around again. Pass the last activity in so it can be compared on start of this diag & not used again
                return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), updatedAttendanceInfo, cancellationToken);
            }
            else if (response.Value == "No")
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Ok; I think we're done here then."), cancellationToken);
            }
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }

}
