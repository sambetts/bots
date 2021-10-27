using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ProactiveBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrainingOnboarding.Bot.Cards;
using TrainingOnboarding.Models;

namespace TrainingOnboarding.Bot.Helpers
{
    /// <summary>
    /// Bot functionality
    /// </summary>
    public class BotHelper : AuthHelper
    {
        #region Privates & Constructors

        private ILogger<BotHelper> _logger;
        public BotHelper(IConfiguration configuration, BotConversationCache botConversationCache, ILogger<BotHelper> logger)
        {
            this.AppBaseUri = configuration["AppBaseUri"];
            this.MicrosoftAppId = configuration["MicrosoftAppId"];
            this.MicrosoftAppPassword = configuration["MicrosoftAppPassword"];
            this.AppCatalogTeamAppId = configuration["AppCatalogTeamAppId"];
            this.SiteId = configuration["SharePointSiteId"];
            this._conversationCache = botConversationCache;
            _logger = logger;

            _logger.LogInformation($"Have config: AppBaseUri:{AppBaseUri}, MicrosoftAppId:{MicrosoftAppId}, AppCatalogTeamAppId:{AppCatalogTeamAppId}");
        }
        #endregion

        #region Properties

        public string SiteId { get; set; }
        public string MicrosoftAppId { get; set; }
        public string MicrosoftAppPassword { get; set; }
        public string AppCatalogTeamAppId { get; set; }
        public string AppBaseUri { get; set; }

        BotConversationCache _conversationCache = null;

        #endregion

        /// <summary>
        /// App installed for user & now we have a conversation reference to cache for future chat threads.
        /// </summary>
        public async Task AddConversationReference(Activity activity)
        {
            var token = await AuthHelper.GetToken(activity.Conversation.TenantId, MicrosoftAppId, MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            var conversationReference = activity.GetConversationReference();
            await _conversationCache.AddOrUpdateUserAndConversationId(conversationReference, activity.ServiceUrl, graphClient);
        }

        public async Task<int> SendNotificationToAllUsersWithCoursesStartingIn(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken, int days)
        {
            if (days < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(days));
            }

            int msgSentCount = 0;

            var token = await AuthHelper.GetToken(turnContext.Activity.Conversation.TenantId, MicrosoftAppId, MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);


            // Load all course data from lists
            var courseInfo = await CoursesMetadata.LoadTrainingSPData(graphClient, this.SiteId);

            var compareDate = DateTime.Now.AddDays(days);
            var coursesStartingInRange = courseInfo.Courses.Where(c => c.Start.HasValue && c.Start.Value < compareDate).ToList();

            var pendingTrainingActions = courseInfo.GetUserActionsWithThingsToDo();

            // Send notification to all the cached members.
            foreach (var user in _conversationCache.GetCachedUsers())
            {
                bool userHasActions = await CheckIfUserHasActionsAndSendMessagesIfNeeded(user, pendingTrainingActions, turnContext.Adapter, graphClient, cancellationToken);
                if (userHasActions)
                {
                    msgSentCount++;
                }
            }

            // Install app for anyone not cached already. This will also trigger the same reminder

            return msgSentCount;
        }

        async Task<bool> CheckIfUserHasActionsAndSendMessagesIfNeeded(CachedUserData user, PendingUserActions pendingTrainingActions, BotAdapter botAdapter, GraphServiceClient graphClient, CancellationToken cancellationToken)
        {
            // Does this user have any training actions?
            var thisUserPendingActions = pendingTrainingActions.GetActionsByEmail(user.EmailAddress);
            if (thisUserPendingActions.Actions.Count > 0)
            {
                var previousConversationReference = new ConversationReference()
                {
                    ChannelId = CardConstants.TeamsBotFrameworkChannelId,
                    Bot = new ChannelAccount() { Id = $"28:{AppCatalogTeamAppId}" },
                    ServiceUrl = user.ServiceUrl,
                    Conversation = new ConversationAccount() { Id = user.ConversationId },
                };

                // Ping an update
                await botAdapter.ContinueConversationAsync(MicrosoftAppId, previousConversationReference,
                    async (turnContext, cancellationToken)
                        => await SendCourseIntroAndTrainingRemindersToUser(user, turnContext, cancellationToken, thisUserPendingActions, graphClient)
                    , cancellationToken);

                return true;
            }
            return false;
        }

