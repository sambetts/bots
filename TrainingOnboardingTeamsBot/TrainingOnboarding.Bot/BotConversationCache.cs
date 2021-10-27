using Azure;
using Azure.Data.Tables;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrainingOnboarding.Bot
{
    public class BotConversationCache
    {
        const string TABLE_NAME = "ConversationCache";
        public BotConversationCache(IConfiguration configuration)
        {
            this.TableClient = new TableClient(
                configuration["StorageConnectionString"],
                TABLE_NAME);

            TableClient.CreateIfNotExists();

            var botId = configuration["AppCatalogTeamAppId"];

            var queryResultsFilter = TableClient.Query<CachedUserData>(filter: $"PartitionKey eq '{CachedUserData.PartitionKeyVal}'");
            foreach (var qEntity in queryResultsFilter)
            {
                _userIdConversationCache.AddOrUpdate(qEntity.RowKey, qEntity, (key, newValue) => qEntity);
                Console.WriteLine($"{qEntity.RowKey}: {qEntity}");
            }

        }

        private ConcurrentDictionary<string, CachedUserData> _userIdConversationCache = new ConcurrentDictionary<string, CachedUserData>();

        private TableClient TableClient { get; set; }
        public int RefrenceCount => _userIdConversationCache.Count;

        internal async Task AddOrUpdateUserAndConversationId(ConversationReference conversationReference, string serviceUrl, GraphServiceClient graphClient)
        {
            CachedUserData u = null; 
            if (!_userIdConversationCache.TryGetValue(conversationReference.User.AadObjectId, out u))
            {

                // Have not got in memory cache

                Response<CachedUserData> entityResponse = null;
                try
                {
                    entityResponse = TableClient.GetEntity<CachedUserData>(CachedUserData.PartitionKeyVal, conversationReference.User.Id);
                }
                catch (RequestFailedException ex)
                {
                    if (ex.ErrorCode == "ResourceNotFound")
                    {
                        // No worries
                    }
                    else
                    {
                        throw;
                    }
                }

                if (entityResponse == null)
                {
                    var user = await graphClient.Users[conversationReference.User.AadObjectId].Request().GetAsync();

                    // Not in storage account either. Add there
                    u = new CachedUserData() 
                    { 
                        RowKey = conversationReference.User.AadObjectId, 
                        ServiceUrl = serviceUrl, 
                        EmailAddress = user.UserPrincipalName 
                    };
                    u.ConversationId = conversationReference.Conversation.Id;
                    TableClient.AddEntity(u);
                }
                else
                {
                    u = entityResponse.Value;
                }
            }

            // Update memory cache
            _userIdConversationCache.AddOrUpdate(conversationReference.User.AadObjectId, u, (key, newValue) => u);
        }


        public List<CachedUserData> GetCachedUsers()
        {
            return _userIdConversationCache.Values.ToList();
        }

        internal CachedUserData GetCachedUser(string aadObjectId)
        {
            return _userIdConversationCache.Values.Where(u=> u.RowKey == aadObjectId).SingleOrDefault();
        }

        internal bool ContainsUserId(string aadId)
        {
            return _userIdConversationCache.ContainsKey(aadId);
        }
    }

    public class CachedUserData : ITableEntity
    {
        public static string PartitionKeyVal => "Users";
        public string PartitionKey { get => PartitionKeyVal; set { return; } }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }

        /// <summary>
        /// Gets or sets service URL.
        /// </summary>
        public string ServiceUrl { get; set; }

        public string ConversationId { get; set; }
        public string EmailAddress { get; set; }
    }
}
