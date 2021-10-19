using Microsoft.Bot.Schema;
using Microsoft.Teams.Apps.NewHireOnboarding.Models.Card;
using System;
using System.Collections.Generic;
using TrainingOnboarding.Models;

namespace TrainingOnboarding.Bot.Cards
{
    /// <summary>
    /// Class that helps to create learning plan list card.
    /// </summary>
    public static class LearningPlanListCard
    {
        public static Attachment GetLearningPlanListCard(IEnumerable<PendingUserActionsForCourse> actionsPending, string cardTitle, string applicationBasePath)
        {
            actionsPending = actionsPending ?? throw new ArgumentNullException(nameof(actionsPending));

            var card = new ListCard
            {
                Title = cardTitle,
                Items = new List<ListCardItem>(),
                Buttons = new List<ListCardButton>(),
            };

            foreach (var action in actionsPending)
            {
                foreach (var item in action.PendingItems)
                {
                    card.Items.Add(new ListCardItem
                    {
                        Type = "resultItem",
                        Id = Guid.NewGuid().ToString(),
                        Title = action.Course.Name,
                        Subtitle = item.Requirement,
                        Icon = $"{applicationBasePath}/Artifacts/listCardDefaultImage.png",
                        Tap = new ListCardItemEvent
                        {
                            Type = CardConstants.OpenUrlType,
                            Value = "https://m365x352268.sharepoint.com/sites/TrainingHQ/"
                        },
                    });
                }
                
            }

            var viewCompletePlanActionButton = new ListCardButton()
            {
                Title = "Go to Tasks List",
                Type = CardConstants.OpenUrlType,
                Value = $"https://m365x352268.sharepoint.com/sites/TrainingHQ/",
            };

            card.Buttons.Add(viewCompletePlanActionButton);

            return new Attachment
            {
                ContentType = CardConstants.ListCardContentType,
                Content = card,
            };
        }
    }
}
