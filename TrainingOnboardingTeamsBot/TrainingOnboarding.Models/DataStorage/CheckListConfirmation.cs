using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainingOnboarding.Models
{

    public class CheckListConfirmation : BaseSPItemWithUser
    {
        public CheckListConfirmation(ListItem item, List<SiteUser> allUsers) : base(item, allUsers, "DoneByLookupId")
        {
            this.CheckListItemId = GetFieldValue(item, "CheckListID");
        }

        public string CheckListItemId { get; set; }
    }
}
