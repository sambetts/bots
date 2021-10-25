using AdaptiveCards;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrainingOnboarding.Bot.Cards
{

    /// <summary>
    /// Class that helps to return introduction detail card as attachment.
    /// </summary>
    public static class IntroductionDetailCard
    {
        /// <summary>
        /// This method will construct the introduction detail card for hiring manager's team.
        /// </summary>
        /// <param name="applicationBasePath">Application base path to get the logo of the application.</param>
        /// <param name="localizer">The current culture's string localizer.</param>
        /// <param name="introductionEntity">Introduction entity.</param>
        /// <returns>Introduction detail card attachment.</returns>
        public static Attachment GetCard(string applicationBasePath)
        {

            var card = new AdaptiveCard(new AdaptiveSchemaVersion(CardConstants.AdaptiveCardVersion))
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveImage
                    {
                        Url = new Uri($"{applicationBasePath}/Artifacts/hiringManagerNotification.png"),
                        AltText = "Welcome to the training onboarding bot",
                    },
                },
            };
            card.Body.Add(
                new AdaptiveTextBlock
                {
                    Weight = AdaptiveTextWeight.Bolder,
                    Spacing = AdaptiveSpacing.Medium,
                    Text = "Hi, I'm the training onboarding bot. You are scheduled on one or more courses - please take time to prepare",
                    Wrap = true,
                });


            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
            };
        }
    }
}
