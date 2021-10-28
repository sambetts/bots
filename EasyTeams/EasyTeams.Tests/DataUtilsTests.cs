using EasyTeams.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyTeams.Tests
{
    [TestClass]
    public class DataUtilsTests
    {
        [TestMethod]
        public void ExtractEmailFromContactTests()
        {
            Assert.ThrowsException<ArgumentNullException>(() => DataUtils.ExtractEmailFromContact(null));
            Assert.ThrowsException<ArgumentNullException>(() => DataUtils.ExtractEmailFromContact(""));

            Assert.ThrowsException<FormatException>(() => DataUtils.ExtractEmailFromContact("asdfasdfasdf"));
            Assert.ThrowsException<FormatException>(() => DataUtils.ExtractEmailFromContact("sambetts@microsoft.com text sambetts@contoso.com"));

            var email = DataUtils.ExtractEmailFromContact("text sambetts@contoso.com blah blah");
            Assert.IsTrue(email == "sambetts@contoso.com");
        }
    }
}
