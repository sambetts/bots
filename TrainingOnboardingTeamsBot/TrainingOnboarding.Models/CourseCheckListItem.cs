using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainingOnboarding.Models
{

    public class CheckListItem : BaseSPItem
    {
        public CheckListItem(ListItem item) : base(item)
        {
            this.CourseID = item.Fields.AdditionalData.ContainsKey("CourseID") ? item.Fields.AdditionalData["CourseID"]?.ToString() : string.Empty;
            this.Requirement = item.Fields.AdditionalData["Title"]?.ToString();
        }

        public string CourseID { get; set; }
        public string Requirement { get; set; }

        public List<string> FinishedBy { get; set; } = new List<string>();
        public List<CourseContact> CompletedUsers { get; set; } = new List<CourseContact>();
    }
}
