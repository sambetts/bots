using DigitalTrainingAssistant.Bot.Cards;
using DigitalTrainingAssistant.Models;
using DigitalTrainingAssistant.Models.Util;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace DigitalTrainingAssistant.Tests
{
    [TestClass]
    public class CardTests
    {
        [TestMethod]
        public void SimpleCardLoadTest()
        {

            var botWelcomeCard = new BotWelcomeCard("Botulus Bob");
            Assert.IsNotNull(botWelcomeCard.GetCardContent());

            var attendeeFixedQuestionsInputCard = new AttendeeFixedQuestionsInputCard(new CourseAttendance { });
            Assert.IsNotNull(attendeeFixedQuestionsInputCard.GetCardContent());

            var course = new Course { };


            var courseWelcomeCard = new CourseWelcomeCard("Botulus Bob", course);
            Assert.IsNotNull(courseWelcomeCard.GetCardContent());

            var attendance = new CourseAttendance
            {
                ParentCourse = course,
                User = new SiteUser
                {
                    Email = "bob@unittest.com",
                    Name = "Bob"
                }
            };

            var pendingTasksListCard = new PendingTasksListCard(attendance,
                new List<PendingUserActionsForCourse>
                {
                    new PendingUserActionsForCourse
                    {
                        Course = course,
                        Attendee = attendance
                    }
                },
                course
            );

            Assert.IsNotNull(pendingTasksListCard.GetCardContent());

        }
    }
}
