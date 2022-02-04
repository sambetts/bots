using DigitalTrainingAssistant.Models;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.UnitTests
{
    internal class TestingUtils
    {

        public static async Task<GraphServiceClient> GetClient(TestConfig configuration)
        {
            var token = await AuthHelper.GetToken(configuration.TenantId, configuration.MicrosoftAppId, configuration.MicrosoftAppPassword);
            return AuthHelper.GetAuthenticatedClient(token);
        }
    }
}
