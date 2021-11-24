using AdaptiveCards;
using DigitalTrainingAssistant.Bot.Models.Card;
using DigitalTrainingAssistant.Models;
using System.Collections.Generic;

namespace DigitalTrainingAssistant.Bot.Cards
{
    /// <summary>
    /// Class that helps to create learning plan list card.
    /// </summary>
    public class PendingTasksListCard : BaseAdaptiveCard
    {
        public PendingTasksListCard(CourseAttendance userAttendeeInfoForCourse, IEnumerable<PendingUserActionsForCourse> actionsPending, Course course)
        {
            this.ActionsPending = actionsPending;
            this.Course = course;
            this.UserAttendeeInfoForCourse = userAttendeeInfoForCourse;
        }

        #region Props

        public IEnumerable<PendingUserActionsForCourse> ActionsPending { get; set; }
        public Course Course { get; set; }
        public CourseAttendance UserAttendeeInfoForCourse { get; set; }

        #endregion

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

            if (!UserAttendeeInfoForCourse.IntroductionDone)
            {
                card.Body.Insert(1, new AdaptiveContainer
                {
                    Items = new List<AdaptiveElement>()
                    {
                        new AdaptiveTextBlock{ Text= "Meet and greet your colleagues in the program" },
                        new AdaptiveActionSet{
                            Actions = new List<AdaptiveAction> {
                                new AdaptiveSubmitAction
                                { 
                                    Type = "Action.Submit", 
                                    Title = "Introduce Yourself",
                                    Data = new { action = "StartIntroduction", SPID = this.UserAttendeeInfoForCourse.ID }
                                }
                            }
                        }
                    }
                });
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(card);
        }
    }
}
