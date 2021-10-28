using EasyTeams.Bot.Models;
using EasyTeams.Common;
using EasyTeams.Common.BusinessLogic;
using EasyTeams.Common.Config;
using EasyTeamsBot.Common;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTeams.Bot.Dialogs
{
    public class AddPeopleDialog : CancelAndHelpDialog
    {
        public AddPeopleDialog(SystemSettings settings) : base(nameof(AddPeopleDialog), settings)
        {
            this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            AddDialog(new TextPrompt(nameof(TextPrompt)));  // People name search
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] 
            {
                AskForName,
                ResolveName,
                ConfirmName,
                ConfirmDone
            }));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));              // People picker

            InitialDialogId = nameof(WaterfallDialog);
        }

        public SystemSettings Settings { get; set; }


        private async Task<DialogTurnResult> AskForName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions() 
            { 
                Prompt = MessageFactory.Text("Who should we add to the meeting? Search for people by a name/email, or for external contacts, enter their email address..."), 
                RetryPrompt = MessageFactory.Text("Seriously, who?")
            });
            
        }
        private async Task<DialogTurnResult> ResolveName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var searchParams = (PeopleSearchList)stepContext.Options;

            var searchQuery = (string)stepContext.Result;

            var teamsManager = new PrecachedAuthTokenTeamsManager(searchParams.OAuthToken.Token, Settings);

            // Is this an email search or a wildcard search?
            if (MeetingContact.IsValidEmailAddress(searchQuery))
            {
                // Email address search
                User userSearch = null;
                try
                {
                    userSearch = await teamsManager.Cache.GetUser(searchQuery);
                }
                catch (ServiceException ex)
                {
                    // Is this error because the user doesn't exist in the directory?
                    if (ex.Error.Code == EasyTeamsConstants.GRAPH_ERROR_RESOURCE_NOT_FOUND)
                    {
                        userSearch = null;      // Assume external contact
                    }
                    else
                    {
                        throw;
                    }
                }
                
                if (userSearch == null)
                {
                    // External
                    // Ask user to select the external user
                    string msg = $"This one?";
                    var promptMessage = MessageFactory.Text(msg, msg, Microsoft.Bot.Schema.InputHints.ExpectingInput);
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                    {
                        Prompt = promptMessage,
                        Choices = new List<Choice>() { new Choice($"{EasyTeamsConstants.STRING_EXTERNAL_CONTACT} ({searchQuery})") }
                    }, cancellationToken);
                }
                else
                {
                    // Ask user to select the single internal user
                    string msg = $"This one?";
                    var promptMessage = MessageFactory.Text(msg, msg, Microsoft.Bot.Schema.InputHints.ExpectingInput);
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                    {
                        Prompt = promptMessage,
                        Choices = new List<Choice>() { new Choice($"{userSearch.DisplayName} ({userSearch.UserPrincipalName})") }
                    }, cancellationToken);
                }
            }

            // Wildcard search. Build & execute people search request
            var req = teamsManager.Client.Me.People.Request();
            req.QueryOptions.Add(new QueryOption("$search", searchQuery));
            var peopleResultsTopTen = await req.GetAsync();

            // Did the people search return anything?
            if (peopleResultsTopTen.Count > 0)
            {
                // Build list of choices from results
                var choices = new List<Choice>();
                foreach (var searchResult in peopleResultsTopTen)
                {
                    choices.Add(new Choice()
                    {
                        Value = $"{searchResult.DisplayName} ({searchResult.UserPrincipalName})"
                    });
                }

                // Ask user to select a person
                string msg = $"Which one?";
                var promptMessage = MessageFactory.Text(msg, msg, Microsoft.Bot.Schema.InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = choices }, cancellationToken);
            }
            else
            {
                // No people search results, and it wasn't an email address
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Couldn't find anyone with that name internally. Try another name."));

                // Go around again
                return await stepContext.ReplaceDialogAsync(nameof(AddPeopleDialog), searchParams, cancellationToken);
            }
        }


        private async Task<DialogTurnResult> ConfirmName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogParams = (PeopleSearchList)stepContext.Options;

            // Get selected contact
            var selectedContactOption = (FoundChoice)stepContext.Result;
            var selectedContact = selectedContactOption.Value;

            string selectedEmail = DataUtils.ExtractEmailFromContact(selectedContact);
            bool externalContact = selectedContact.StartsWith(EasyTeamsConstants.STRING_EXTERNAL_CONTACT);
            dialogParams.Recipients.Add(new MeetingContact(selectedEmail, externalContact));

            string msg = $"Anyone else?";
            var promptMessage = MessageFactory.Text(msg, msg, Microsoft.Bot.Schema.InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }


        private async Task<DialogTurnResult> ConfirmDone(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            bool addMorePeople = (bool)stepContext.Result;

            var dialogParams = (PeopleSearchList)stepContext.Options;
            if (addMorePeople)
            {
                // Go around again
                return await stepContext.ReplaceDialogAsync(nameof(AddPeopleDialog), dialogParams, cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync(dialogParams, cancellationToken);
            }
        }


    }

}
