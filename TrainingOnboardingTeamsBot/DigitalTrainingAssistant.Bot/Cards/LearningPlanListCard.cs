using AdaptiveCards;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using TrainingOnboarding.Bot.Models.Card;
using TrainingOnboarding.Models;

namespace TrainingOnboarding.Bot.Cards
{
    /// <summary>
    /// Class that helps to create learning plan list card.
    /// </summary>
    public class LearningPlanListCard : BaseAdaptiveCard
    {
        public LearningPlanListCard(IEnumerable<PendingUserActionsForCourse> actionsPending, Course course)
        {
            this.ActionsPending = actionsPending;
            this.Course = course;
        }

        public IEnumerable<PendingUserActionsForCourse> ActionsPending { get; set; }
        public Course Course { get; set; }


        public override string GetCardContent()
        {
            var checkBoxes = new List<AdaptiveElement>();
            var labels = new List<AdaptiveElement>();


            foreach (var action in ActionsPending)
            {
                foreach (var item in action.PendingItems)
                {
                    if (item.IsValid)
                    {
                        checkBoxes.Add(new AdaptiveToggleInput { Id = "chk-" + item.ID, Title = string.Empty });
                        labels.Add(new AdaptiveTextBlock { Id = "txt", Text = item.Requirement });
                    }
                }
            }
            var cols = new AdaptiveColumnSet
            {
                Columns = new List<AdaptiveColumn>
                {
                    new AdaptiveColumn{ Items = checkBoxes, Width="80px" },
                    new AdaptiveColumn{ Items = labels }
                }
            };

            var card = new CardWithButtons()
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Style = AdaptiveContainerStyle.Emphasis, Bleed = true, Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock($"Your outstanding tasks for '{Course.Name}'") { Size = AdaptiveTextSize.Medium, Weight = AdaptiveTextWeight.Bolder }
                        }
                    },
                    new AdaptiveContainer()
                    {
                        Bleed = true, Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock("Tell me what's done by selecting tasks and clicking the button below") { Size = AdaptiveTextSize.Medium }
                        }
                    },
                    cols
                },
                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveSubmitAction{ Title= "Set Tasks Complete", DataJson="{\"" + CardConstants.CardActionPropName + "\":\"" + CardConstants.CardActionValLearnerTasksDone + "\"}" }
                }
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(card);
        }
    }
}
