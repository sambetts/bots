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

    public class ActionResponseForSharePointItem
    {
        [JsonProperty(CardConstants.CardSharePointIdPropName)]
        public string SPID { get; set; }
    }

    public class IntroduceYourselfResponse : ActionResponseForSharePointItem
    {
        [JsonProperty("txtQAOrg")]
        public string Org { get; set; }

        [JsonProperty("txtQARole")]
        public string Role { get; set; }

        [JsonProperty("txtQACountry")]
        public string Country { get; set; }

        [JsonProperty("txtQASpareTimeActivities")]
        public string SpareTimeActivities { get; set; }

        [JsonProperty("txtQAMobilePhoneNumber")]
        public string MobilePhoneNumber { get; set; }


        public bool IsValid => !string.IsNullOrEmpty(Org) && !string.IsNullOrEmpty(Role) && !string.IsNullOrEmpty(Country) && !string.IsNullOrEmpty(SpareTimeActivities) && !string.IsNullOrEmpty(MobilePhoneNumber);
    }
}
