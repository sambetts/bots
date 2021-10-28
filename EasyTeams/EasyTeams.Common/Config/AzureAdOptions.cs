using Microsoft.Extensions.Configuration;

namespace EasyTeams.Common.Config
{
    public class AzureAdOptions
    {

        public AzureAdOptions()
        {
        }
        public AzureAdOptions(IConfiguration configuration) :this()
        {
            configuration.Bind("AzureAd", this);
        }


        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Instance { get; set; }

        public string Domain { get; set; }

        public string TenantId { get; set; }

        public string CallbackPath { get; set; }
        public string RedirectURL { get; set; }

        public string ApiIDURL { get; set; }

        /// <summary>
        /// Authority delivering the token for your tenant
        /// </summary>
        public string Authority
        {
            get
            {
                return $"{Instance}{TenantId}";
            }
        }
    }
}
