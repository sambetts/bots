using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Models
{
    /// <summary>
    /// Who is on what course & state of user's info for the course
    /// </summary>
    public class CourseAttendance : BaseSPItemWithUser
    {
        public CourseAttendance() { }
        public CourseAttendance(ListItem item, List<SiteUser> allUsers) : base(item, allUsers, "AssignedUserLookupId")
        {
            this.CourseId = GetFieldInt(item, "CourseattendanceID");
            this.QACountry = GetFieldValue(item, "QACountry");
            this.QARole = GetFieldValue(item, "QARole");
            this.QAOrg = GetFieldValue(item, "QAOrg");
            this.QASpareTimeActivities = GetFieldValue(item, "QASpareTimeActivities");
            this.QAMobilePhoneNumber = GetFieldValue(item, "QAMobileNumber");

            this.IntroductionDone = GetFieldBool(item, "IntroductionDone");
            this.BotContacted = GetFieldBool(item, "BotContacted");
        }

        #region Props

        public int CourseId { get; set; }

        public string QACountry { get; set; }
        public string QARole { get; set; }
        public string QAOrg { get; set; }
        public string QASpareTimeActivities { get; set; }
        public string QAMobilePhoneNumber { get; set; }
        public bool BotContacted { get; set; }

        public bool IntroductionDone { get; set; }

        #endregion

        public async Task SaveChanges(GraphServiceClient graphClient, string siteId)
        {
            var allLists = await graphClient.Sites[siteId]
                    .Lists
                    .Request()
                    .GetAsync();

            var attendenceList = allLists.Where(l => l.Name == ModelConstants.ListNameCourseAttendance).SingleOrDefault();

            ListItem taskItem = null;
            try
            {
                taskItem = (await graphClient
                    .Sites[siteId]
                    .Lists[attendenceList.Id]
                    .Items[this.ID.ToString()]
                    .Request()
                    .Expand("fields")
                    .GetAsync());
            }
            catch (ServiceException ex)
            {
                if (ex.IsNotFoundError())
                {
                    throw new ArgumentOutOfRangeException(nameof(this.ID), $"No attendence record with ID {ID} found");
                }
                else
                {
                    throw;
                }
            }

            await graphClient
                        .Sites[siteId]
                        .Lists[attendenceList.Id]
                        .Items[this.ID.ToString()]
                        .Request()
                        .UpdateAsync(new ListItem
                        {
                            Fields = new FieldValueSet
                            {
                                AdditionalData = new Dictionary<string, object>
                                {
                                    {"QACountry", this.QACountry },
                                    {"QARole", this.QARole},
                                    {"QASpareTimeActivities", this.QASpareTimeActivities},
                                    {"QAMobilePhoneNumber", this.QAMobilePhoneNumber},
                                    {"BotContacted", this.BotContacted},
                                    {"IntroductionDone", this.IntroductionDone}
                                }
                            }
                        });

        }

        public static async Task<CourseAttendance> LoadById(GraphServiceClient graphClient, string siteId, int sPID)
        {
            var allLists = await graphClient.Sites[siteId]
                                .Lists
                                .Request()
                                .GetAsync();

            var coursesList = allLists.Where(l => l.Name == ModelConstants.ListNameCourses).SingleOrDefault();
            var courseAttendanceList = allLists.Where(l => l.Name == ModelConstants.ListNameCourseAttendance).SingleOrDefault();
            var courseAttendanceItem = await graphClient.Sites[siteId].Lists[courseAttendanceList.Id].Items[sPID.ToString()].Request().Expand("fields").GetAsync();
            var allUsers = await CoursesMetadata.LoadSiteUsers(graphClient, siteId);

            return new CourseAttendance(courseAttendanceItem, allUsers);
        }
    }
}
