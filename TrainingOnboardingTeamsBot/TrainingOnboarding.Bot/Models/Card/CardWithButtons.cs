using AdaptiveCards;
using Microsoft.Teams.Apps.NewHireOnboarding.Models.Card;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrainingOnboarding.Bot.Models.Card
{
    public class CardWithButtons : AdaptiveCard
    {
        public CardWithButtons() : base("1.3")
        { 
        }

        /// <summary>
        /// Gets or sets action buttons on list card.
        /// </summary>
        [JsonProperty("buttons")]
#pragma warning disable CA2227 // Getting error to make collection property as read only but needs to assign values.
        public List<ListCardButton> Buttons { get; set; } = new List<ListCardButton>();
    }
}
