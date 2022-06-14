using Azure;
using Azure.Data.Tables;
using DigitalTrainingAssistant.Models;
using Microsoft.Bot.Schema;
using Microsoft.Graph;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot
{
    public class BotConversationCache
    {
        #region Privates & Constructors

        const string TABLE_NAME = "ConversationCache";
        private ConcurrentDictionary<string, CachedUserAndConversationData> _userIdConversationCache = new();
        private BotConfig _config;
        private TableClient _tableClient;

        public BotConversationCache(BotConfig config)
        {
            _config = config;   
            this._tableClient = new TableClient(
                config.Storage,
                TABLE_NAME);

            // Dev only: make sure the Azure Storage emulator is running or this will fail
            _tableClient.CreateIfNotExists();

            var queryResultsFilter = _tableClient.Query<CachedUserAndConversationData>(filter: $"PartitionKey eq '{CachedUserAndConversationData.PartitionKeyVal}'");
            foreach (var qEntity in queryResultsFilter)
            {
                _userIdConversationCache.AddOrUpdate(qEntity.RowKey, qEntity, (key, newValue) => qEntity);
                Console.WriteLine($"{qEntity.RowKey}: {qEntity}");
            }

        }
        #endregion

        internal async Task RemoveFromCache(string aadObjectId)
        {
            CachedUserAndConversationData u = null;
            if (_userIdConversationCache.TryGetValue(aadObjectId, out u))
            {
                _userIdConversationCache.TryRemove(aadObjectId, out u);
            }

            await _tableClient.DeleteEntityAsync(CachedUserAndConversationData.PartitionKeyVal, aadObjectId);
        }

        /// <summary>
        /// App installed for user & now we have a conversation reference to cache for future chat threads.
        /// </summary>
        public async Task AddConversationReferenceToCache(Activity activity)
        {
            var token = await AuthHelper.GetToken(_config.TenantId, _config.MicrosoftAppId, _config.MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            var conversationReference = activity.GetConversationReference();
            await AddOrUpdateUserAndConversationId(conversationReference, activity.ServiceUrl, graphClient);
        }

        internal async Task AddOrUpdateUserAndConversationId(ConversationReference conversationReference, string serviceUrl, GraphServiceClient graphClient)
        {
            CachedUserAndConversationData u = null;
            if (!_userIdConversationCache.TryGetValue(conversationReference.User.AadObjectId, out u))
            {

                // Have not got in memory cache

                Response<CachedUserAndConversationData> entityResponse = null;
                try
                {
                    entityResponse = _tableClient.GetEntity<CachedUserAndConversationData>(CachedUserAndConversationData.PartitionKeyVal, conversationReference.User.Id);
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
                    u = new CachedUserAndConversationData()
                    {
                        RowKey = conversationReference.User.AadObjectId,
                        ServiceUrl = serviceUrl,
                        EmailAddress = user.UserPrincipalName
                    };
                    u.ConversationId = conversationReference.Conversation.Id;
                    _tableClient.AddEntity(u);
                }
                else
                {
                    u = entityResponse.Value;
                }
            }

            // Update memory cache
            _userIdConversationCache.AddOrUpdate(conversationReference.User.AadObjectId, u, (key, newValue) => u);
        }


        public List<CachedUserAndConversationData> GetCachedUsers()
        {
            return _userIdConversationCache.Values.ToList();
        }

        internal CachedUserAndConversationData GetCachedUser(string aadObjectId)
        {
            return _userIdConversationCache.Values.Where(u => u.RowKey == aadObjectId).SingleOrDefault();
        }

        internal bool ContainsUserId(string aadId)
        {
            return _userIdConversationCache.ContainsKey(aadId);
        }
    }

    /// <summary>
    /// Table storage or memory cache for user
    /// </summary>
    public class CachedUserAndConversationData : ITableEntity
    {
        public static string PartitionKeyVal => "Users";
        public string PartitionKey { get => PartitionKeyVal; set { return; } }

        /// <summary>
        /// Azure AD ID
        /// </summary>
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        /// <summary>
        /// Gets or sets service URL.
        /// </summary>
        public string ServiceUrl { get; set; }

        public string ConversationId { get; set; }
        public string EmailAddress { get; set; }
    }
}
