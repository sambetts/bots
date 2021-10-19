using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainingOnboarding.Models
{
    public class CourseAttendance : BaseSPItemWithUser
    {
        public CourseAttendance(ListItem courseItem, List<CourseContact> allUsers) : base(courseItem, allUsers, "AssignedUserLookupId")
        {
            this.CourseId = courseItem.Fields.AdditionalData["Title"]?.ToString();

        }
        public string CourseId { get; set; }
    }
}
