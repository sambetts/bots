using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainingOnboarding.Models
{

    public class Course : BaseSPItemWithUser
    {
        public Course(ListItem courseItem, List<SiteUser> allUsers) : base(courseItem, allUsers, "TrainerLookupId")
        {
            this.Name = courseItem.Fields.AdditionalData["Title"]?.ToString();
            this.WelcomeMessage = courseItem.Fields.AdditionalData.ContainsKey("WelcomeMessage") ? courseItem.Fields.AdditionalData["WelcomeMessage"]?.ToString() : string.Empty;

            var startString = courseItem.Fields.AdditionalData.ContainsKey("Start") ? courseItem.Fields.AdditionalData["Start"]?.ToString() : string.Empty;
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

        public SiteUser Trainer => base.User;
        public DateTime? Start { get; set; }
        public string Name { get; set; }
        public string WelcomeMessage { get; set; }
        public List<CheckListItem> CheckListItems { get; set; } = new List<CheckListItem>();
        public List<CourseAttendance> Attendees { get; set; } = new List<CourseAttendance>();

    }
}
