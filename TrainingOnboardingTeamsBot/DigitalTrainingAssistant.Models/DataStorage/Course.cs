using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Models
{

    public class Course : BaseSPItemWithUser
    {
        public Course()
        { 
        }

        public Course(ListItem courseItem, List<SiteUser> allUsers) : base(courseItem, allUsers, "TrainerLookupId")
        {
            this.Name = base.GetFieldValue(courseItem, "Title");
            this.WelcomeMessage = base.GetFieldValue(courseItem, "WelcomeMessage");
            this.TeamId = base.GetFieldValue(courseItem, "TeamID");
            this.TeamChannelId = base.GetFieldValue(courseItem, "ChannelID");

            var daysBeforeToSendRemindersString = base.GetFieldValue(courseItem, "DaysBeforeToSendReminders");
            var days = 3;       // Default 3 days
            int.TryParse(daysBeforeToSendRemindersString, out days);
            this.DaysBeforeToSendReminders = days;

            var startString = base.GetFieldValue(courseItem, "Start");
            var dt = DateTime.MinValue;
            if (DateTime.TryParse(startString, out dt))
            {
                this.Start = dt;
            }
            else
            {
                this.Start = null;
            }
        }

        #region Props

        public SiteUser Trainer => base.User;
        public DateTime? Start { get; set; }
        public string Name { get; set; }
        public string WelcomeMessage { get; set; }
        public List<CheckListItem> CheckListItems { get; set; } = new List<CheckListItem>();
        public List<CourseAttendance> Attendees { get; set; } = new List<CourseAttendance>();
        public int DaysBeforeToSendReminders { get; set; }

        public string TeamId { get; set; }
        public string TeamChannelId { get; set; }
        public bool HasValidTeamsSettings => !string.IsNullOrEmpty(TeamId) && !string.IsNullOrEmpty(TeamChannelId);

        #endregion

        public static async Task<Course> LoadById(GraphServiceClient graphClient, string siteId, int courseSharePointId)
        {
            if (courseSharePointId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(courseSharePointId), "Cannot load unsaved course");
            }

            var spCache = new SPCache(siteId, graphClient);

            var courseList = await spCache.GetList(ModelConstants.ListNameCourses);
            var courseItem = await graphClient.Sites[siteId].Lists[courseList.Id].Items[courseSharePointId.ToString()].Request().Expand("fields").GetAsync();
            var allUsers = await CoursesMetadata.LoadSiteUsers(graphClient, siteId);

            return new Course(courseItem, allUsers);
        }

        public override string ToString()
        {
            return $"{this.Name}";
        }
    }
}
