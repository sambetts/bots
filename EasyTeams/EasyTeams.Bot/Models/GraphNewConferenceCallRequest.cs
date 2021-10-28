using EasyTeams.Common.BusinessLogic;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyTeams.Bot
{
    /// <summary>
    /// New conference request details + OAuth token
    /// </summary>
    public class GraphNewConferenceCallRequest : NewConferenceCallRequest
    {

        public TokenResponse OAuthToken { get; set; }
    }
}
