using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrainingOnboarding.Bot.Cards
{
    /// <summary>
    /// A class that holds card constants that are used in multiple files.
    /// </summary>
    public static class CardConstants
    {
        public static string FIELD_NAME_BOT_NAME => "${BotName}";
        public static string FIELD_NAME_TRAINER_NAME => "${TrainerName}";
        public static string FIELD_NAME_COURSE_NAME => "${CourseName}";
        public static string FIELD_NAME_COURSE_INTRO_TEXT => "${CourseIntroduction}";

        public const string CardActionPropName = "action";
        public const string CardActionValLearnerTasksDone = "LearnerTasksDone";


        /// <summary>
        /// default value for channel activity to send notifications.
        /// </summary>
        public const string TeamsBotFrameworkChannelId = "msteams";
    }
}
