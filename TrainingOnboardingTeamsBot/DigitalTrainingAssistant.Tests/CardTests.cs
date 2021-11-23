using DigitalTrainingAssistant.Bot.Cards;
using DigitalTrainingAssistant.Models.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DigitalTrainingAssistant.Tests
{
    [TestClass]
    public class CardTests
    {
        [TestMethod]
        public void SimpleCardLoadTest()
        {
            var c = new AttendeeFixedQuestionsInputCard(new Models.CourseAttendance { });
            Assert.IsNotNull(c.GetCardContent());
        }
    }
}
