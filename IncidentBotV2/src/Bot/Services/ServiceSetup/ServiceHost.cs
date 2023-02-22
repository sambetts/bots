// ***********************************************************************
// Assembly         : TranslatorBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : bcage29
// Last Modified On : 02-28-2022
// ***********************************************************************
// <copyright file="ServiceHost.cs" company="Microsoft">
//     Copyright ©  2020
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Communications.Common.Telemetry;
using TranslatorBot.Services.Bot;
using TranslatorBot.Services.Contract;
using System;
using TranslatorBot.Services.Http;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace TranslatorBot.Services.ServiceSetup
{
    /// <summary>
    /// Class ServiceHost.
    /// Implements the <see cref="TranslatorBot.Services.Contract.IServiceHost" />
    /// </summary>
    /// <seealso cref="TranslatorBot.Services.Contract.IServiceHost" />
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
        /// <param name="channel"></param>
        /// <returns>ServiceHost.</returns>
        public ServiceHost Configure(IServiceCollection services, IConfiguration configuration, ITelemetryChannel channel)
        {
            var appSettings = configuration.GetSection(nameof(AzureSettings));
            services.AddSingleton<IGraphLogger, GraphLogger>(_ => new GraphLogger("TranslatorBot", redirectToTrace: true));
            services.Configure<AppSettings>(appSettings);
            services.AddSingleton<IAzureSettings, AzureSettings>();
            services.AddSingleton<IBotService, BotService>();
            services.AddSingleton<IBotMediaLogger, BotMediaLogger>();

            services.Configure<TelemetryConfiguration>(config => config.TelemetryChannel = channel);
            services.AddLogging(build =>
            {
                var appInsightsKey = appSettings[nameof(AppSettings.AppInsightsInstrumentationKey)];
                if (!string.IsNullOrEmpty(appInsightsKey))
                {
                    build.AddApplicationInsights(ops => ops.ConnectionString = $"InstrumentationKey={appInsightsKey}",
                        options => options.TrackExceptionsAsExceptionTelemetry = false);
                }

                if (bool.TryParse(appSettings[nameof(AppSettings.UseLocalDevSettings)], out _))
                {
                    build.AddConsole();
                }
            });

            return this;
        }

        /// <summary>
        /// Builds this instance.
        /// </summary>
        /// <returns>IServiceProvider.</returns>
        public IServiceProvider Build()
        {
            ServiceProvider = Services.BuildServiceProvider();
            return ServiceProvider;
        }
    }
}
