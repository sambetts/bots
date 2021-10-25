using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainingOnboarding.Models
{
    public abstract class BaseSPItem
    {
        public BaseSPItem() { }

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

        protected string GetFieldValue(ListItem item, string propName)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (string.IsNullOrEmpty(propName))
            {
                throw new ArgumentException($"'{nameof(propName)}' cannot be null or empty.", nameof(propName));
            }

            if (item.Fields.AdditionalData.ContainsKey(propName))
            {
                return item.Fields.AdditionalData[propName]?.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        public string ID { get; set; }

        public virtual bool IsValid => true;
    }

    public abstract class BaseSPItemWithUser : BaseSPItem
    {
        public BaseSPItemWithUser() { }

        protected BaseSPItemWithUser(ListItem item, List<SiteUser> allUsers, string userFieldName) : base(item)
        {
            var userId = item.Fields.AdditionalData.ContainsKey(userFieldName) ? item.Fields.AdditionalData[userFieldName]?.ToString() : string.Empty;
            if (!string.IsNullOrEmpty(userId))
            {
                this.User = allUsers.Where(u => u.ID == userId).FirstOrDefault();
            }
        }

        public SiteUser User { get; set; }
    }
}
