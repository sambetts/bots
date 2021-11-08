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
            this.CheckListItemId = GetFieldInt(item, "CheckListID");
        }

        public int CheckListItemId { get; set; }
    }
}
