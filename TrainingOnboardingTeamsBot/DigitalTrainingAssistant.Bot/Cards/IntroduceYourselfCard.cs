using DigitalTrainingAssistant.Models;

namespace DigitalTrainingAssistant.Bot.Cards
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
            var json = ReadResource(CardConstants.CardFileNameIntroduceYourself);

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_SHAREPOINT_ID, this.InfoToUpdate.ID.ToString());


            return json;
        }
    }
}
