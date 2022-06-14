using DigitalTrainingAssistant.Models;

namespace DigitalTrainingAssistant.Bot.Cards
{
    public class BotIntroductionProactiveCard : BaseAdaptiveCard
    {
        public BotIntroductionProactiveCard(string botName)
        {
            this.BotName = botName;
        }

        public string BotName { get; set; }

        
        public override string GetCardContent()
        {
            var json = ReadResource(CardConstants.CardFileNameBotIntroductionProactive);

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_BOT_NAME, this.BotName);

            return json;
        }
    }
}
