using AdaptiveCards;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrainingOnboarding.Bot.Cards
{
    public class BotWelcomeCard : BaseAdaptiveCard
    {
        public BotWelcomeCard(string botName)
        {
            this.BotName = botName;
        }

        public string BotName { get; set; }

        
        public override string GetCardContent()
        {
            var json = Properties.Resources.BotIntroduction;

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_BOT_NAME, this.BotName);

            return json;
        }
    }
}
