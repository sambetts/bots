using DigitalTrainingAssistant.Bot.Dialogues.Abstract;
using DigitalTrainingAssistant.Bot.Helpers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Dialogues
{
    /// <summary>
    /// Entrypoint to all new conversations
    /// </summary>
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
                var command = inputText.ToLower();

                if (command == "remind")
                {
                    try
                    {
                        // Find users to notify for outstanding tasks
                        var coursesFound = await _botHelper.RemindClassMembersWithOutstandingTasks((ITurnContext<IMessageActivity>)stepContext.Context, cancellationToken, false);

                        // Send actions summary back to trainer
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                            $"Found {coursesFound.Actions.Count} outstanding action(s) for {coursesFound.UniqueUsers.Count} user(s), " +
                            $"across {coursesFound.UniqueCourses.Count} course(s) that you are the trainer for. All users notified."
                            ), cancellationToken);
                    }
                    catch (BotSharePointAccessException)
                    {
                        // Can't connect to SharePoint
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                            $"I can't seem to access my database to figure out what's going on, sorry! Check my access to SharePoint site '{_configuration.SharePointSiteId}' " +
                            $"(app ID {_configuration.MicrosoftAppId})?"
                        ), cancellationToken);

                    }
                    catch (GraphAccessException ex)
                    {
                        // The exception contains the message for users
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(ex.Message), cancellationToken);
                    }
                }
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
    }
}
