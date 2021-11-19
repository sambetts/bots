using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrainingOnboarding.Models.Util;

namespace TrainingOnboarding.Tests
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
