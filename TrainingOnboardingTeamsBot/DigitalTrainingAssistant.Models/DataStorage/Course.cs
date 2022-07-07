using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Models
{

    public class Course : BaseSPItemWithUser
    {
        public Course() { }

        public Course(ListItem courseItem, List<SiteUser> allUsers) : base(courseItem, allUsers, "TrainerLookupId")
        {
            this.Name = base.GetFieldValue(courseItem, "Title");
            this.WelcomeMessage = base.GetFieldValue(courseItem, "WelcomeMessage");
            this.TeamId = base.GetFieldValue(courseItem, "TeamID");
            this.GeneralTeamChannelId = base.GetFieldValue(courseItem, "ChannelID");
            this.IntroductionTeamChannelId = base.GetFieldValue(courseItem, "IntroductionChannelID");
            this.Link = base.GetFieldValue(courseItem, "LearnerAppLink");
            this.ImageBase64Data = base.GetFieldValue(courseItem, "CourseImgBase64");
   
            // Default 3 days
            this.DaysBeforeToSendReminders = base.GetFieldInt(courseItem, "DaysBeforeToSendReminders", 3);

            this.Start = GetFieldDateTime(courseItem, "Start");
            this.End = GetFieldDateTime(courseItem, "End");
        }

        #region Props

        public SiteUser Trainer => base.User;
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string Name { get; set; }
        public string WelcomeMessage { get; set; }
        public List<CheckListItem> CheckListItems { get; set; } = new List<CheckListItem>();
        public List<CourseAttendance> Attendees { get; set; } = new List<CourseAttendance>();
        public int DaysBeforeToSendReminders { get; set; }

        public string ImageBase64Data { get; set; }
        public string Link { get; set; }
        public string TeamId { get; set; }

        /// <summary>
        /// Either the introduction channel or the general channel. 
        /// </summary>
        public string PostToTeamChannelId
        {
            get
            {
                if (!string.IsNullOrEmpty(IntroductionTeamChannelId))
                {
                    return IntroductionTeamChannelId;
                }
                return GeneralTeamChannelId;
            } 
        }
        public string GeneralTeamChannelId { get; set; }
        public string IntroductionTeamChannelId { get; set; }


        public bool HasValidTeamsSettings => !string.IsNullOrEmpty(TeamId) && !string.IsNullOrEmpty(PostToTeamChannelId);

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
