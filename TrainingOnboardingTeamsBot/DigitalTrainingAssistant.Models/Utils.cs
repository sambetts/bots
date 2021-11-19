using Microsoft.Graph;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Models
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

        public static async Task<List> GetList(string siteId, string listName, GraphServiceClient graphClient)
        {
            var allLists = await graphClient.Sites[siteId]
                                .Lists
                                .Request()
                                .GetAsync();

            var coursesList = allLists.Where(l => l.Name == ModelConstants.ListNameCourses).SingleOrDefault();
            return allLists.Where(l => l.Name == listName).SingleOrDefault();

        }
    }
}
