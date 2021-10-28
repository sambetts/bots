using AdaptiveCards;
using EasyTeams.Common.BusinessLogic;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyTeams.Bot
{
    /// <summary>
    /// Generates adaptive cards from various objects
    /// </summary>
    public class CardGenerator
    {
        /// <summary>
        /// Show summary of conference call to be created.
        /// </summary>
        public static AdaptiveCard GetConferenceDetailsCard(NewConferenceCallRequest newConfCall)
        {
            var facts = new AdaptiveFactSet();
            int partipantNumber = 1;
            foreach (var recipient in newConfCall.Recipients)
            {
                facts.Facts.Add(new AdaptiveFact($"Participant {partipantNumber}", recipient.Email));
                partipantNumber++;
            }
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2))
            {
                Body = new List<AdaptiveElement>()
                    {
                        new AdaptiveContainer(){ Style = AdaptiveContainerStyle.Good, Bleed = true, Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock("Here's your Conference Call Details"){ Size = AdaptiveTextSize.Medium },
                            new AdaptiveTextBlock("Check it's all OK; we're about to make it happen."),
                        }},
                        new AdaptiveContainer(){Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock($"{newConfCall.Subject}") { Weight = AdaptiveTextWeight.Bolder },
                            new AdaptiveTextBlock($"{newConfCall.Start} ({newConfCall.TimeZoneName})")
                        }},
                        facts
                    },
            };

            return card;
        }

        /// <summary>
        /// Created Teams call summary
        /// </summary>
        internal static object GetTeamsCallDetailsCard(OnlineMeeting call)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2))
            {
                Body = new List<AdaptiveElement>()
                    {
                        new AdaptiveContainer(){ Style = AdaptiveContainerStyle.Good, Bleed = true, Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock("Teams Call Created"){ Size = AdaptiveTextSize.Medium }
                        }},
                        new AdaptiveTextBlock($"{call.StartDateTime.Value}") { Weight = AdaptiveTextWeight.Bolder },
                        new AdaptiveFactSet(){ Facts = new List<AdaptiveFact>()
                            {
                                new AdaptiveFact($"Attendees", call.Participants.Attendees.Count().ToString()),
                                new AdaptiveFact($"Contributors", call.Participants.Contributors.Count().ToString())
                            }
                        }
                    },
                Actions = new List<AdaptiveAction>() 
                {
                    new AdaptiveOpenUrlAction(){ Url = new Uri(call.JoinUrl), Title = "Join Call" }
                }
            };

            return card;
        }
    }
}
