using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrainingOnboarding.Models
{
    /// <summary>
    /// Outstanding tasks for all users/course. Generated from a CoursesMetadata.
    /// </summary>
    public class PendingUserActions
    {
        public List<PendingUserActionsForCourse> Actions { get; set; } = new List<PendingUserActionsForCourse>();


        public List<CourseAttendance> UniqueUsers
        { 
            get 
            {
                var list = new List<CourseAttendance>();
                foreach (var item in Actions)
                {
                    if (!list.Contains(item.Attendee))
                    {
                        list.Add(item.Attendee);
                    }
                }
                return list;
            } 
        }

        public List<Course> UniqueCourses
        {
            get
            {
                var list = new List<Course>();
                foreach (var item in Actions)
                {
                    if (!list.Contains(item.Course))
                    {
                        list.Add(item.Course);
                    }
                }
                return list;
            }
        }
        public PendingUserActions GetActionsByEmail(string email)
        {
            return new PendingUserActions { Actions = Actions.Where(a => a.Attendee.User.Email.ToLower() == email.ToLower()).ToList() };
        }
    }


    public class PendingUserActionsForCourse
    {
        public Course Course { get; set; }
        public CourseAttendance Attendee { get; set; }
        public List<CheckListItem> PendingItems { get; set; } = new List<CheckListItem>();
    }
}
