using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using TrainingOnboarding.Bot.Helpers;
using TrainingOnboarding.Models;

namespace TrainingOnboarding.Bot.Services
{
    public class CourseChecklistReminderService : BackgroundService
    {
        private readonly BotHelper _helper;
        private readonly IBotFrameworkHttpAdapter _adapter;
        public BotConfig Config { get; set; }

        public CourseChecklistReminderService(BotHelper helper, BotConfig config, IBotFrameworkHttpAdapter adapter)
        {
            _helper = helper;
            _adapter = adapter;
            this.Config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
#if DEBUG
            return;     // Just aint got time for dat
#endif
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var currentDateTime = DateTime.UtcNow;
                    Console.WriteLine($"Training bot Hosted Service is running at: {currentDateTime}.");

                    await this.InstallAppAndCheckTrainingPlans(stoppingToken);
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


        async Task InstallAppAndCheckTrainingPlans(CancellationToken stoppingToken)
        {
            var token = await AuthHelper.GetToken(Config.TenantId, Config.MicrosoftAppId, Config.MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            // Load all course data from lists
            var courseInfo = await CoursesMetadata.LoadTrainingSPData(graphClient, Config.SiteId);

            // Trigger appropriate conversation updates
            var pendingTrainingActions = courseInfo.GetUserActionsWithThingsToDo(true);
            foreach (var courseWithStuffToDo in pendingTrainingActions.UniqueCourses)
            {
                await _helper.RemindClassMembersWithOutstandingTasks(((BotFrameworkAdapter)this._adapter), courseWithStuffToDo.Trainer.Email, Config.TenantId, stoppingToken, true);
            }
        }
    }
}
