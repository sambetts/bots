using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot.Cards
{
    /// <summary>
    /// A class that holds card constants that are used in multiple files.
    /// </summary>
    public static class CardConstants
    {
        public static string FIELD_NAME_BOT_NAME => "${BotName}";
        public static string FIELD_NAME_TRAINER_NAME => "${TrainerName}";
        public static string FIELD_NAME_ATTENDEE_NAME => "${AttendeeName}";
        public static string FIELD_NAME_ATTENDEE_EMAIL => "${AttendeeEmail}";
        public static string FIELD_NAME_COURSE_NAME => "${CourseName}";
        public static string FIELD_NAME_COURSE_INTRO_TEXT => "${CourseIntroduction}";

        public static string FIELD_NAME_SHAREPOINT_ID => "${SharePointId}";


        public static string FIELD_NAME_QARole => "${QARole}";
        public static string FIELD_NAME_QAOrg => "${QAOrg}";
        public static string FIELD_NAME_QACountry => "${QACountry}";
        public static string FIELD_NAME_QASpareTimeActivities => "${QASpareTimeActivities}";
        public static string FIELD_NAME_QAMobilePhoneNumber => "${QAMobilePhoneNumber}";

        public static string CardFileNameAttendeeFixedQuestionsInput => "AttendeeFixedQuestionsInput.json";
        public static string CardFileNameAttendeeFixedQuestionsPublication => "AttendeeFixedQuestionsPublication.json";
        public static string CardFileNameBotIntroduction => "BotIntroduction.json";
        public static string CardFileNameCourseWelcome => "CourseWelcome.json";
        public static string CardFileNameIntroduceYourself => "IntroduceYourself.json";
        public static string CardFileNameLearnerQuestions => "LearnerQuestions.json";


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
