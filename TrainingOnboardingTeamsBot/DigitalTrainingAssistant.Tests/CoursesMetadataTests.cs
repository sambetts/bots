using DigitalTrainingAssistant.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Tests
{
    [TestClass]
    public class CoursesMetadataTests
    {
        #region Stuff

        public IConfiguration _configuration;

        [TestInitialize]
        public void Init()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            _configuration = builder.Build();
        }
        async Task<GraphServiceClient> GetClient()
        {
            var token = await AuthHelper.GetToken(_configuration["TenantId"], _configuration["MicrosoftAppId"], _configuration["MicrosoftAppPassword"]);
            return AuthHelper.GetAuthenticatedClient(token);
        }
        #endregion

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
        public async Task CoursesMetadataLoad()
        {
            var graphClient = await GetClient();
            var meta = await CoursesMetadata.LoadTrainingSPData(graphClient, _configuration["SharePointSiteId"]);
            Assert.IsNotNull(meta);

            Assert.IsTrue(meta.Courses.Count > 0);
            Assert.IsTrue(meta.Courses[0].Attendees.Count > 0);
        }

        /// <summary>
        /// Tests we can save a CourseTasksUpdateInfo
        /// </summary>
        [TestMethod]
        public async Task CourseTasksUpdateInfoSave()
        {
            var graphClient = await GetClient();

            var testInvalidUpdate = new CourseTasksUpdateInfo { UserAadObjectId = _configuration["TestUserAadObjectId"] };
            testInvalidUpdate.ConfirmedTaskIds.Add(233331);
            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() => testInvalidUpdate.SaveChanges(graphClient, _configuration["SharePointSiteId"]));

            var testUpdate = new CourseTasksUpdateInfo { UserAadObjectId = _configuration["TestUserAadObjectId"] };
            testUpdate.ConfirmedTaskIds.Add(1);

            await testUpdate.SaveChanges(graphClient, _configuration["SharePointSiteId"]);
        }

        /// <summary>
        /// Tests we can save CourseAttendance
        /// </summary>
        [TestMethod]
        public async Task CourseAttendanceSave()
        {
            var graphClient = await GetClient();

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

            await a.SaveChanges(graphClient, _configuration["SharePointSiteId"]);
        }
    }
}
