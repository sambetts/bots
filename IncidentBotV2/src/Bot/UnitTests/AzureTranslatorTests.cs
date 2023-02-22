using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using TranslatorBot.Services.Media;

namespace UnitTests
{
    [TestClass]
    public class AzureTranslatorTests
    {
        [TestMethod]
        public async Task AzureTranslatorClient()
        {
            Assert.ThrowsException<ArgumentException>(()=> new AzureTranslatorClient(null, string.Empty, null, null));

            var key = ConfigurationManager.AppSettings["TranslatorConfigKey"];
            var url = ConfigurationManager.AppSettings["TranslatorConfigBaseUrl"];
            var region = ConfigurationManager.AppSettings["TranslatorConfigRegion"];

            var trace = LoggerFactory.Create(config =>
            {
                //config.AddConsole();
            }).CreateLogger<object>();

            // Test valid
            var c = new AzureTranslatorClient(url, key, region, trace);
            var helloTestResponse = await c.TranslateAsync("Hola, que tal", "es-ES", "En-GB");

            Assert.IsNotNull(helloTestResponse);
            Assert.IsTrue(helloTestResponse.Translations.Count == 1 && helloTestResponse.Translations[0].Text.ToLower().Contains("hello"));


            // Test nonsense
            var nowtResponse = await c.TranslateAsync("", "es-ES", "En-GB");
            Assert.IsTrue(nowtResponse.Translations.Count == 1 && nowtResponse.Translations[0].Text == string.Empty);

        }
    }
}
