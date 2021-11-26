using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Models
{
    public class SPCache
    {
        public SPCache(string siteId, GraphServiceClient graphClient)
        {
            SiteId = siteId;
            GraphClient = graphClient;
        }

        private ISiteListsCollectionPage _allListsCache = null;
        public string SiteId { get; }
        public GraphServiceClient GraphClient { get; }



        public async Task<List> GetList(string listName)
        {
            if (string.IsNullOrEmpty(listName))
            {
                throw new ArgumentException($"'{nameof(listName)}' cannot be null or empty.", nameof(listName));
            }

            if (_allListsCache == null)
            {
                _allListsCache = await GraphClient.Sites[SiteId]
                                    .Lists
                                    .Request()
                                    .GetAsync();
            }

            var list = _allListsCache.Where(l => l.DisplayName.ToLower() == listName.ToLower()).SingleOrDefault();
            if (list == null)
            {
                throw new ArgumentOutOfRangeException(nameof(listName), $"No list found with name '{listName}'");
            }

            return list;
        }
    }
}
