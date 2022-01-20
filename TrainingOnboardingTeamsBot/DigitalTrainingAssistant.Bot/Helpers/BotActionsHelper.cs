using DigitalTrainingAssistant.Bot.Cards;
using DigitalTrainingAssistant.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Helpers
{
    /// <summary>
    /// Bot functionality
    /// </summary>
    public class BotActionsHelper : AuthHelper
    {
        #region Privates & Constructors

        private BotConversationCache _conversationCache = null;
        private ILogger<BotActionsHelper> _logger = null;
        private BotAppInstallHelper _botAppInstallHelper;
        public BotActionsHelper(BotConfig config, BotConversationCache botConversationCache, ILogger<BotActionsHelper> logger, BotAppInstallHelper botAppInstallHelper)
        {
            this.Config = config;
            this._conversationCache = botConversationCache;
            _logger = logger;
            _botAppInstallHelper = botAppInstallHelper;

            _logger.LogInformation($"Have config: AppBaseUri:{config.AppBaseUri}, MicrosoftAppId:{config.MicrosoftAppId}, AppCatalogTeamAppId:{config.AppCatalogTeamAppId}");
        }
        #endregion

        #region Properties

        public BotConfig Config { get; set; }


        #endregion



        /// <summary>
        /// Main logic for sending notifications to trainees
        /// </summary>
        public async Task SendCourseIntroAndTrainingRemindersToUser(CachedUserAndConversationData toUser, ITurnContext turnContext, CancellationToken cancellationToken, PendingUserActions userPendingActionsForCourse, GraphServiceClient graphClient)
        {
            // Send seperate card for each course with outstanding items
            foreach (var course in userPendingActionsForCourse.UniqueCourses)
            {
                // Install bot to course Team if there is one
                if (course.HasValidTeamsSettings)
                {
                    // Todo: check app installation 1st
                    try
                    {
                        await _botAppInstallHelper.InstallTrainingBotForTeam(course.TeamId, Config.TenantId, Config.MicrosoftAppId, Config.MicrosoftAppPassword, Config.AppCatalogTeamAppId);
                    }
                    catch (ServiceException ex)
                    {
                        if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            throw new GraphAccessException("I don't seem to have permissions to install to the associated Team to publish introductions. (TeamsAppInstallation.ReadWriteForTeam.All)");
                        }
                    }
                }

                // Get attendee info for this user
                var userAttendeeInfoForCourse = userPendingActionsForCourse.Actions
                    .Where(a => a.Course == course)
                        .Select(a => a.Attendee)
                        .Where(a => a.User.Email == toUser.EmailAddress).FirstOrDefault();

                // Send course intro?
                if (userAttendeeInfoForCourse != null)
                {
                    if (!userAttendeeInfoForCourse.BotContacted && course.HasValidTeamsSettings)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(new CourseWelcomeCard(BotConstants.BotName, course).GetCard()), cancellationToken);

                        // Don't send course intro twice to same user
                        userAttendeeInfoForCourse.BotContacted = true;
                        await userAttendeeInfoForCourse.SaveChanges(graphClient, Config.SharePointSiteId);
                    }

                    // Send outstanding course actions
                    var actionsForCourse = userPendingActionsForCourse.Actions.Where(a => a.Course == course);
                    var coursePendingItemsAttachment = new PendingTasksListCard(userAttendeeInfoForCourse, actionsForCourse, course).GetCard();

                    await turnContext.SendActivityAsync(MessageFactory.Attachment(coursePendingItemsAttachment), cancellationToken);
                }

            }
        }

        public async Task<PendingUserActions> RemindClassMembersWithOutstandingTasks(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken, bool filterByCourseReminderDays)
        {
            var token = await AuthHelper.GetToken(turnContext.Activity.Conversation.TenantId, Config.MicrosoftAppId, Config.MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            var conversationReference = turnContext.Activity.GetConversationReference();

            // Ensure calling user is fully cached
            await _conversationCache.AddOrUpdateUserAndConversationId(conversationReference, turnContext.Activity.ServiceUrl, graphClient);

            var userTalkingEmail = _conversationCache.GetCachedUsers().Where(u => u.RowKey == conversationReference.User.AadObjectId).SingleOrDefault();

            return await RemindClassMembersWithOutstandingTasks(turnContext.Adapter, userTalkingEmail.EmailAddress, turnContext.Activity.Conversation.TenantId, cancellationToken, filterByCourseReminderDays);
        }

        async Task<bool> CheckIfUserHasActionsAndSendMessagesIfNeeded(CachedUserAndConversationData user, PendingUserActions userPendingActions, BotAdapter botAdapter, GraphServiceClient graphClient, CancellationToken cancellationToken)
        {
            // Does this user have any training actions?
            var thisUserPendingActions = userPendingActions.GetActionsByEmail(user.EmailAddress);
            if (thisUserPendingActions.Actions.Count > 0)
            {
                var previousConversationReference = new ConversationReference()
                {
                    ChannelId = CardConstants.TeamsBotFrameworkChannelId,
                    Bot = new ChannelAccount() { Id = $"28:{Config.AppCatalogTeamAppId}" },
                    ServiceUrl = user.ServiceUrl,
                    Conversation = new ConversationAccount() { Id = user.ConversationId },
                };

                // Ping an update
                await botAdapter.ContinueConversationAsync(Config.MicrosoftAppId, previousConversationReference,
                    async (turnContext, cancellationToken)
                        => await SendCourseIntroAndTrainingRemindersToUser(user, turnContext, cancellationToken, thisUserPendingActions, graphClient)
                    , cancellationToken);

                return true;
            }
            return false;
        }

        internal async Task<PendingUserActions> RemindClassMembersWithOutstandingTasks(BotAdapter botAdapter, string trainerEmail, string tenantId, CancellationToken cancellationToken, bool filterByCourseReminderDays)
        {
            var token = await AuthHelper.GetToken(tenantId, Config.MicrosoftAppId, Config.MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            // Load all course data from lists
            var allTrainingData = await CoursesMetadata.LoadTrainingSPData(graphClient, Config.SharePointSiteId);

            var coursesThisUserIsLeading = allTrainingData.Courses.Where(c => c.Trainer?.Email?.ToLower() == trainerEmail.ToLower()).ToList();

            var pendingTrainingActionsForCoursesThisUserIsTeaching = allTrainingData.GetUserActionsWithThingsToDo(coursesThisUserIsLeading, filterByCourseReminderDays);

            if (pendingTrainingActionsForCoursesThisUserIsTeaching.Actions.Count > 0)
            {
                // Send notification to all the members for this users classes
                foreach (var user in _conversationCache.GetCachedUsers())
                {

                    // Does this user have any custom training actions?
                    var thisUserPendingActions = pendingTrainingActionsForCoursesThisUserIsTeaching.GetActionsByEmail(user.EmailAddress);
                    if (thisUserPendingActions.Actions.Count > 0)
                    {
                        try
                        {
                            await CheckIfUserHasActionsAndSendMessagesIfNeeded(user, thisUserPendingActions, botAdapter, graphClient, cancellationToken);
                        }
                        catch (ErrorResponseException)
                        {
                            // Something wierd happened resuming the conversation. Assume invalid conversation reference cache
                            await _conversationCache.RemoveFromCache(user.RowKey);
                        }
                    }
                }
            }

            // Install for anyone not cached yet. Will also trigger a reminder for each user
            var cachedConversationEmailAddresses = _conversationCache.GetCachedUsers().Select(u => u.EmailAddress.ToLower());
            var actionsEmailAddresses = pendingTrainingActionsForCoursesThisUserIsTeaching.UniqueUsers.Select(u => u.User.Email.ToLower());

            var uncachedEmailAddresses = actionsEmailAddresses.Except(cachedConversationEmailAddresses);

            foreach (var userEmailToInstallApp in uncachedEmailAddresses)
            {
                // Get user from Graph
                User user = null;
                try
                {
                    user = await graphClient.Users[userEmailToInstallApp].Request().GetAsync();
                }
                catch (ServiceException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new GraphAccessException("I don't seem to have permissions to read Azure AD users (User.Read.All)");
                    }
                    throw;
                }

                try
                {
                    await _botAppInstallHelper.InstallTrainingBotForUser(user.Id,
                        tenantId,
                        Config.MicrosoftAppId,
                        Config.MicrosoftAppPassword,
                        Config.AppCatalogTeamAppId);
                }
                catch (ServiceException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new GraphAccessException("I don't seem to have permissions to install my Teams App for trainees so I can proactively remind them (TeamsAppInstallation.ReadWriteForUser.All)");
                    }

                    throw;
                }

            }

            return pendingTrainingActionsForCoursesThisUserIsTeaching;
        }

    }
}
