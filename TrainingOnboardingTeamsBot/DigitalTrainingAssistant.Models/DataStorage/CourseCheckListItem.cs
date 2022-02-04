using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalTrainingAssistant.Models
{

    public class CheckListItem : BaseSPItem
    {
        public CheckListItem()
        { 
        }
        public CheckListItem(ListItem item) : base(item)
        {
            this.CourseID = base.GetFieldInt(item, "CourseID");
            this.Requirement = item.Fields.AdditionalData["Title"]?.ToString();
        }

        public int CourseID { get; set; }
        public string Requirement { get; set; }

        public override bool IsValid => !string.IsNullOrEmpty(Requirement) && CourseID != 0;

        public List<string> FinishedBy { get; set; } = new List<string>();
        public List<SiteUser> CompletedUsers { get; set; } = new List<SiteUser>();

        public override string ToString()
        {
            return $"{this.Requirement}";
        }
    }
}
