using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainingOnboarding.Models
{

    public class CheckListItem : BaseSPItem
    {
        public CheckListItem()
        { 
        }
        public CheckListItem(ListItem item) : base(item)
        {
            this.CourseID = base.GetFieldValue(item, "CourseID");
            this.Requirement = item.Fields.AdditionalData["Title"]?.ToString();
        }

        public string CourseID { get; set; }
        public string Requirement { get; set; }

        public override bool IsValid => !string.IsNullOrEmpty(Requirement) && !string.IsNullOrEmpty(CourseID);

        public List<string> FinishedBy { get; set; } = new List<string>();
        public List<SiteUser> CompletedUsers { get; set; } = new List<SiteUser>();
    }
}
