using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrainingOnboarding.Models
{
    public abstract class BaseSPItem
    {
        protected BaseSPItem(ListItem item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.Fields == null)
            {
                throw new ArgumentNullException("Fields");
            }
            this.ID = item.Fields.Id;
        }

        public string ID { get; set; }
    }

    public abstract class BaseSPItemWithUser : BaseSPItem
    {
        protected BaseSPItemWithUser(ListItem item, List<CourseContact> allUsers, string userFieldName) : base(item)
        {
            var userId = item.Fields.AdditionalData.ContainsKey(userFieldName) ? item.Fields.AdditionalData[userFieldName]?.ToString() : string.Empty;
            if (!string.IsNullOrEmpty(userId))
            {
                this.User = allUsers.Where(u => u.ID == userId).FirstOrDefault();
            }
        }

        public CourseContact User { get; set; }
    }
}
