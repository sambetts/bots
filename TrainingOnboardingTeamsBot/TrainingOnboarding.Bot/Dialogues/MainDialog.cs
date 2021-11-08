using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using TrainingOnboarding.Bot.Cards;
using TrainingOnboarding.Bot.Dialogues.Abstract;
using TrainingOnboarding.Bot.Helpers;
using TrainingOnboarding.Bot.Models;
using TrainingOnboarding.Models;

namespace TrainingOnboarding.Bot.Dialogues
{
    public class MainDialog : CancelAndHelpDialog
    {
        private BotHelper _botHelper;
        private BotConfig _configuration;
        public MainDialog(UpdateProfileDialog updateProfileDialog, BotHelper botHelper, BotConfig configuration) : base(nameof(MainDialog))
        {
            _botHelper = botHelper;
            _configuration = configuration;

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                Act
            }));
            AddDialog(updateProfileDialog);
            InitialDialogId = nameof(WaterfallDialog);

        }


        private async Task<DialogTurnResult> Act(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var inputText = stepContext.Context.Activity.Text ?? string.Empty;

            var val = stepContext.Context.Activity.Value ?? string.Empty;

            if (val != null && !string.IsNullOrEmpty(val.ToString()))
            {
                return await _botHelper.HandleCardResponse(stepContext, val.ToString(), cancellationToken);
            }
            else 
            {
                // Text command. Done. 
                if (inputText.ToLower() == "remind")
                {
                    var coursesFound = await _botHelper.RemindClassMembersWithOutstandingTasks((ITurnContext<IMessageActivity>)stepContext.Context, cancellationToken, false);

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                        $"Found {coursesFound.Actions.Count} outstanding action(s) for {coursesFound.UniqueUsers.Count} user(s), " +
                        $"across {coursesFound.UniqueCourses.Count} course(s) that you are the trainer for. All users notified."
                        ), cancellationToken);
                }
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

        }


    }
}
