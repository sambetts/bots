using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using ProactiveBot.Bots;
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
    public class BotHelper : BaseHelper
    {
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
        /// <param name="activity"></param>
        public async Task AddConversationReference(Activity activity)
        {

            var token = await BaseHelper.GetToken(activity.Conversation.TenantId, MicrosoftAppId, MicrosoftAppPassword);
            var graphClient = BaseHelper.GetAuthenticatedClient(token);

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

            var token = await BaseHelper.GetToken(turnContext.Activity.Conversation.TenantId, MicrosoftAppId, MicrosoftAppPassword);
            var graphClient = BaseHelper.GetAuthenticatedClient(token);


            // Load all course data from lists
            var courseInfo = await CoursesMetadata.LoadTrainingSPData(graphClient, this.SiteId);

            var compareDate = DateTime.Now.AddDays(days);
            var coursesStartingInRange = courseInfo.Courses.Where(c => c.Start.HasValue && c.Start.Value < compareDate).ToList();


            var pendingTrainingActions = courseInfo.GetUserActionsWithThingsToDo();


            // Send notification to all the members
            foreach (var user in _conversationCache.GetCachedUsers())
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
                    await turnContext.Adapter.ContinueConversationAsync(MicrosoftAppId, previousConversationReference,
                        async (turnContext, cancellationToken) => await SendUserTrainingReminders(turnContext, cancellationToken, thisUserPendingActions), cancellationToken);
                }

                msgSentCount++;
            }

            return msgSentCount;
        }

        internal async Task<PendingUserActions> RemindMyClassMembersWithOutstandingTasks(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var token = await BaseHelper.GetToken(turnContext.Activity.Conversation.TenantId, MicrosoftAppId, MicrosoftAppPassword);
            var graphClient = BaseHelper.GetAuthenticatedClient(token);

            var conversationReference = turnContext.Activity.GetConversationReference();

            // Ensure calling user is fully cached
            await _conversationCache.AddOrUpdateUserAndConversationId(conversationReference, turnContext.Activity.ServiceUrl, graphClient);

            var userTalkingEmail = _conversationCache.GetCachedUsers().Where(u => u.RowKey == conversationReference.User.AadObjectId).SingleOrDefault();

            return await RemindMyClassMembersWithOutstandingTasks(turnContext.Adapter, userTalkingEmail, turnContext.Activity.Conversation.TenantId, cancellationToken);
        }


        internal async Task<PendingUserActions> RemindMyClassMembersWithOutstandingTasks(BotAdapter botAdapter, CachedUserData trainer, string tenantId, CancellationToken cancellationToken)
        {
            var token = await BaseHelper.GetToken(tenantId, MicrosoftAppId, MicrosoftAppPassword);
            var graphClient = BaseHelper.GetAuthenticatedClient(token);


            // Load all course data from lists
            var courseInfo = await CoursesMetadata.LoadTrainingSPData(graphClient, this.SiteId);

            var coursesThisUserIsLeading = courseInfo.Courses.Where(c => c.Trainer?.Email?.ToLower() == trainer.EmailAddress.ToLower()).ToList();

            var pendingTrainingActionsForCoursesThisUserIsTeaching = courseInfo.GetUserActionsWithThingsToDo(coursesThisUserIsLeading);

            if (pendingTrainingActionsForCoursesThisUserIsTeaching.Actions.Count > 0)
            {
                // Send notification to all the members for this users classes
                foreach (var user in _conversationCache.GetCachedUsers())
                {
                    // Does this user have any training actions?
                    var thisUserPendingActions = pendingTrainingActionsForCoursesThisUserIsTeaching.GetActionsByEmail(user.EmailAddress);
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
                            async (turnContext, cancellationToken) => await SendUserTrainingReminders(turnContext, cancellationToken, thisUserPendingActions), cancellationToken);
                    }

                }
            }

            return pendingTrainingActionsForCoursesThisUserIsTeaching;
        }

        public async Task SendUserTrainingReminders(ITurnContext turnContext, CancellationToken cancellationToken, PendingUserActions thisUserPendingActions)
        {
            foreach (var course in thisUserPendingActions.UniqueCourses)
            {
                var listCardAttachment = LearningPlanListCard.GetLearningPlanListCard(thisUserPendingActions.Actions.Where(a=> a.Course == course),
                    !string.IsNullOrEmpty(course.WelcomeMessage) ? course.WelcomeMessage : "Your outstanding training tasks", AppBaseUri);
                await turnContext.SendActivityAsync(MessageFactory.Attachment(listCardAttachment));
            }

            await turnContext.SendActivityAsync($"You have {thisUserPendingActions.Actions.SelectMany(a=> a.PendingItems).Count()} training actions to complete.");
            Console.WriteLine("Sent training reminders");
        }

        public async Task<InstallationCounts> InstallBotForCourseMembersAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            var token = await BaseHelper.GetToken(turnContext.Activity.Conversation.TenantId, MicrosoftAppId, MicrosoftAppPassword);
            var graphClient = BaseHelper.GetAuthenticatedClient(token);

            var courseInfo = await CoursesMetadata.LoadTrainingSPData(graphClient, this.SiteId);
            var memberIdsWithPendingCourse = new List<string>();

            // Convert user emails into AAD ids
            foreach (var userOnCourse in courseInfo.AllUsersAllCourses)
            {
                var usersResult = await graphClient.Users.Request().Filter($"userPrincipalName eq '{userOnCourse.Email}'").GetAsync();
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
                        turnContext.Activity.Conversation.TenantId,
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
