﻿using AdaptiveCards;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrainingOnboarding.Bot.Models.Card;
using TrainingOnboarding.Models;

namespace TrainingOnboarding.Bot.Cards
{

    /// <summary>
    /// Class that helps to return introduction detail card as attachment.
    /// </summary>
    public class CourseWelcomeCard : BaseAdaptiveCard
    {
        public CourseWelcomeCard(string botName, Course course)
        {
            this.Course = course;
            this.BotName = botName;
        }

        public Course Course { get; set; }
        public string BotName { get; set; }

        public override string GetCardContent()
        {
            var json = Properties.Resources.CourseWelcome;

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_COURSE_NAME, this.Course.Name);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_BOT_NAME, this.BotName);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_TRAINER_NAME, Course.Trainer.Name);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_COURSE_INTRO_TEXT, Course.WelcomeMessage);

            return json;
        }
    }
}