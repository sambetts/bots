using EasyTeams.Common.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyTeams.Tests
{
    public class TestObjects
    {
        public static NewConferenceCallRequest NewConferenceCallRequest 
        {
            get 
            {
                // Create test meeting
                var newConfCall = new NewConferenceCallRequest()
                {
                    Subject = "Test Meeting",
                    Start = DateTime.Now.AddHours(1),
                    OnBehalfOf = new MeetingContact("admin@M365x176143.onmicrosoft.com", false),
                    Recipients = new List<MeetingContact>()
                {
                    new MeetingContact("meganb@M365x176143.onmicrosoft.com", false)
                },
                };

                return newConfCall;
            }
        }
    }
}
