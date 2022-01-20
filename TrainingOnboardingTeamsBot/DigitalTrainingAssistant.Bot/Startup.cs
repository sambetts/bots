// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using DigitalTrainingAssistant.Bot;
using DigitalTrainingAssistant.Bot.Dialogues;
using DigitalTrainingAssistant.Bot.Services;
using DigitalTrainingAssistant.Bot.Helpers;
using Microsoft.Bot.Connector.Authentication;
using DigitalTrainingAssistant.Models;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public IConfiguration Configuration { get; set; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            var config = new BotConfig(Configuration);
            services.AddSingleton(config);
            services.AddApplicationInsightsTelemetry(config.AppInsights);

            services.AddControllersWithViews();

            services.AddControllers().AddNewtonsoftJson();

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // The Dialog that will be run by the bot.
            services.AddSingleton<MainDialog>();
            services.AddSingleton<UpdateProfileDialog>();


            // Create a global hashset for our ConversationReferences
            services.AddSingleton<BotActionsHelper>();
            services.AddSingleton<BotAppInstallHelper>();

            services.AddSingleton<BotConversationCache>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DigitalTrainingAssistantBot<MainDialog>>();

            services.AddHostedService<CourseChecklistReminderService>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                  {
                      endpoints.MapControllerRoute(
                      name: "default",
                      pattern: "{controller=Home}/{action=Index}/{id?}");
                  });
        }


    }
}
