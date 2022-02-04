using DigitalTrainingAssistant.Models;
using DigitalTrainingAssistant.Models.Util;
using Microsoft.Graph;
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

        [TestMethod]
        public void ServiceExceptionIsNotFoundError()
        {
            var listError = new ServiceException(new Error() { Code = "itemNotFound", Message = "The specified list was not found" });

            Assert.IsTrue(listError.IsItemNotFoundError());
        }
    }
}
