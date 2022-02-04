using Microsoft.Graph;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Models
{
    public static class Utils
    {
        public static bool IsItemNotFoundError(this ServiceException ex)
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