        internal async Task<PendingUserActions> RemindClassMembersWithOutstandingTasks(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var token = await AuthHelper.GetToken(turnContext.Activity.Conversation.TenantId, MicrosoftAppId, MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            var conversationReference = turnContext.Activity.GetConversationReference();

            // Ensure calling user is fully cached
            await _conversationCache.AddOrUpdateUserAndConversationId(conversationReference, turnContext.Activity.ServiceUrl, graphClient);

            var userTalkingEmail = _conversationCache.GetCachedUsers().Where(u => u.RowKey == conversationReference.User.AadObjectId).SingleOrDefault();

            return await RemindClassMembersWithOutstandingTasks(turnContext.Adapter, userTalkingEmail, turnContext.Activity.Conversation.TenantId, cancellationToken);
        }


        internal async Task<PendingUserActions> RemindClassMembersWithOutstandingTasks(BotAdapter botAdapter, CachedUserData trainer, string tenantId, CancellationToken cancellationToken)
        {
            var token = await AuthHelper.GetToken(tenantId, MicrosoftAppId, MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            // Load all course data from lists
            var allTrainingData = await CoursesMetadata.LoadTrainingSPData(graphClient, this.SiteId);

            var coursesThisUserIsLeading = allTrainingData.Courses.Where(c => c.Trainer?.Email?.ToLower() == trainer.EmailAddress.ToLower()).ToList();

            var pendingTrainingActionsForCoursesThisUserIsTeaching = allTrainingData.GetUserActionsWithThingsToDo(coursesThisUserIsLeading);

            if (pendingTrainingActionsForCoursesThisUserIsTeaching.Actions.Count > 0)
            {
                // Send notification to all the members for this users classes
                foreach (var user in _conversationCache.GetCachedUsers())
                {

                    // Does this user have any training actions?
                    var thisUserPendingActions = pendingTrainingActionsForCoursesThisUserIsTeaching.GetActionsByEmail(user.EmailAddress);

                    await CheckIfUserHasActionsAndSendMessagesIfNeeded(user, thisUserPendingActions, botAdapter, graphClient, cancellationToken);
                    
                    //if (thisUserPendingActions.Actions.Count > 0)
                    //{
                    //    var previousConversationReference = new ConversationReference()
                    //    {
                    //        ChannelId = CardConstants.TeamsBotFrameworkChannelId,
                    //        Bot = new ChannelAccount() { Id = $"28:{AppCatalogTeamAppId}" },
                    //        ServiceUrl = user.ServiceUrl,
                    //        Conversation = new ConversationAccount() { Id = user.ConversationId },
                    //    };

                    //    // Ping an update for each course they're on
                    //    await botAdapter.ContinueConversationAsync(MicrosoftAppId, previousConversationReference,
                    //        async (turnContext, cancellationToken) => await SendCourseIntroAndTrainingRemindersToUser(user, turnContext, cancellationToken, thisUserPendingActions, graphClient), cancellationToken);
                    //}

                }
            }

            var cachedConversationEmailAddresses = _conversationCache.GetCachedUsers().Select(u => u.EmailAddress.ToLower());
            var actionsEmailAddresses = pendingTrainingActionsForCoursesThisUserIsTeaching.UniqueUsers.Select(u => u.User.Email.ToLower());

            var uncachedEmailAddresses = actionsEmailAddresses.Except(cachedConversationEmailAddresses);

            foreach (var userEmailToInstallApp in uncachedEmailAddresses)
            {
                var user = await graphClient.Users[userEmailToInstallApp].Request().GetAsync();
                await InstallTrainingBotForTarget(user.Id,
                        tenantId,
                        MicrosoftAppId,
                        MicrosoftAppPassword,
                        AppCatalogTeamAppId);
            }

            return pendingTrainingActionsForCoursesThisUserIsTeaching;
        }

        public async Task SendCourseIntroAndTrainingRemindersToUser(CachedUserData user, ITurnContext turnContext, CancellationToken cancellationToken, PendingUserActions thisUserPendingActions, GraphServiceClient graphClient)
        {
            // Send seperate card for each course with outstanding items
            foreach (var course in thisUserPendingActions.UniqueCourses)
            {
                // Get attendee info
                var attendeeInfoForCourse = thisUserPendingActions.Actions.Where(a => a.Course == course).Select(a => a.Attendee).Where(a => a.User.Email == user.EmailAddress).FirstOrDefault();

                // Send course intro?
                if (!attendeeInfoForCourse.BotContacted)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(new CourseWelcomeCard(BotConstants.BotName, course).GetCard()), cancellationToken);

                    // Don't send course intro twice
                    attendeeInfoForCourse.BotContacted = true;
                    await attendeeInfoForCourse.SaveChanges(graphClient, this.SiteId);
                }

                // Send outstanding course actions
                var actionsForCourse = thisUserPendingActions.Actions.Where(a => a.Course == course);
                var listCardAttachment = new LearningPlanListCard(actionsForCourse, course).GetCard();

                await turnContext.SendActivityAsync(MessageFactory.Attachment(listCardAttachment), cancellationToken);
            }
        }

