using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainingOnboarding.Models
{
    public static class Utils
    {
        public static bool IsNotFoundError(this ServiceException ex)
        {
            if (ex is null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            if (ex.Error?.Code == "itemNotFound")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
