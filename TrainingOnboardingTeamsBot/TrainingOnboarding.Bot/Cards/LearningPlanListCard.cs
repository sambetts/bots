using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.Teams.Apps.NewHireOnboarding.Models.Card;
using System;
using System.Collections.Generic;
using TrainingOnboarding.Bot.Models.Card;
using TrainingOnboarding.Models;

namespace TrainingOnboarding.Bot.Cards
{
    /// <summary>
    /// Class that helps to create learning plan list card.
    /// </summary>
    public static class LearningPlanListCard
    {

        public static Attachment GetLearningPlanListCard(IEnumerable<PendingUserActionsForCourse> actionsPending, Course course)
        {
            actionsPending = actionsPending ?? throw new ArgumentNullException(nameof(actionsPending));

            var checkBoxes = new List<AdaptiveElement>();
            var labels = new List<AdaptiveElement>();


            foreach (var action in actionsPending)
            {
                foreach (var item in action.PendingItems)
                {
                    checkBoxes.Add(new AdaptiveToggleInput { Id = "chk-" + item.ID });
                    labels.Add(new AdaptiveTextBlock { Id = "txt", Text = item.Requirement });
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
                            new AdaptiveTextBlock($"Your outstanding tasks for '{course.Name}'") { Size = AdaptiveTextSize.Medium, Weight = AdaptiveTextWeight.Bolder }
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


            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = card,
            };
        }

        public static Attachment GetCourseWelcome(Course course)
        {
            var intoText = course.WelcomeMessage ?? "Please do the things";
            var card = new CardWithButtons()
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveContainer()
                    {
                        Style = AdaptiveContainerStyle.Emphasis, Bleed = true, Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock($"Welcome to '{course.Name}'") { Size = AdaptiveTextSize.Medium, Weight = AdaptiveTextWeight.Bolder }
                        }
                    },
                    new AdaptiveContainer()
                    {
                        Bleed = true, Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock(intoText) { Size = AdaptiveTextSize.Medium }
                        }
                    }
                }
            };


            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = card,
            };
        }
    }
}