        public async Task<InstallationCounts> InstallBotForCourseMembersAsync(string tenantId)
        {

            var token = await AuthHelper.GetToken(tenantId, MicrosoftAppId, MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            var courseInfo = await CoursesMetadata.LoadTrainingSPData(graphClient, this.SiteId);
            var memberIdsWithPendingCourse = new List<string>();

            // Convert user emails into AAD ids
            foreach (var userOnCourse in courseInfo.AllUsersAllCourses)
            {
                var usersResult = await graphClient.Users.Request().Filter($"userPrincipalName eq '{userOnCourse.User.Email}'").GetAsync();
                memberIdsWithPendingCourse.Add(usersResult.FirstOrDefault().Id);
            }

            // Install app to target users
            int existingAppInstallCount = _conversationCache.RefrenceCount;
            int newInstallationCount = 0;

            foreach (var memberIdWithPendingCourse in memberIdsWithPendingCourse)
            {
                // Check if present in App Conversation reference
                if (!_conversationCache.ContainsUserId(memberIdWithPendingCourse))
                {
                    // Perform installation for all the member whose conversation reference is not available.
                    await InstallTrainingBotForTarget(memberIdWithPendingCourse,
                        tenantId,
                        MicrosoftAppId,
                        MicrosoftAppPassword,
                        AppCatalogTeamAppId);

                    newInstallationCount++;
                }
            }

            return new InstallationCounts
            {
                Existing = existingAppInstallCount,
                New = newInstallationCount
            };
        }

        /// <summary>
        /// Install the app so we can get a conversation reference. 
        /// </summary>
        async Task InstallTrainingBotForTarget(string userid, string tenantId, string appId, string appPassword, string teamAppid)
        {
            string token = await GetToken(tenantId, appId, appPassword);
            var graphClient = GetAuthenticatedClient(token);

            try
            {
                var userScopeTeamsAppInstallation = new UserScopeTeamsAppInstallation
                {
                    AdditionalData = new Dictionary<string, object>()
                    {
                        {"teamsApp@odata.bind", "https://graph.microsoft.com/v1.0/appCatalogs/teamsApps/"+teamAppid}
                    }
                };
                await graphClient.Users[userid].Teamwork.InstalledApps
                    .Request()
                    .AddAsync(userScopeTeamsAppInstallation);
            }
            catch (ServiceException ex)
            {
                // This is where app is already installed but we don't have conversation reference.
                if (ex.Error.Code == "Conflict")
                {
                    await TriggerConversationUpdate(userid, tenantId, appId, appPassword);
                }
                else throw;
            }
        }

        async Task TriggerConversationUpdate(string userid, string tenantId, string appId, string appPassword)
        {
            string accessToken = await GetToken(tenantId, appId, appPassword);
            GraphServiceClient graphClient = GetAuthenticatedClient(accessToken);

            // Docs here: https://docs.microsoft.com/en-us/microsoftteams/platform/graph-api/proactive-bots-and-messages/graph-proactive-bots-and-messages#-retrieve-the-conversation-chatid
            var installedApps = await graphClient.Users[userid].Teamwork.InstalledApps
                .Request()
                .Filter($"teamsApp/externalId eq '{appId}'")
                .Expand("teamsAppDefinition")
                .GetAsync();

            var installedApp = installedApps.FirstOrDefault();

            if (installedApp != null)
                await graphClient.Users[userid].Teamwork.InstalledApps[installedApp.Id].Chat
                    .Request()
                    .GetAsync();

        }
    }
}
