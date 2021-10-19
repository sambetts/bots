using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using ProactiveBot.Bots;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrainingOnboarding.Bot.Cards;
using TrainingOnboarding.Bot.Helpers;
using TrainingOnboarding.Models;

namespace TrainingOnboarding.Bot.Services
{
    public class CourseChecklistReminderService : BackgroundService
    {
        public readonly IConfiguration _configuration;
        private readonly BotHelper _helper;
        BotConversationCache _conversationCache = null;
        private readonly IBotFrameworkHttpAdapter _adapter;
        public string MicrosoftAppId { get; set; }
        public string MicrosoftAppPassword { get; set; }
        public string AppCatalogTeamAppId { get; set; }
        public string AppBaseUri { get; set; }
        public string TenantId { get; set; }
        public string SiteId { get; set; }

        public CourseChecklistReminderService(BotHelper helper, IConfiguration configuration, BotConversationCache botConversationCache, IBotFrameworkHttpAdapter adapter)
        {
            _helper = helper;
            _configuration = configuration;
            _conversationCache = botConversationCache;
            _adapter = adapter;
            this.AppBaseUri = configuration["AppBaseUri"];
            this.MicrosoftAppId = configuration["MicrosoftAppId"];
            this.MicrosoftAppPassword = configuration["MicrosoftAppPassword"];
            this.AppCatalogTeamAppId = configuration["AppCatalogTeamAppId"];
            this.TenantId = configuration["TenantId"];
            this.SiteId = configuration["SharePointSiteId"];

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var currentDateTime = DateTime.UtcNow;
                    Console.WriteLine($"Training bot Hosted Service is running at: {currentDateTime}.");

                    await this.CheckTrainingPlans();
                }
#pragma warning disable CA1031 // Catching general exceptions that might arise during execution to avoid blocking next run.
                catch (Exception ex)
#pragma warning restore CA1031 // Catching general exceptions that might arise during execution to avoid blocking next run.
                {
                    Console.WriteLine($"Error occurred while running learning plan notification service: {ex.Message}");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                    Console.WriteLine($"Learning plan notification service execution completed and will resume after {TimeSpan.FromDays(1)} delay.");
                }
            }
        }


        private async Task CheckTrainingPlans()
        {

            var token = await BaseHelper.GetToken(TenantId, MicrosoftAppId, MicrosoftAppPassword);
            var graphClient = BaseHelper.GetAuthenticatedClient(token);


            // Load all course data from lists
            var courseInfo = await CoursesMetadata.LoadTrainingSPData(graphClient, this.SiteId);

            var compareDate = DateTime.Now.AddDays(7);
            var coursesStartingInRange = courseInfo.Courses.Where(c => c.Start.HasValue && c.Start.Value < compareDate).ToList();


            var pendingTrainingActions = courseInfo.GetUserActionsWithThingsToDo();

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
                    await ((BotFrameworkAdapter)this._adapter).ContinueConversationAsync(MicrosoftAppId, previousConversationReference,
                        async (turnContext, cancellationToken) => await _helper.SendUserTrainingReminders(turnContext, cancellationToken, thisUserPendingActions), default);


                }

            }
        }
    }
}
