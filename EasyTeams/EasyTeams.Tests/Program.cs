using EasyTeams.Common.BusinessLogic;
using EasyTeams.Common.Config;
using EasyTeamsBot.Common;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.UnitTests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World! This is a console app for testing whatever isn't working. Shouldn't be run normally.");
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", false, true)
              .Build();
            SystemSettings systemSettings = new SystemSettings(config, true);

            await DoThings(systemSettings);
        }

        private async static Task DoThings(SystemSettings systemSettings)
        {
            UserDelegatedTeamsManager manager = new UserDelegatedTeamsManager(systemSettings);

            // Create test meeting
            var newConfCall = new NewConferenceCallRequest()
            {
                Subject = "Test Meeting",
                Start = DateTime.Now.AddHours(1),
                OnBehalfOf = new MeetingContact("meganb@M365x176143.onmicrosoft.com", false),
                Recipients = new List<MeetingContact>()
                {
                    new MeetingContact("admin@M365x176143.onmicrosoft.com", false),
                    new MeetingContact("someguy@contoso.onmicrosoft.com", true)
                },
                TimeZoneName = TimeZoneInfo.Local.Id,
                MinutesLong = 30
            };

            var meeting = await manager.CreateNewConferenceCall(newConfCall, false);

            string json = JsonConvert.SerializeObject(meeting, Formatting.Indented);


            Console.WriteLine(json);
        }
    }


}
