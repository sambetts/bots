using EasyTeams.Common.BusinessLogic;
using EasyTeams.Common.Config;
using EasyTeamsBot.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace EasyTeams.Tests
{
    [TestClass]
    public class TeamsManagerTests
    {
        [TestMethod]
        public async Task AppIndentityTeamsManagerUsers()
        {
            TeamsManager manager = new AppIndentityTeamsManager(Settings);

            // We can't create online meetings with app identities, so just read all users
            var allUsers = await manager.GetInternalParticipants(TestObjects.NewConferenceCallRequest);
            AssertParticipants(allUsers);
        }
        [TestMethod]
        public async Task PrecachedAuthTokenTeamsManagerTests()
        {

            var app = ConfidentialClientApplicationBuilder.Create(Settings.AzureAdOptions.ClientId)
                .WithTenantId(Settings.AzureAdOptions.TenantId)
                .WithRedirectUri(Settings.AzureAdOptions.RedirectURL)
                .WithClientSecret(Settings.AzureAdOptions.ClientSecret)
                .Build();

            ClientCredentialProvider authProvider = new ClientCredentialProvider(app);
            var msg = new HttpRequestMessage();
            await authProvider.AuthenticateRequestAsync(msg);
            string token = msg.Headers.Authorization.Parameter;
            var client = new PrecachedAuthTokenTeamsManager(token, Settings);

            var particpants = await client.GetInternalParticipants(TestObjects.NewConferenceCallRequest);
            AssertParticipants(particpants);
        }

        private void AssertParticipants(List<User> particpants)
        {
            Assert.IsNotNull(particpants);
            Assert.IsTrue(particpants.Count == 2);
        }

        SystemSettings _settings = null;
        public SystemSettings Settings 
        { 
            get
            {
                if (_settings == null)
                {

                    IConfiguration config = new ConfigurationBuilder()
                      .AddJsonFile("appsettings.json", false, true)
                      .Build();
                    _settings = new SystemSettings(config);
                }
                return _settings;
            } 
        }
    }
}
