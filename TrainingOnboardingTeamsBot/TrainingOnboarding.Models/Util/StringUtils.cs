using System;
using System.Collections.Generic;
using System.Text;

namespace TrainingOnboarding.Models.Util
{
    public class StringUtils
    {
        /// <summary>
        /// Checks if "1.00" is really an integer. This is how SharePoint sometimes send numbers, even with configuration set to not.
        /// </summary>
        public static bool IsIntegerReally(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            decimal dec = 0;
            if (decimal.TryParse(input, out dec))
            {
                return dec % 1 == 0;
            }

            return false;
        }

        public static int GetIntFromDecimalString(string input)
        {
            if (!IsIntegerReally(input))
            {
                throw new ArgumentOutOfRangeException(nameof(input), "Input string isn't an integer");
            }

            decimal dec = 0;
            decimal.TryParse(input, out dec);

            return (int)Math.Round(dec, 0);
        }
    }
}
