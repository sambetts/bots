using DigitalTrainingAssistant.Bot.Cards;
using DigitalTrainingAssistant.Bot.Dialogues;
using DigitalTrainingAssistant.Bot.Models;
using DigitalTrainingAssistant.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Helpers
{
    /// <summary>
    /// Bot functionality
    /// </summary>
    public class BotHelper : AuthHelper
    {
        #region Privates & Constructors

        private BotConversationCache _conversationCache = null;
        private ILogger<BotHelper> _logger = null;

        public BotHelper(BotConfig config, BotConversationCache botConversationCache, ILogger<BotHelper> logger)
        {
            this.Config = config;
            this._conversationCache = botConversationCache;
            _logger = logger;

            _logger.LogInformation($"Have config: AppBaseUri:{config.AppBaseUri}, MicrosoftAppId:{config.MicrosoftAppId}, AppCatalogTeamAppId:{config.AppCatalogTeamAppId}");
        }
        #endregion

        #region Properties

        public BotConfig Config { get; set; }


        #endregion

        /// <summary>
        /// App installed for user & now we have a conversation reference to cache for future chat threads.
        /// </summary>
        public async Task AddConversationReference(Activity activity)
        {
            var token = await AuthHelper.GetToken(activity.Conversation.TenantId, Config.MicrosoftAppId, Config.MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            var conversationReference = activity.GetConversationReference();
            await _conversationCache.AddOrUpdateUserAndConversationId(conversationReference, activity.ServiceUrl, graphClient);
        }

        public async Task SendCourseIntroAndTrainingRemindersToUser(CachedUserAndConversationData toUser, ITurnContext turnContext, CancellationToken cancellationToken, PendingUserActions userPendingActionsForCourse, GraphServiceClient graphClient)
        {
            // Send seperate card for each course with outstanding items
            foreach (var course in userPendingActionsForCourse.UniqueCourses)
            {
                // Get attendee info for this user
                var userAttendeeInfoForCourse = userPendingActionsForCourse.Actions
                    .Where(a => a.Course == course)
                        .Select(a => a.Attendee)
                        .Where(a => a.User.Email == toUser.EmailAddress).FirstOrDefault();

                // Send course intro?
                if (!userAttendeeInfoForCourse.BotContacted)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(new CourseWelcomeCard(BotConstants.BotName, course).GetCard()), cancellationToken);

                    // Don't send course intro twice to same user
                    userAttendeeInfoForCourse.BotContacted = true;
                    await userAttendeeInfoForCourse.SaveChanges(graphClient, Config.SharePointSiteId);
                }

                // Personal introduction needed?
                if (!userAttendeeInfoForCourse.IntroductionDone)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(new IntroduceYourselfCard(userAttendeeInfoForCourse).GetCard()), cancellationToken);
                }

                // Send outstanding course actions
                var actionsForCourse = userPendingActionsForCourse.Actions.Where(a => a.Course == course);
                var coursePendingItemsAttachment = new LearningPlanListCard(actionsForCourse, course).GetCard();

                await turnContext.SendActivityAsync(MessageFactory.Attachment(coursePendingItemsAttachment), cancellationToken);
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



        /// <summary>
        /// Someone replied via an Adaptive card form
        /// </summary>
        public async Task<DialogTurnResult> HandleCardResponse(WaterfallStepContext stepContext, string submitJson, CancellationToken cancellationToken)
        {
            // Form action
            ActionResponse r = null;

            try
            {
                r = JsonConvert.DeserializeObject<ActionResponse>(submitJson);
            }
            catch (JsonException)
            {
                return await ReplyWithNoIdeaAndEndDiag(stepContext, cancellationToken);
            }

            // Figure out what was done
            if (r.Action == CardConstants.CardActionValLearnerTasksDone)
            {
                var update = new CourseTasksUpdateInfo(submitJson, stepContext.Context.Activity.From.AadObjectId);
                await update.SendReply(stepContext.Context, cancellationToken, Config.MicrosoftAppId, Config.MicrosoftAppPassword, Config.SharePointSiteId);

            }
            else if (r.Action == CardConstants.CardActionValStartIntroduction)
            {
                var spAction = JsonConvert.DeserializeObject<ActionResponseForSharePointItem>(submitJson);

                var token = await AuthHelper.GetToken(stepContext.Context.Activity.Conversation.TenantId, Config.MicrosoftAppId, Config.MicrosoftAppPassword);
                var graphClient = AuthHelper.GetAuthenticatedClient(token);

                var attendanceInfo = await CourseAttendance.LoadById(graphClient, Config.SharePointSiteId, spAction.SPID);


                return await stepContext.BeginDialogAsync(nameof(UpdateProfileDialog), attendanceInfo, cancellationToken);

            }
            else if (r.Action == CardConstants.CardActionValSaveIntroductionQuestions)
            {
                var introductionData = JsonConvert.DeserializeObject<IntroduceYourselfResponse>(submitJson);

                var token = await AuthHelper.GetToken(stepContext.Context.Activity.Conversation.TenantId, Config.MicrosoftAppId, Config.MicrosoftAppPassword);
                var graphClient = AuthHelper.GetAuthenticatedClient(token);

                var attendanceInfo = await CourseAttendance.LoadById(graphClient, Config.SharePointSiteId, introductionData.SPID);
                if (introductionData.IsValid)
                {
                    // Save intro data
                    attendanceInfo.QACountry = introductionData.Country;
                    attendanceInfo.QAMobilePhoneNumber = introductionData.MobilePhoneNumber;
                    attendanceInfo.QAOrg = introductionData.Org;
                    attendanceInfo.QARole = introductionData.Role;
                    attendanceInfo.QASpareTimeActivities = introductionData.SpareTimeActivities;
                    attendanceInfo.IntroductionDone = true;

                    await attendanceInfo.SaveChanges(graphClient, Config.SharePointSiteId);

                    // Send back to user for now
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Saved. Now post it to the Team..."));

                    return null;
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                        $"Oops, that doesn't seem right - check the values & try again?"
                        ), cancellationToken);
                }
            }

            // Something else
            return await ReplyWithNoIdeaAndEndDiag(stepContext, cancellationToken);

        }

        private async Task<DialogTurnResult> ReplyWithNoIdeaAndEndDiag(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                    $"You sent me something but I can't work out what, sorry! Try again?."
                    ), cancellationToken);
            return await stepContext.EndDialogAsync(null);
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
            CoursesMetadata allTrainingData = null;
            try
            {
                allTrainingData = await CoursesMetadata.LoadTrainingSPData(graphClient, Config.SharePointSiteId);
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new BotSharePointAccessException();
                }
            }
            

            var coursesThisUserIsLeading = allTrainingData.Courses.Where(c => c.Trainer?.Email?.ToLower() == trainerEmail.ToLower()).ToList();

            var pendingTrainingActionsForCoursesThisUserIsTeaching = allTrainingData.GetUserActionsWithThingsToDo(coursesThisUserIsLeading, filterByCourseReminderDays);

            if (pendingTrainingActionsForCoursesThisUserIsTeaching.Actions.Count > 0)
            {
                // Send notification to all the members for this users classes
                foreach (var user in _conversationCache.GetCachedUsers())
                {

                    // Does this user have any training actions?
                    var thisUserPendingActions = pendingTrainingActionsForCoursesThisUserIsTeaching.GetActionsByEmail(user.EmailAddress);

                    try
                    {
                        await CheckIfUserHasActionsAndSendMessagesIfNeeded(user, thisUserPendingActions, botAdapter, graphClient, cancellationToken);
                    }
                    catch (ErrorResponseException)
                    {
                        await _conversationCache.RemoveFromCache(user.RowKey);
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
                    await InstallTrainingBotForTarget(user.Id,
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
