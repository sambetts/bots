using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrainingOnboarding.Models
{

    public class PendingUserActions
    {
        public List<PendingUserActionsForCourse> Actions { get; set; } = new List<PendingUserActionsForCourse>();


        public List<CourseContact> UniqueUsers
        { 
            get 
            {
                var list = new List<CourseContact>();
                foreach (var item in Actions)
                {
                    if (!list.Contains(item.User))
                    {
                        list.Add(item.User);
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
            return new PendingUserActions { Actions = Actions.Where(a => a.User.Email.ToLower() == email.ToLower()).ToList() };
        }
    }


    public class PendingUserActionsForCourse
    {
        public Course Course { get; set; }
        public CourseContact User { get; set; }
        public List<CheckListItem> PendingItems { get; set; } = new List<CheckListItem>();
    }
}
