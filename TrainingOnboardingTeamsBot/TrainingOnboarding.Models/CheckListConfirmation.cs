using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainingOnboarding.Models
{

    public class CheckListConfirmation : BaseSPItemWithUser
    {
        public CheckListConfirmation(ListItem item, List<CourseContact> allUsers) : base(item, allUsers, "DoneByLookupId")
        {
            this.CheckListItemId = item.Fields.AdditionalData["Title"]?.ToString();
        }

        public string CheckListItemId { get; set; }
    }
}
