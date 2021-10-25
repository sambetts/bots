using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrainingOnboarding.Bot.Cards;
using TrainingOnboarding.Bot.Helpers;
using TrainingOnboarding.Bot.Models;
using TrainingOnboarding.Models;

namespace TrainingOnboarding.Bot
{
    public class TraingOnboardingBot : TeamsActivityHandler
    {
        public readonly IConfiguration _configuration;
        private readonly BotHelper _helper;

        BotConversationCache _conversationCache = null;

        public TraingOnboardingBot(BotHelper helper, IConfiguration configuration, BotConversationCache botConversationCache)
        {
            _helper = helper;
            _configuration = configuration;
            _conversationCache = botConversationCache;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            turnContext.Activity.RemoveRecipientMention();
            var text = !string.IsNullOrEmpty(turnContext.Activity.Text?.Trim().ToLower()) ? turnContext.Activity.Text?.Trim().ToLower() : string.Empty;
            var val = turnContext.Activity.Value;

            if (string.IsNullOrEmpty(text) && val != null)
            {
                await HandleCardResponse(val.ToString(), turnContext, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(text))
            {
                await HandleUserTextChat(text, turnContext, cancellationToken);

            }
        }

        private async Task HandleUserTextChat(string text, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (text.Contains("install"))
            {
                // This happens in CourseChecklistReminderService too
                var result = await _helper.InstallBotForCourseMembersAsync(turnContext.Activity.Conversation.TenantId);
                await turnContext.SendActivityAsync(MessageFactory.Text($"Existing: {result.Existing} \n\n Newly Installed: {result.New}"), cancellationToken);
            }
            else if (text.Contains("send"))
            {
                // This needs moving
                int days = 31;
                var count = await _helper.SendNotificationToAllUsersWithCoursesStartingIn(turnContext, cancellationToken, days);
                await turnContext.SendActivityAsync(MessageFactory.Text($"Message sent: {count}"), cancellationToken);
            }
            else if (text.Contains("remind"))
            {
                var coursesFound = await _helper.RemindClassMembersWithOutstandingTasks(turnContext, cancellationToken);

                await turnContext.SendActivityAsync(MessageFactory.Text(
                    $"Found {coursesFound.Actions.Count} outstanding action(s) for {coursesFound.UniqueUsers.Count} user(s), " +
                    $"across {coursesFound.UniqueCourses.Count} course(s) that you are the trainer for. All users notified."
                    ), cancellationToken);
            }
        }

        /// <summary>
        /// Someone replied via an Adaptive card form
        /// </summary>
        private async Task HandleCardResponse(string json, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Form action
            ActionResponse r = null;

            try
            {
                r = JsonConvert.DeserializeObject<ActionResponse>(json);
            }
            catch (JsonException)
            {
                return;
            }

            // Figure out what was done
            if (r.Action == CardConstants.CardActionValLearnerTasksDone)
            {
                var update = new CourseTasksUpdateInfo
                {
                    UserAadObjectId = turnContext.Activity.From.AadObjectId
                };

                // Enum the JSon dynamically to discover properties
                var d = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
                foreach (var item in d)
                {
                    if (item.Key != null && item.Key.StartsWith("chk-"))
                    {
                        var requirementdIdString = item.Key.TrimStart("chk-".ToCharArray());
                        var requirementdId = 0;
                        var done = false;
                        bool.TryParse(item.Value, out done);
                        int.TryParse(requirementdIdString, out requirementdId);
                        if (done && requirementdId != 0)
                        {
                            update.ConfirmedTaskIds.Add(requirementdId);
                        }
                    }
                }
                if (update.HasChanges)
                {
                    var token = await AuthHelper.GetToken(turnContext.Activity.Conversation.TenantId, _configuration["MicrosoftAppId"], _configuration["MicrosoftAppPassword"]);
                    var graphClient = AuthHelper.GetAuthenticatedClient(token);

                    // Save to SP list
                    var updateCount = await update.SaveChanges(graphClient, _configuration["SharePointSiteId"]);

                    await turnContext.SendActivityAsync(MessageFactory.Text(
                        $"Updated {updateCount} tasks as complete - thanks for getting ready!"
                    ), cancellationToken);
                }
                else
                {

                    await turnContext.SendActivityAsync(MessageFactory.Text(
                        $"Nothing finished from this list?"
                    ), cancellationToken);
                }
            }

        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var token = await AuthHelper.GetToken(turnContext.Activity.Conversation.TenantId, _configuration["MicrosoftAppId"], _configuration["MicrosoftAppPassword"]);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            // Load all course data from lists
            var courseInfo = await CoursesMetadata.LoadTrainingSPData(graphClient, _configuration["SharePointSiteId"]);

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    // Add current user to conversation reference.
                    await _helper.AddConversationReference(turnContext.Activity as Activity);

                    // Now figure out if user needs to do something
                    var user = _conversationCache.GetCachedUser(turnContext.Activity.GetConversationReference().User.AadObjectId);
                    var pendingTrainingActions = courseInfo.GetUserActionsWithThingsToDo().GetActionsByEmail(user.EmailAddress);

                    // Send bot intro if they're on a course
                    if (pendingTrainingActions.Actions.Count > 0)
                    {
                        var introCardAttachment = new BotWelcomeCard(BotConstants.BotName).GetCard();
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(introCardAttachment));

                        // Send outstanding tasks
                        await _helper.SendCourseIntroAndTrainingRemindersToUser(user, turnContext, cancellationToken, pendingTrainingActions, graphClient);
                    }
                }
            }
        }
    }
}