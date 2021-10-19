
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Communications.Common.Telemetry;
using RecordingBot.Services.Bot;
using RecordingBot.Services.Contract;
using System;

namespace RecordingBot.Services.ServiceSetup
{
    /// <summary>
    /// Class ServiceHost.
    /// Implements the <see cref="RecordingBot.Services.Contract.IServiceHost" />
    /// </summary>
    /// <seealso cref="RecordingBot.Services.Contract.IServiceHost" />
    public class ServiceHost : IServiceHost
    {
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public IServiceCollection Services { get; private set; }
        /// <summary>
        /// Gets the service provider.
        /// </summary>
        /// <value>The service provider.</value>
        public IServiceProvider ServiceProvider { get; private set; }


        /// <summary>
        /// Configures the specified services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>ServiceHost.</returns>
        public ServiceHost Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IGraphLogger, GraphLogger>(_ => new GraphLogger("RecordingBot", redirectToTrace: true));
            services.Configure<AzureSettings>(configuration.GetSection(nameof(AzureSettings)));

            services.AddSingleton<IAzureSettings>(_ => _.GetRequiredService<IOptions<AzureSettings>>().Value);

            var config = (AzureSettings)services.BuildServiceProvider().GetRequiredService<IAzureSettings>();

            // App Insights logging. We're only interested in info msgs
            services.AddLogging(loggingBuilder => 
                loggingBuilder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("", LogLevel.Information));
            services.AddApplicationInsightsTelemetryWorkerService(new ApplicationInsightsServiceOptions
            {
                InstrumentationKey = config.ApplicationInsightsKey
            });

            services.AddSingleton<IBotService, BotService>();

            return this;
        }
    }
}
