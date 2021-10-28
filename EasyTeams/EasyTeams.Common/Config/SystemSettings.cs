using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;

namespace EasyTeams.Common.Config
{
    /// <summary>
    /// Settings used for solution
    /// </summary>
    public class SystemSettings
    {
        #region Constructors

        public SystemSettings(IConfiguration config) : this(config, true)
        {
        }
        public SystemSettings(IConfiguration config, bool validateNullConfig)
        {
            var appSettingsConfigSection = config.GetSection("AppSettings");

            // Set config
            this.NewEventCreationURL = appSettingsConfigSection["NewEventCreationURL"];
            this.FunctionAppKey = appSettingsConfigSection["FunctionAppKey"];

            AzureAdOptions = new AzureAdOptions(config);

            // Mak sure we got everything
            if (validateNullConfig)
            {
                VerifyConfigValues(
                    new string[]
                    {
                        NewEventCreationURL
                    });
            }

        }
        #endregion

        #region Config Verification

        static void VerifyConfigValues(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.IsNullOrEmpty(args[i]))
                {
                    ThrowConfigException();
                }
            }
        }
        private static void ThrowConfigException()
        {
            throw new ApplicationException("Missing configuration values");
        }
        #endregion

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string NewEventCreationURL { get; set; }
        public AzureAdOptions AzureAdOptions { get; set; }
        public string FunctionAppKey { get; set; }

    }
}
