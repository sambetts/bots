using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainingOnboarding.Models
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

        public SiteUser Trainer => base.User;
        public DateTime? Start { get; set; }
        public string Name { get; set; }
        public string WelcomeMessage { get; set; }
        public List<CheckListItem> CheckListItems { get; set; } = new List<CheckListItem>();
        public List<CourseAttendance> Attendees { get; set; } = new List<CourseAttendance>();
        public int DaysBeforeToSendReminders { get; set; }
    }
}
