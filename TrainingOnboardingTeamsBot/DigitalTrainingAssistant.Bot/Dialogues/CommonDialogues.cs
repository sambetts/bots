using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Dialogues
{
    public class CommonDialogues
    {

        public static async Task<DialogTurnResult> ReplyWithNoIdeaAndEndDiag(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                    $"You sent me something but I can't work out what, sorry! Try again?."
                    ), cancellationToken);
            return await stepContext.EndDialogAsync(null);
        }
    }
}
