using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using ProactiveBot.Bots;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrainingOnboarding.Bot.Cards;
using TrainingOnboarding.Bot.Helpers;
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
            var text = turnContext.Activity.Text.Trim().ToLower();

            if (text.Contains("install"))
            {
                // This to be scheduled
                var result = await _helper.InstallBotForCourseMembersAsync(turnContext, cancellationToken);
                await turnContext.SendActivityAsync(MessageFactory.Text($"Existing: {result.Existing} \n\n Newly Installed: {result.New}"), cancellationToken);
            }
            else if (text.Contains("send"))
            {
                int days = 31;
                var count = await _helper.SendNotificationToAllUsersWithCoursesStartingIn(turnContext, cancellationToken, days);
                await turnContext.SendActivityAsync(MessageFactory.Text($"Message sent: {count}"), cancellationToken);
            }
            else if (text.Contains("remind"))
            {
                var coursesFound = await _helper.RemindMyClassMembersWithOutstandingTasks(turnContext, cancellationToken);
                await turnContext.SendActivityAsync(MessageFactory.Text(
                    $"Found {coursesFound.Actions.Count} outstanding action(s) for {coursesFound.UniqueUsers.Count} user(s), " +
                    $"across {coursesFound.UniqueCourses.Count} course(s) that you are the trainer for. All users notified."
                    ), cancellationToken);
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {

            var token = await BaseHelper.GetToken(turnContext.Activity.Conversation.TenantId, _configuration["MicrosoftAppId"], _configuration["MicrosoftAppPassword"]);
            var graphClient = BaseHelper.GetAuthenticatedClient(token);


            // Load all course data from lists
            var courseInfo = await CoursesMetadata.LoadTrainingSPData(graphClient, _configuration["SharePointSiteId"]);

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    // Add current user to conversation reference.
                    await _helper.AddConversationReference(turnContext.Activity as Activity);

                    var user = _conversationCache.GetCachedUser(turnContext.Activity.GetConversationReference().User.AadObjectId);
                    var pendingTrainingActions = courseInfo.GetUserActionsWithThingsToDo().GetActionsByEmail(user.EmailAddress);
                    var introCardAttachment = IntroductionDetailCard.GetCard(_configuration["AppBaseUri"], pendingTrainingActions.Actions.Select(a=> a.Course));

                    await turnContext.SendActivityAsync(MessageFactory.Attachment(introCardAttachment));

                }
            }
        }

    }
}