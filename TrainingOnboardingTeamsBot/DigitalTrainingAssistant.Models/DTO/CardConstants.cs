using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Models
{
    /// <summary>
    /// A class that holds card constants that are used in multiple files.
    /// </summary>
    public static class CardConstants
    {
        public static string FIELD_NAME_BOT_NAME => "${BotName}";
        public static string FIELD_NAME_TRAINER_NAME => "${TrainerName}";
        public static string FIELD_NAME_TRAINER_EMAIL => "${TrainerEmai}";
        public static string FIELD_NAME_ATTENDEE_NAME => "${AttendeeName}";
        public static string FIELD_NAME_ATTENDEE_EMAIL => "${AttendeeEmail}";
        public static string FIELD_NAME_COURSE_NAME => "${CourseName}";
        public static string FIELD_NAME_COURSE_IMAGE_BASE64 => "${CourseImageBase64}";
        public static string FIELD_NAME_COURSE_INTRO_TEXT => "${CourseIntroduction}";
        public static string FIELD_NAME_COURSE_LINK => "${CourseLink}";

        public static string FIELD_NAME_SHAREPOINT_ID => "${SharePointId}";


        public static string FIELD_NAME_QARole => "${QARole}";
        public static string FIELD_NAME_QAOrg => "${QAOrg}";
        public static string FIELD_NAME_QACountry => "${QACountry}";
        public static string FIELD_NAME_QASpareTimeActivities => "${QASpareTimeActivities}";
        public static string FIELD_NAME_QAMobilePhoneNumber => "${QAMobilePhoneNumber}";
        public static string FIELD_NAME_PROFILE_IMG => "${ProfileImg}";

        public static string CardFileNameAttendeeFixedQuestionsInput => "DigitalTrainingAssistant.Bot.Cards.Templates.AttendeeFixedQuestionsInput.json";
        public static string CardFileNameAttendeeFixedQuestionsPublication => "DigitalTrainingAssistant.Bot.Cards.Templates.AttendeeFixedQuestionsPublication.json";
        public static string CardFileNameBotIntroductionProactive => "DigitalTrainingAssistant.Bot.Cards.Templates.BotIntroduction.json";
        public static string CardFileNameBotIntroductionReactive => "DigitalTrainingAssistant.Bot.Cards.Templates.BotIntroductionReactive.json";
        public static string CardFileNameCourseWelcome => "DigitalTrainingAssistant.Bot.Cards.Templates.CourseWelcome.json";
        public static string CardFileNameIntroduceYourself => "DigitalTrainingAssistant.Bot.Cards.Templates.IntroduceYourself.json";
        public static string CardFileNameLearnerQuestions => "DigitalTrainingAssistant.Bot.Cards.Templates.LearnerQuestions.json";
        public static string CourseDefaultImage => "DigitalTrainingAssistant.Bot.Cards.Templates.DefaultCourseImageBase64.txt";


        public const string CardActionPropName = "action";
        public const string CardSharePointIdPropName = "SPID";
        public const string CardActionValLearnerTasksDone = "LearnerTasksDone";
        public const string CardActionValStartIntroduction = "StartIntroduction";
        public const string CardActionValSaveIntroductionQuestions = "SaveIntroductionQuestions";


        /// <summary>
        /// default value for channel activity to send notifications.
        /// </summary>
        public const string TeamsBotFrameworkChannelId = "msteams";
    }
}
