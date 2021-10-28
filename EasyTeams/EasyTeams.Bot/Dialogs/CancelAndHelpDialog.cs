// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using EasyTeams.Common;
using EasyTeams.Common.Config;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace EasyTeams.Bot.Dialogs
{
    public class CancelAndHelpDialog : ComponentDialog
    {
        private const string HelpMsgText = "Ask to create a conference call. At the moment that's all I can do.";
        private const string CancelMsgText = "Ok then; I was bored of this conversation anyway.";

        private SystemSettings _settings;

        public CancelAndHelpDialog(string id, SystemSettings systemSettings)
            : base(id)
        {
            _settings = systemSettings;
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                var text = innerDc.Context.Activity.Text?.ToLowerInvariant();

                switch (text)
                {
                    case "systemtest":

                        string msg = "Testing with config: " + _settings.ToString();
                        var pingMessage = MessageFactory.Text(msg, msg, InputHints.ExpectingInput);
                        await innerDc.Context.SendActivityAsync(pingMessage, cancellationToken);

                        var p = new FunctionAppProxy(_settings);
                        await p.PostDataToFunctionApp(EasyTeamsConstants.FUNCTION_BODY_TEST, true);

                        await innerDc.Context.SendActivityAsync(MessageFactory.Text("That appeared to work!"), cancellationToken);

                        break;
                    case "help":
                    case "?":
                        var helpMessage = MessageFactory.Text(HelpMsgText, HelpMsgText, InputHints.ExpectingInput);
                        await innerDc.Context.SendActivityAsync(helpMessage, cancellationToken);
                        return new DialogTurnResult(DialogTurnStatus.Waiting);

                    case "cancel":
                    case "quit":
                        var cancelMessage = MessageFactory.Text(CancelMsgText, CancelMsgText, InputHints.IgnoringInput);
                        await innerDc.Context.SendActivityAsync(cancelMessage, cancellationToken);
                        return await innerDc.CancelAllDialogsAsync(cancellationToken);
                }
            }

            return null;
        }

    }
}
