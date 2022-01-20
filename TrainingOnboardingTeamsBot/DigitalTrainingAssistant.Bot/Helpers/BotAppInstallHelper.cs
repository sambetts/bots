using DigitalTrainingAssistant.Models;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Helpers
{
    /// <summary>
    /// Bot functionality
    /// </summary>
    public class BotAppInstallHelper : AuthHelper
    {
        #region Privates & Constructors

        private BotConversationCache _conversationCache = null;
        private ILogger<BotActionsHelper> _logger = null;

        public BotAppInstallHelper(BotConfig config, BotConversationCache botConversationCache, ILogger<BotActionsHelper> logger)
        {
            this.Config = config;
            this._conversationCache = botConversationCache;
            _logger = logger;

            _logger.LogInformation($"Have config: AppBaseUri:{config.AppBaseUri}, MicrosoftAppId:{config.MicrosoftAppId}, AppCatalogTeamAppId:{config.AppCatalogTeamAppId}");
        }
        #endregion

        #region Properties

        public BotConfig Config { get; set; }


        #endregion


        /// <summary>
        /// Install the app so we can get a conversation reference. 
        /// </summary>
        public async Task InstallTrainingBotForUser(string userid, string tenantId, string appId, string appPassword, string teamAppid)
        {
            string token = await GetToken(tenantId, appId, appPassword);
            var graphClient = GetAuthenticatedClient(token);

            try
            {
                var userScopeTeamsAppInstallation = new UserScopeTeamsAppInstallation
                {
                    AdditionalData = new Dictionary<string, object>()
                    {
                        {"teamsApp@odata.bind", "https://graph.microsoft.com/v1.0/appCatalogs/teamsApps/"+teamAppid}
                    }
                };
                await graphClient.Users[userid].Teamwork.InstalledApps
                    .Request()
                    .AddAsync(userScopeTeamsAppInstallation);
            }
            catch (ServiceException ex)
            {
                // This is where app is already installed but we don't have conversation reference.
                if (ex.Error.Code == "Conflict")
                {
                    await TriggerUserConversationUpdate(userid, tenantId, appId, appPassword);
                }
                else if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new BotConfigException($"Teams app ID '{Config.AppCatalogTeamAppId}' doesn't seem to exist");
                }
                else throw;
            }
        }

        public async Task InstallTrainingBotForTeam(string teamId, string tenantId, string appId, string appPassword, string teamAppid)
        {
            string token = await GetToken(tenantId, appId, appPassword);
            var graphClient = GetAuthenticatedClient(token);

            try
            {
                var userScopeTeamsAppInstallation = new UserScopeTeamsAppInstallation
                {
                    AdditionalData = new Dictionary<string, object>()
                    {
                        {"teamsApp@odata.bind", "https://graph.microsoft.com/v1.0/appCatalogs/teamsApps/"+teamAppid}
                    }
                };
                await graphClient.Teams[teamId].InstalledApps
                    .Request()
                    .AddAsync(userScopeTeamsAppInstallation);
            }
            catch (ServiceException ex)
            {
                // This is where app is already installed but we don't have conversation reference.
                if (ex.Error.Code == "Conflict")
                {
                    //await TriggerUserConversationUpdate(teamId, tenantId, appId, appPassword);
                }
                else throw;
            }
        }

        async Task TriggerUserConversationUpdate(string userid, string tenantId, string appId, string appPassword)
        {
            string accessToken = await GetToken(tenantId, appId, appPassword);
            GraphServiceClient graphClient = GetAuthenticatedClient(accessToken);

            // Docs here: https://docs.microsoft.com/en-us/microsoftteams/platform/graph-api/proactive-bots-and-messages/graph-proactive-bots-and-messages#-retrieve-the-conversation-chatid
            var installedApps = await graphClient.Users[userid].Teamwork.InstalledApps
                .Request()
                .Filter($"teamsApp/externalId eq '{appId}'")
                .Expand("teamsAppDefinition")
                .GetAsync();

            var installedApp = installedApps.FirstOrDefault();

            if (installedApp != null)
                await graphClient.Users[userid].Teamwork.InstalledApps[installedApp.Id].Chat
                    .Request()
                    .GetAsync();
            else
            {
                throw new ArgumentOutOfRangeException(nameof(appId), $"Can't find Teams app with id {appId} to trigger a conversation for");
            }

        }
    }
}
