using DigitalTrainingAssistant.Models;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace DigitalTrainingAssistant.Bot.Cards
{

    /// <summary>
    /// Class that helps to return introduction detail card as attachment.
    /// </summary>
    public class AttendeeFixedQuestionsPublicationCard : BaseAdaptiveCard
    {
        public AttendeeFixedQuestionsPublicationCard(CourseAttendance attendanceInfo)
        {
            this.Info = attendanceInfo;
        }

        public CourseAttendance Info { get; set; }

        public override string GetCardContent()
        {
            var json = ReadResource(CardConstants.CardFileNameAttendeeFixedQuestionsPublication);

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_ATTENDEE_NAME, this.Info.User.Name);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_ATTENDEE_EMAIL, this.Info.User.Email);

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QARole, this.Info.QARole);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QAOrg, this.Info.QAOrg);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QACountry, this.Info.QACountry);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QASpareTimeActivities, this.Info.QASpareTimeActivities);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_QAMobilePhoneNumber, this.Info.QAMobilePhoneNumber);

            return json;
        }


        public ChatMessageAttachment GetChatMessageAttachment()
        {
            return new ChatMessageAttachment
            {
                ContentType = AdaptiveCards.AdaptiveCard.ContentType,
                Content = this.GetCardContent(),
            };
        }
    }
}
