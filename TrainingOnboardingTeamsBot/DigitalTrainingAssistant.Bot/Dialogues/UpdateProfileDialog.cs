using DigitalTrainingAssistant.Bot.Cards;
using DigitalTrainingAssistant.Bot.Helpers;
using DigitalTrainingAssistant.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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

        private readonly IHttpClientFactory _clientFactory;

        private BotConfig _botConfig;
        private BotHelper _botHelper;
        public UpdateProfileDialog(BotHelper botHelper, BotConfig botConfig, ILogger<UpdateProfileDialog> logger, IHttpClientFactory clientFactory)
            : base(nameof(UpdateProfileDialog), botConfig.BotOAuthConnectionName)
        {
            Logger = logger;
            _botHelper = botHelper;
            _botConfig = botConfig;
            _clientFactory = clientFactory;

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
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
                SaveProfileAsync,
                HandleAskIfUploadPhoto,
                GetPhoto
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
                await MainDialog.HandleCardResponse(stepContext, stepContext.Context.Activity.Value?.ToString(), cancellationToken, _botConfig);
            }


            // See if they want to upload a photo
            string msg = $"Would you like to upload a photo of yourself for the team?";
            var promptMessage = MessageFactory.Text(msg, msg, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }


        private async Task<DialogTurnResult> HandleAskIfUploadPhoto(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var courseAttendance = (CourseAttendance)stepContext.Options;

            bool uploadPhoto = (bool)stepContext.Result;

            if (uploadPhoto)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Send me it then. File attachement icon should be in the chat window"), cancellationToken);
                var opts = new PromptOptions
                {
                    Prompt = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = "waiting for file...", // You can comment this out if you don't want to display any text. Still works.
                    }
                };
                return await stepContext.PromptAsync(nameof(TextPrompt), opts);
            }
            else
            {
                // Save and done
                await PostIntroToTeam(stepContext, courseAttendance, cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> GetPhoto(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var courseAttendance = (CourseAttendance)stepContext.Options;
            bool messageWithFileDownloadInfo = stepContext.Context.Activity.Attachments?[0].ContentType == FileDownloadInfo.ContentType;
            if (messageWithFileDownloadInfo)
            {
                var file = stepContext.Context.Activity.Attachments[0];
                var fileDownload = Newtonsoft.Json.Linq.JObject.FromObject(file.Content).ToObject<FileDownloadInfo>();

                string filePath = Path.Combine("Files", file.Name);

                var client = _clientFactory.CreateClient();
                var response = await client.GetAsync(fileDownload.DownloadUrl);
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                var reply = MessageFactory.Text($"<b>{file.Name}</b> received and saved.");
                reply.TextFormat = "xml";
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
            }
            else if (stepContext.Context.Activity.Attachments?[0].ContentType.Contains("image/*") == true)
            {
                // Inline image se.
                await ProcessInlineImage(stepContext.Context, cancellationToken);
            }
            await PostIntroToTeam(stepContext, courseAttendance, cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);

        }

        private async Task ProcessInlineImage(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var attachment = turnContext.Activity.Attachments[0];
            var client = _clientFactory.CreateClient();

            // Get Bot's access token to fetch inline image. 
            var token = await new MicrosoftAppCredentials(_botConfig.MicrosoftAppId, _botConfig.MicrosoftAppPassword).GetTokenAsync();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var responseMessage = await client.GetAsync(attachment.ContentUrl);

            // Save the inline image to Files directory.
            var filePath = Path.Combine("Files", "ImageFromUser.png");
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await responseMessage.Content.CopyToAsync(fileStream);
            }

            // Create reply with image.
            var reply = MessageFactory.Text($"Attachment of {attachment.ContentType} type and size of {responseMessage.Content.Headers.ContentLength} bytes received.");
            reply.Attachments = new List<Attachment>() { GetInlineAttachment() };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }


        private static Attachment GetInlineAttachment()
        {
            var imagePath = Path.Combine("Files", "ImageFromUser.png");
            var imageData = Convert.ToBase64String(File.ReadAllBytes(imagePath));

            return new Attachment
            {
                Name = @"ImageFromUser.png",
                ContentType = "image/png",
                ContentUrl = $"data:image/png;base64,{imageData}",
            };
        }

        async Task PostIntroToTeam(WaterfallStepContext stepContext, CourseAttendance courseAttendance, CancellationToken cancellationToken)
        {

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
        }
    }

}
