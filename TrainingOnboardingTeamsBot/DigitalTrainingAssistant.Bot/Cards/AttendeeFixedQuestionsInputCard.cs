using DigitalTrainingAssistant.Models;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace DigitalTrainingAssistant.Bot.Cards
{

    /// <summary>
    /// Class that helps to return introduction detail card as attachment.
    /// </summary>
    public class AttendeeFixedQuestionsInputCard : BaseAdaptiveCard
    {
        public AttendeeFixedQuestionsInputCard(CourseAttendance attendanceInfo)
        {
            this.InfoToUpdate = attendanceInfo;
        }
        public CourseAttendance InfoToUpdate { get; set; }

        public override string GetCardContent()
        {
            var json = Properties.Resources.AttendeeFixedQuestionsInput;

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_ATTENDEE_NAME, this.InfoToUpdate.User.Name);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_ATTENDEE_EMAIL, this.InfoToUpdate.User.Email);

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QARole, this.InfoToUpdate.QARole);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QAOrg, this.InfoToUpdate.QAOrg);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QACountry, this.InfoToUpdate.QACountry);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QASpareTimeActivities, this.InfoToUpdate.QASpareTimeActivities);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QAMobilePhoneNumber, this.InfoToUpdate.QAMobilePhoneNumber);

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_SHAREPOINT_ID, this.InfoToUpdate.ID.ToString());

            return json;
        }


    }


}
