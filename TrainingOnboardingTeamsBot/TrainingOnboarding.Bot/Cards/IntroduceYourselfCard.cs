using AdaptiveCards;
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
    public class IntroduceYourselfCard : BaseAdaptiveCard
    {

        public IntroduceYourselfCard(CourseAttendance attendanceInfo)
        {
            this.InfoToUpdate = attendanceInfo;
        }
        public CourseAttendance InfoToUpdate { get; set; }

        public override string GetCardContent()
        {
            var json = Properties.Resources.IntroduceYourself;

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_SHAREPOINT_ID, this.InfoToUpdate.ID.ToString());


            return json;
        }
    }
}
