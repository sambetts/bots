using AdaptiveCards;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Cards
{
    public abstract class BaseAdaptiveCard
    {

        public abstract string GetCardContent();

        internal string ReplaceVal(string json, string fieldName, string val)
        {
            json = json.Replace(fieldName, val);

            return json;
        }

        public Attachment GetCard()
        {
            dynamic cardJson = JsonConvert.DeserializeObject(this.GetCardContent());

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = cardJson,
            };
        }
    }
}
