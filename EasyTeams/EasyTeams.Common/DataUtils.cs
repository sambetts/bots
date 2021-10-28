using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EasyTeams.Common
{
    public class DataUtils
    {
        public static string ExtractEmailFromContact(string selectedContact)
        {
            if (string.IsNullOrEmpty(selectedContact))
            {
                throw new ArgumentNullException(nameof(selectedContact));
            }

            var emailRegex = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", RegexOptions.IgnoreCase);

            // Find items that matches with our pattern
            var emailMatches = emailRegex.Matches(selectedContact);

            if (emailMatches.Count == 1)
            {
                return emailMatches[0].Value;
            }

            throw new FormatException("Unexpected string format - no single email address found");

        }
    }
}
