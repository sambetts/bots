using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrainingOnboarding.Bot.Cards;

namespace TrainingOnboarding.Bot.Models
{
    /// <summary>
    /// Json response from an adaptive card submit
    /// </summary>
    public class ActionResponse
    {
        [JsonProperty(CardConstants.CardActionPropName)]
        public string Action { get; set; }
    }
}
