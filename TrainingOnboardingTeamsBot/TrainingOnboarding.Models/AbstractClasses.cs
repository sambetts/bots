using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainingOnboarding.Models.Util;

namespace TrainingOnboarding.Models
{
    public abstract class BaseSPItem
    {
        #region Constructors

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
            int id = 0;
            int.TryParse(item.Fields.Id, out id);
            this.ID = id;
        }
        #endregion

        #region SP Parsers

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

        protected bool GetFieldBool(ListItem item, string propName)
        {
            var str = GetFieldValue(item, propName);
            var val = false;
            bool.TryParse(str, out val);
            return val;
        }

        protected int GetFieldInt(ListItem item, string propName)
        {
            var str = GetFieldValue(item, propName);
            var intVal = 0;

            if (!string.IsNullOrEmpty(str))
            {
                if (!int.TryParse(str, out intVal))
                {
                    // Is this a wierd SharePoint "0 decimal places number, shown as 1.00" (for example)?
                    if (StringUtils.IsIntegerReally(str))
                    {
                        return StringUtils.GetIntFromDecimalString(str);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(propName, $"Cannot read integer value for field '{propName}': value read was '{str}'");
                    }
                }
            }
            
            return intVal;
        }

        #endregion

        public int ID { get; set; }

        public virtual bool IsValid => true;
    }

    public abstract class BaseSPItemWithUser : BaseSPItem
    {
        public BaseSPItemWithUser() { }

        protected BaseSPItemWithUser(ListItem item, List<SiteUser> allUsers, string userFieldName) : base(item)
        {
            var userId = GetFieldInt(item, userFieldName);

            if (userId != 0)
            {
                this.User = allUsers.Where(u => u.ID == userId).FirstOrDefault();
            }
        }

        public SiteUser User { get; set; }
    }
}
