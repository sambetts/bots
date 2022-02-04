using DigitalTrainingAssistant.Bot.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.UnitTests
{
    [TestClass]
    public class GraphHelperTests : BaseUnitTest
    {
        [TestMethod]
        public async Task PhotoTests()
        {
            var graphClient = await TestingUtils.GetClient(_configuration);

            var loader = new UserDataLoader(graphClient);

            // Test with user with image
            var adeleUserSearchResults = await graphClient.Users.Request().Filter("startswith(displayName,'Adele Vance')").GetAsync();
            Assert.IsTrue(adeleUserSearchResults.Count == 1);

            var base64 = await loader.GetUserPhotoBase64(adeleUserSearchResults[0].Id);

            Assert.IsNotNull(base64);


            var adminUserSearchResults = await graphClient.Users.Request().Filter("startswith(displayName,'MOD Admin')").GetAsync();
            Assert.IsTrue(adminUserSearchResults.Count == 1);

            var adminImg = await loader.GetUserPhotoBase64(adminUserSearchResults[0].Id);
            Assert.IsTrue(string.IsNullOrEmpty(adminImg));
        }
    }
}
