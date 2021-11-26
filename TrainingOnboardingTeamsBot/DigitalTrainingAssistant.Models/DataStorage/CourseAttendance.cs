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
        #region Constructors

        public CourseAttendance() { }

        public CourseAttendance(ListItem item, List<SiteUser> allUsers) : this(item, allUsers, new Course())       // Call default constructor
        {
        }
        public CourseAttendance(ListItem item, List<SiteUser> allUsers, Course parentCourse) : base(item, allUsers, "AssignedUserLookupId")
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (allUsers is null)
            {
                throw new ArgumentNullException(nameof(allUsers));
            }

            this.CourseId = GetFieldInt(item, "CourseattendanceID");
            this.QACountry = GetFieldValue(item, "QACountry");
            this.QARole = GetFieldValue(item, "QARole");
            this.QAOrg = GetFieldValue(item, "QAOrg");
            this.QASpareTimeActivities = GetFieldValue(item, "QASpareTimeActivities");
            this.QAMobilePhoneNumber = GetFieldValue(item, "QAMobileNumber");

            this.ParentCourse = parentCourse ?? throw new ArgumentNullException(nameof(parentCourse));
            this.IntroductionDone = GetFieldBool(item, "IntroductionDone");
            this.BotContacted = GetFieldBool(item, "BotContacted");
        }

        public CourseAttendance(ListItem item, List<SiteUser> allUsers, List<Course> allCourses) : this(item, allUsers, new Course())       // Call default constructor
        { 
            // Override parent-course
            this.ParentCourse = allCourses.Where(c=> c.ID == this.CourseId).FirstOrDefault();
        }
        #endregion

        #region Props

        public Course ParentCourse { get; set; }
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
            var spCache = new SPCache(siteId, graphClient);
            var attendenceList = await spCache.GetList(ModelConstants.ListNameCourseAttendance);

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
                if (ex.IsItemNotFoundError())
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
            var spCache = new SPCache(siteId, graphClient);

            var courseAttendanceList = await spCache.GetList(ModelConstants.ListNameCourseAttendance);
            var courseAttendanceItem = await graphClient.Sites[siteId].Lists[courseAttendanceList.Id].Items[sPID.ToString()].Request().Expand("fields").GetAsync();
            var allUsers = await CoursesMetadata.LoadSiteUsers(graphClient, siteId);

            var attendance = new CourseAttendance(courseAttendanceItem, allUsers);

            var course = await Course.LoadById(graphClient, siteId, attendance.CourseId);

            attendance.ParentCourse = course;

            return attendance;
        }

        public override string ToString()
        {
            return $"{this.User}";
        }
    }
}
