using DigitalTrainingAssistant.Models.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DigitalTrainingAssistant.Tests
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void StringHelperTests()
        {
            Assert.IsTrue(StringUtils.IsIntegerReally("1.00"));
            Assert.IsTrue(StringUtils.IsIntegerReally("2.00"));
            Assert.IsFalse(StringUtils.IsIntegerReally("1.01"));
        }
    }
}
