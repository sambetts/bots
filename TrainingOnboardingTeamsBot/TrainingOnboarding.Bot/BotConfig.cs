using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrainingOnboarding.Bot
{
    public class BotConfig
    {
        public BotConfig(IConfiguration configuration)
        {
            this.AppInsights = configuration["APPINSIGHTS_CONNECTIONSTRING"];
            this.Storage = configuration["StorageConnectionString"];
            this.AppCatalogTeamAppId = configuration["AppCatalogTeamAppId"];
            this.SharePointSiteId = configuration["SharePointSiteId"];

            this.TenantId = configuration["TenantId"];
            this.AppBaseUri = configuration["AppBaseUri"];
            this.MicrosoftAppId = configuration["MicrosoftAppId"];
            this.MicrosoftAppPassword = configuration["MicrosoftAppPassword"];
            this.AppCatalogTeamAppId = configuration["AppCatalogTeamAppId"];
        }

        public string AppInsights { get; set; }
        public string Storage { get; set; }
        public string AppCatalogTeamAppId { get; set; }
        public string SharePointSiteId { get; set; }
        public string AppBaseUri { get; set; }
        public string MicrosoftAppId { get; set; }
        public string MicrosoftAppPassword { get; set; }
        public string TenantId { get; set; }
    }
}
