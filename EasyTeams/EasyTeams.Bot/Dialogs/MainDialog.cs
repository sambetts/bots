using EasyTeams.Common;
using EasyTeams.Common.Config;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTeams.Bot.Dialogs
{
    public class MainDialog : CancelAndHelpDialog
    {
        private ConfCallManagerRecognizer _confCallManagerRecognizer;
        public MainDialog(NewConferenceCallDiag newConferenceCallDiag, ConfCallManagerRecognizer luisRecognizer, SystemSettings settings) 
            : base(nameof(MainDialog), settings)
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] 
            {
                AskWhatTheyWant,
                DoWhatTheyWant,
                End
            }));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(newConferenceCallDiag);
            InitialDialogId = nameof(WaterfallDialog);

            _confCallManagerRecognizer = luisRecognizer;
        }

        private async Task<DialogTurnResult> AskWhatTheyWant(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Activity lastDiagActivity = (Activity)stepContext.Options;

            // Did the user type something before this new dialogue? Could've been an instruction after last dialogue finished...
            // Make sure it's not the same as last time we started this dialogue to avoid infinite dialog loops
            var lastActivity = stepContext.Context.Activity;
            if (lastActivity?.Text != null && lastDiagActivity?.Text != lastActivity.Text)
            {
                // User typed something, and it wasn't the same as the last time we were here.
                string lastMsg = (string)lastActivity.Text;
                return await stepContext.NextAsync(lastMsg, cancellationToken);
            }
            else
            {
                // If nothing typed before, ask
                string msg = "What do you want to do?";
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions() { Prompt = MessageFactory.Text(msg) }, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> DoWhatTheyWant(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResponse = await _confCallManagerRecognizer.RecognizeAsync<LUISConferenceCallRequest>(stepContext.Context, cancellationToken);
            switch (luisResponse.TopIntent().intent)
            {
                case LUISConferenceCallRequest.Intent.AddPerson:

                    // ToDo: Add person to existing call. 
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("You want to create a conference call then? Say 'Create conference'"));
                    break;
                case LUISConferenceCallRequest.Intent.CreateConferenceCall:

                    // Start new conference-call
                    DateTime? when = null;
                    if (!string.IsNullOrEmpty(luisResponse.WhenTimex))
                    {
                        var timex = new Microsoft.Recognizers.Text.DataTypes.TimexExpression.TimexProperty(luisResponse.WhenTimex);
                        when = timex.GetDateTime();
                    }
                    var newConfDetails = new GraphNewConferenceCallRequest() { Start = when };

                    return await stepContext.BeginDialogAsync(nameof(NewConferenceCallDiag), newConfDetails, cancellationToken);

                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Try typing that again, or ask me for help.";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);

                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Go around again. Pass the last activity in so it can be compared on start of this diag & not used again
            return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), stepContext.Context.Activity, cancellationToken);
        }
    }
}
