using Microsoft.Graph;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyTeams.Common
{
    public static class Extensions
    {
        /// <summary>
        /// Is this just a Date (no time)? Assume if it's midnight exactly, then no.
        /// </summary>
        public static bool HasValidTime(this DateTime dt)
        {
            if (dt.Hour == 0 && dt.Minute == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Does this timex include a time?
        /// </summary>
        public static bool HasValidHoursAndMinutesTime(this TimexProperty timex)
        {
            if (timex.Hour.HasValue && timex.Minute.HasValue)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Does this timex include a date?
        /// </summary>
        public static bool HasValidDate(this TimexProperty timex)
        {
            if (timex.Year.HasValue && timex.Month.HasValue && timex.DayOfMonth.HasValue)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static DateTime GetDateTime(this TimexProperty timex)
        {
            if (timex.HasValidDate())
            {
                DateTime dt = new DateTime(timex.Year.Value, timex.Month.Value, timex.DayOfMonth.Value);
                if (timex.HasValidHoursAndMinutesTime())
                {
                    dt = dt.AddHours(timex.Hour.Value);
                    dt = dt.AddMinutes(timex.Minute.Value);
                }

                return dt;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(timex), "Timex is ambigious");
            }
        }

        public static string ToGraphString(this DateTime dateTime)
        {
            return dateTime.ToString($"{dateTime.Year}-{dateTime.Month}-{dateTime.Day}T" +
                $"{dateTime.Hour:D2}:{dateTime.Minute:D2}:{dateTime.Second:D2}");
        }
        public static string ToGraphString(this DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                return dateTime.Value.ToGraphString();
            }
            else
            {
                return string.Empty;
            }
        }

        public static User FindUserByEmail(this List<User> users, string email) 
        {
            var graphUser = users.Where(u => u.UserPrincipalName.ToLower() == email.ToLower()).FirstOrDefault();
            return graphUser;
        }

    }
}
