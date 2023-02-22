
using Microsoft.Graph.Communications.Common.Telemetry;
using TranslatorBot.Model.Constants;
using TranslatorBot.Services.ServiceSetup;
using System.Net.Http;
using System.Web.Http;
using System.Net;

namespace TranslatorBot.Services.Http.Controllers
{
    /// <summary>
    /// Entry point for handling call-related web hook requests from Skype Platform.
    /// </summary>
    public class HealthController : ApiController
    {
        /// The logger
        /// </summary>
        private readonly IGraphLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformCallController" /> class.

        /// </summary>
        public HealthController()
        {
            _logger = AppHost.AppHostInstance.Resolve<IGraphLogger>();
        }

        /// <summary>
        /// Handle a callback for an incoming call.
        /// </summary>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpGet]
        [Route(HttpRouteConstants.HealthRoute)]
        public HttpResponseMessage Health()
        {
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}
