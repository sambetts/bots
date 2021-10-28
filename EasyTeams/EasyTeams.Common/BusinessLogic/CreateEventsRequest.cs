using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyTeams.Common.BusinessLogic
{
    /// <summary>
    /// Request for function-app to create calendar events
    /// </summary>
    public class CreateEventsRequest
    {
        public NewConferenceCallRequest Request { get; set; }
        public OnlineMeeting Meeting { get; set; }

    }
}
