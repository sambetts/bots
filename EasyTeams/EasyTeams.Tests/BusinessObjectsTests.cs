using EasyTeams.Common.BusinessLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyTeams.Tests
{
    [TestClass]
    public class BusinessObjectsTests
    {

        [TestMethod]
        public void ValidNewConferenceCallRequestTests()
        {
            NewConferenceCallRequest request = new NewConferenceCallRequest();

            Assert.IsFalse(request.IsValid());

            // Fill out the things correctly
            request.MinutesLong = 10;
            request.OnBehalfOf = new MeetingContact("jimbo@contoso.com", false);
            request.Recipients.Add(new MeetingContact("bob@contoso.com", false));
            request.TimeZoneName = TimeZoneInfo.Local.DisplayName;
            request.Start = DateTime.Now;
            request.Subject = "Test";

            Assert.IsTrue(request.IsValid());
        }

        [TestMethod]
        public void ValidContactEmailAddressTests()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                MeetingContact emailAddress = new MeetingContact("bob", false);
            });

            // Should work
            MeetingContact emailAddress = new MeetingContact("jimbo@contoso.com");
        }
    }
}
