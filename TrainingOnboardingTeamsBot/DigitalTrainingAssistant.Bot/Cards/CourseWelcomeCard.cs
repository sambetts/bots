using DigitalTrainingAssistant.Models;

namespace DigitalTrainingAssistant.Bot.Cards
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
            var json = ReadResource(CardConstants.CardFileNameCourseWelcome);
            var defaultImageString = ReadResource(CardConstants.CourseDefaultImage);

            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_COURSE_NAME, this.Course.Name);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_BOT_NAME, this.BotName);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_TRAINER_NAME, Course?.Trainer?.Name);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_TRAINER_EMAIL, Course?.Trainer?.Email);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_COURSE_INTRO_TEXT, Course?.WelcomeMessage);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_COURSE_LINK, Course?.Link);
            json = base.ReplaceVal(json, CardConstants.FIELD_NAME_COURSE_IMAGE_BASE64, 
                !string.IsNullOrEmpty(Course?.ImageBase64Data) ? Course?.ImageBase64Data : defaultImageString);

            return json;
        }
    }
}
