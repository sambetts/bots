using EasyTeams.Bot.Models;
using EasyTeams.Common;
using EasyTeams.Common.Config;
using EasyTeamsBot.Common;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTeams.Bot.Dialogs
{
    /// <summary>
    /// Creates a new conf call object; populates with data from Graph.
    /// </summary>
    public class NewConferenceCallDiag : CancelAndHelpDialog
    {
        public NewConferenceCallDiag(SystemSettings settings) : base(nameof(NewConferenceCallDiag), settings)
        {
            this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            AddDialog(new OAuthPrompt(
                   nameof(OAuthPrompt),
                   new OAuthPromptSettings
                   {
                       ConnectionName = EasyTeamsConstants.BOT_OAUTH_CONNECTION_NAME,
                       Text = "I need your permission to do access Office 365: people searches, your email address, and your calendar...",
                       Title = "Login to Office 365"
                   }, LoginValidator));
            AddDialog(new DateResolverDialog(settings));                // When
            AddDialog(new TextPrompt(nameof(TextPrompt)));      // Subject
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
               {
                    WhatSubject,
                    AskWhen,
                    AskHowLong,
                    Login,
                    AddPeople,
                    ConfirmMeetingDetails,
                    ConfirmFinish
               }));
            AddDialog(new AddPeopleDialog(Settings));        // Add people
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>), DurationValidator));    // Duration prompt
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));              // Conf call details confirmation

            InitialDialogId = nameof(WaterfallDialog);
        }

        public SystemSettings Settings { get; set; }

        private async Task<DialogTurnResult> WhatSubject(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var newCallDetails = (GraphNewConferenceCallRequest)stepContext.Options;
            if (string.IsNullOrWhiteSpace(newCallDetails.Subject))
            {
                // No date supplied at all
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions() { Prompt = MessageFactory.Text("Subject?") }, cancellationToken);
            }
            else
            {
                // We have a subject already. Move on.
                return await stepContext.NextAsync(newCallDetails.Start.Value, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> AskWhen(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var newCallDetails = (GraphNewConferenceCallRequest)stepContext.Options;

            // Save subject
            var subject = (string)stepContext.Result;
            newCallDetails.Subject = subject;

            if (!newCallDetails.Start.HasValue)
            {
                // No date supplied at all
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), null, cancellationToken);
            }
            else
            {
                if (!newCallDetails.Start.Value.HasValidTime())
                {
                    // Date supplied but no time
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("I saw a date, but not a time..."));
                    return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), null, cancellationToken);
                }
            }

            // We have a date already. Move on.
            return await stepContext.NextAsync(newCallDetails.Start.Value, cancellationToken);
        }

        private async Task<DialogTurnResult> AskHowLong(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var newCallDetails = (GraphNewConferenceCallRequest)stepContext.Options;

            // Get date from last step
            var startDate = (DateTime)stepContext.Result;
            newCallDetails.Start = startDate;

            if (!newCallDetails.MinutesLong.HasValue)
            {
                // No time specified. Ask.
                return await stepContext.PromptAsync(nameof(NumberPrompt<int>), new PromptOptions() { Prompt = MessageFactory.Text("How minutes will the call be?") }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(newCallDetails.MinutesLong, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> Login(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var newCallDetails = (GraphNewConferenceCallRequest)stepContext.Options;

            // Get length from last step
            var len = (int)stepContext.Result;
            newCallDetails.MinutesLong = len;

            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> AddPeople(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var newCallDetails = (GraphNewConferenceCallRequest)stepContext.Options; 
            
            // Get login token
            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse != null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Looks like I've got access to your Office 365 data I need..."), cancellationToken);

                // Remember OAuth token
                newCallDetails.OAuthToken = tokenResponse;

                // Figure out email address of this user
                var teamsManager = new PrecachedAuthTokenTeamsManager(newCallDetails.OAuthToken.Token, Settings);
                var user = await teamsManager.Client.Me.Request().Select("UserPrincipalName,MailboxSettings").GetAsync();
                var userMailboxSettings = await teamsManager.Client.Me.Request().GetAsync();
                newCallDetails.OnBehalfOf = new Common.BusinessLogic.MeetingContact(user.UserPrincipalName, false);
                newCallDetails.TimeZoneName = user.MailboxSettings.TimeZone;
            }

            // Pass in OAuth
            var peopleDialogParam = new PeopleSearchList() { OAuthToken = newCallDetails.OAuthToken };

            // Add all the people
            return await stepContext.BeginDialogAsync(nameof(AddPeopleDialog), peopleDialogParam, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmMeetingDetails(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var newCallDetails = (GraphNewConferenceCallRequest)stepContext.Options;

            var peopleDialogResult = (PeopleSearchList)stepContext.Result;
            newCallDetails.Recipients.AddRange(peopleDialogResult.Recipients);

            // Send adaptive card with conference-call details
            var adaptiveCard = CardGenerator.GetConferenceDetailsCard(newCallDetails);
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = adaptiveCard,
            };
            var promptMessageSummary = MessageFactory.Attachment(adaptiveCardAttachment, "All Good?");
            await stepContext.Context.SendActivityAsync(promptMessageSummary, cancellationToken);

            // Check S'all Good, Man
            string msg = $"All good?";
            var promptMessage = MessageFactory.Text(msg, msg, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = GetChoices() }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmFinish(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var response = (FoundChoice)stepContext.Result;
            if (response.Value == "Send it")
            {

                var newCallDetails = (GraphNewConferenceCallRequest)stepContext.Options;

                // Create call
                var teamsManager = new PrecachedAuthTokenTeamsManager(newCallDetails.OAuthToken.Token, Settings);
                var call = await teamsManager.CreateNewConferenceCall(newCallDetails, true);

                // Send adaptive card with Teams details
                var adaptiveCard = CardGenerator.GetTeamsCallDetailsCard(call);
                var adaptiveCardAttachment = new Attachment()
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = adaptiveCard,
                };
                var promptMessageSummary = MessageFactory.Attachment(adaptiveCardAttachment, "Your Teams Meeting");
                await stepContext.Context.SendActivityAsync(promptMessageSummary, cancellationToken);
            }
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private IList<Choice> GetChoices()
        {
            return new List<Choice>() {
                new Choice() { Value = "Send it", Synonyms = new List<string>() { "Yes", "Do it", "Send" } },
                new Choice() { Value = "Cancel", Synonyms = new List<string>() { "No", "Stop", "Abort" } }
            };
        }

        #region Validators

        private Task<bool> LoginValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                return Task.FromResult(promptContext.Recognized.Value != null);
            }
            return Task.FromResult(false);
        }

        private static Task<bool> DurationValidator(PromptValidatorContext<int> numberPrompt, CancellationToken cancellationToken)
        {
            if (numberPrompt.Recognized.Succeeded)
            {
                int duration = numberPrompt.Recognized.Value;
                return (Task.FromResult<bool>(duration > 0));
            }

            return Task.FromResult<bool>(false);
        }
        #endregion
    }
}
