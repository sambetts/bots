using DigitalTrainingAssistant.Bot.Dialogues.Abstract;
using DigitalTrainingAssistant.Bot.Helpers;
using DigitalTrainingAssistant.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Dialogues
{
    /// <summary>
    /// Entrypoint to all new conversations
    /// </summary>
    public class MainDialog : CancelAndHelpDialog
    {
        private BotActionsHelper _botHelper;
        private BotConfig _configuration;
        private BotConversationCache _botConversationCache;
        public MainDialog(UpdateProfileDialog updateProfileDialog, BotActionsHelper botHelper, BotConfig configuration, BotConversationCache botConversationCache) : base(nameof(MainDialog))
        {
            _botHelper = botHelper;
            _configuration = configuration;
            _botConversationCache = botConversationCache;

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                Act
            }));
            AddDialog(updateProfileDialog);
            InitialDialogId = nameof(WaterfallDialog);
        }

        /// <summary>
        /// Main entry-point for bot new chat
        /// </summary>
        private async Task<DialogTurnResult> Act(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var inputText = stepContext.Context.Activity.Text ?? string.Empty;
            var val = stepContext.Context.Activity.Value ?? string.Empty;

            if (val != null && !string.IsNullOrEmpty(val.ToString()))
            {
                return await HandleCardResponse(stepContext, val.ToString(), cancellationToken, _configuration);
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
                    catch (BotException ex)
                    {
                        // The exception contains the message for users
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(ex.Message), cancellationToken);
                    }
                }
                else if (command == "reset")
                {
                    // Reset user profile
                    var token = await AuthHelper.GetToken(_configuration.TenantId, _configuration.MicrosoftAppId, _configuration.MicrosoftAppPassword);
                    var graphClient = AuthHelper.GetAuthenticatedClient(token);
                    var allData = await CoursesMetadata.LoadTrainingSPData(graphClient, _configuration.SharePointSiteId);


                    var conversationReference = stepContext.Context.Activity.GetConversationReference();
                    await _botConversationCache.AddOrUpdateUserAndConversationId(conversationReference, stepContext.Context.Activity.ServiceUrl, graphClient);
                    var userTalkingEmail = _botConversationCache.GetCachedUsers().Where(u => u.RowKey == conversationReference.User.AadObjectId).SingleOrDefault();

                    int removeCount = 0;
                    foreach (var c in allData.Courses)
                    {
                        var attendeesWithThisUserEmail = c.Attendees.Where(a => a.User.Email == userTalkingEmail.EmailAddress);
                        foreach (var attendance in attendeesWithThisUserEmail)
                        {
                            attendance.BotContacted = false;
                            attendance.IntroductionDone = false;
                            await attendance.SaveChanges(graphClient, _configuration.SharePointSiteId);
                            removeCount++;
                        }
                    }

                    // Remove conversation cache
                    await _botConversationCache.RemoveFromCache(conversationReference.User.AadObjectId);

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                            $"Forgot you from {removeCount} courses."
                        ), cancellationToken);
                }
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }


        /// <summary>
        /// Someone replied via an Adaptive card form
        /// </summary>
        public static async Task<DialogTurnResult> HandleCardResponse(WaterfallStepContext stepContext, string submitJson, CancellationToken cancellationToken, BotConfig _configuration)
        {
            // Form action
            var action = AdaptiveCardUtils.GetAdaptiveCardAction(submitJson, stepContext.Context.Activity.From.AadObjectId);

            // Figure out what was done
            if (action is CourseTasksUpdateInfo)
            {
                var update = (CourseTasksUpdateInfo)action;
                await update.SendReply(stepContext.Context, cancellationToken, _configuration.MicrosoftAppId, _configuration.MicrosoftAppPassword, _configuration.SharePointSiteId);

            }
            else if (action is ActionResponseForSharePointItem)
            {
                var token = await AuthHelper.GetToken(stepContext.Context.Activity.Conversation.TenantId, _configuration.MicrosoftAppId, _configuration.MicrosoftAppPassword);
                var graphClient = AuthHelper.GetAuthenticatedClient(token);

                var update = (ActionResponseForSharePointItem)action;

                var attendanceInfo = await CourseAttendance.LoadById(graphClient, _configuration.SharePointSiteId, update.SPID);
                return await stepContext.BeginDialogAsync(nameof(UpdateProfileDialog), attendanceInfo, cancellationToken);

            }

            // Something else
            return await CommonDialogues.ReplyWithNoIdeaAndEndDiag(stepContext, cancellationToken);

        }

    }
}
