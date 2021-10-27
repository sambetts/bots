// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using TrainingOnboarding.Bot;
using TrainingOnboarding.Bot.Helpers;
using TrainingOnboarding.Bot.Services;

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

            // Create a global hashset for our ConversationReferences
            services.AddSingleton<BotHelper>();

            services.AddSingleton<BotConversationCache>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, TraingOnboardingBot>();

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
