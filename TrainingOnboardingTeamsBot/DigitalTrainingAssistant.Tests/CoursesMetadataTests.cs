using DigitalTrainingAssistant.Models;
using DigitalTrainingAssistant.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Tests
{
    [TestClass]
    public class CoursesMetadataTests : BaseUnitTest
    {
        [TestMethod]
        public void RemindersEndDateTests()
        {
            var trainer = new SiteUser { Name = "trainer" };
            var atendee = new SiteUser { Name = "atendee" };

            var course = new Course
            {
                Start = DateTime.Now.AddMinutes(-10),
                Name = "Course that's already started",
                User = trainer,
                DaysBeforeToSendReminders = 3,
                Attendees = new List<CourseAttendance> { new CourseAttendance { User = atendee } },
                CheckListItems = new List<CheckListItem> { new CheckListItem { Requirement = "Test requirement" } }
            };
            var meta = new CoursesMetadata()
            {
                Courses = new List<Course> { course }
            };

            // We should get none, as no end date & start date has passed
            var resultsWithinCourseDaysBeforeToSendReminders = meta.GetUserActionsWithThingsToDo(true);
            Assert.IsTrue(resultsWithinCourseDaysBeforeToSendReminders.UniqueCourses.Count == 0);

            resultsWithinCourseDaysBeforeToSendReminders = meta.GetUserActionsWithThingsToDo(false);            // Test again & ignore "days before"
            Assert.IsTrue(resultsWithinCourseDaysBeforeToSendReminders.UniqueCourses.Count == 0);

            // Set end date as later than now & try again
            course.End = DateTime.Now.AddDays(3);
            resultsWithinCourseDaysBeforeToSendReminders = meta.GetUserActionsWithThingsToDo(true);
            Assert.IsTrue(resultsWithinCourseDaysBeforeToSendReminders.UniqueCourses.Count == 1);

            resultsWithinCourseDaysBeforeToSendReminders = meta.GetUserActionsWithThingsToDo(false);            // Test again & ignore "days before"
            Assert.IsTrue(resultsWithinCourseDaysBeforeToSendReminders.UniqueCourses.Count == 1);
        }

        /// <summary>
        /// Tests that DaysBeforeToSendReminders is taken into 
        /// </summary>
        [TestMethod]
        public void CourseDaysBeforeToSendRemindersFilter()
        {
            var nowish = DateTime.Now.AddMinutes(5);        // Slightly in the future so DateTime.Now will be < "now" when test is run
            var trainer = new SiteUser { Name = "trainer" };
            var atendee = new SiteUser { Name = "atendee" };

            var meta = new CoursesMetadata()
            {
                Courses = new List<Course>
                {
                    new Course
                    {
                        Start = nowish.AddDays(1),
                        Name = "Course that's in scope for reminder",
                        User = trainer,
                        DaysBeforeToSendReminders = 3,
                        Attendees = new List<CourseAttendance>{ new CourseAttendance { User = atendee } },
                        CheckListItems = new List<CheckListItem>{ new CheckListItem { Requirement = "Test requirement" } }
                    },
                    new Course
                    {
                        Start = nowish.AddDays(4),
                        Name = "Not in scope",
                        User = trainer,
                        DaysBeforeToSendReminders = 3,
                        Attendees = new List<CourseAttendance>{ new CourseAttendance { User = atendee } },
                        CheckListItems = new List<CheckListItem>{ new CheckListItem { Requirement = "Test requirement" } }
                    }
                }
            };

            // Get actions, respecting DaysBeforeToSendReminders
            var resultsWithinCourseDaysBeforeToSendReminders = meta.GetUserActionsWithThingsToDo(true);
            Assert.IsTrue(resultsWithinCourseDaysBeforeToSendReminders.UniqueCourses.Count == 1);

            // Get all actions
            var defaultResults = meta.GetUserActionsWithThingsToDo(false);
            Assert.IsTrue(defaultResults.UniqueCourses.Count == 2);
        }

        /// <summary>
        /// Basic data load test
        /// </summary>
        [TestMethod]
        public async Task CoursesMetadataLoadTrainingSPData()
        {
            var graphClient = await TestingUtils.GetClient(_configuration);
            var meta = await CoursesMetadata.LoadTrainingSPData(graphClient, _configuration.SharePointSiteId);
            Assert.IsNotNull(meta);

            Assert.IsTrue(meta.Courses.Count > 0);
            Assert.IsTrue(meta.Courses[0].Attendees.Count > 0);
            Assert.IsTrue(meta.Courses[0].Attendees[0].ParentCourse.ID != 0);
        }

        [TestMethod]
        public async Task CourseAttendanceLoadById()
        {
            var graphClient = await TestingUtils.GetClient(_configuration);
            var attendance = await CourseAttendance.LoadById(graphClient, _configuration.SharePointSiteId, 1);
            Assert.IsNotNull(attendance);

            Assert.IsTrue(attendance.ParentCourse.ID != 0);
        }

        /// <summary>
        /// Tests we can save a CourseTasksUpdateInfo
        /// </summary>
        [TestMethod]
        public async Task CourseTasksUpdateInfoSave()
        {
            var graphClient = await TestingUtils.GetClient(_configuration);

            var testInvalidUpdate = new CourseTasksUpdateInfo { UserAadObjectId = _configuration.TestUserAadObjectId };
            testInvalidUpdate.ConfirmedTaskIds.Add(233331);
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => testInvalidUpdate.SaveChanges(graphClient, _configuration.SharePointSiteId));

            // Phind phirst task
            var spCache = new SPCache(_configuration.SharePointSiteId, graphClient);
            var checkListList = await spCache.GetList(ModelConstants.ListNameCourseChecklist);
            var firstRecordResult = await graphClient.Sites[_configuration.SharePointSiteId].Lists[checkListList.Id].Items.Request()
                .Top(1)
                .GetAsync();
            if (firstRecordResult.Count < 1)
            {
                Assert.Fail("No checklist items found in site");
            }
            var testUpdate = new CourseTasksUpdateInfo { UserAadObjectId = _configuration.TestUserAadObjectId };
            testUpdate.ConfirmedTaskIds.Add(int.Parse(firstRecordResult[0].Id));

            await testUpdate.SaveChanges(graphClient, _configuration.SharePointSiteId);
        }

        /// <summary>
        /// Tests we can save CourseAttendance
        /// </summary>
        [TestMethod]
        public async Task CourseAttendanceSave()
        {
            var graphClient = await TestingUtils.GetClient(_configuration);

            var a = new CourseAttendance
            {
                BotContacted = true,
                CourseId = 1,
                ID = 1,
                QACountry = "1232",
                QAMobilePhoneNumber = "345",
                QARole = "567",
                QASpareTimeActivities = "9898",
                User = new SiteUser { Email = "AdeleV@M365x352268.OnMicrosoft.com", Name = "Adele" }
            };

            await a.SaveChanges(graphClient, _configuration.SharePointSiteId);
        }
    }
}
