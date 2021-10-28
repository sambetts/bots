using EasyTeamsBot.Common;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyTeams.Common
{
    /// <summary>
    /// To cache Graph object lookups - users, etc.
    /// </summary>
    public class TeamsObjectCache
    {
        private UserCache userCache = null;
        public TeamsObjectCache(TeamsManager teamsManager)
        {
            this.Manager = teamsManager;
            userCache = new UserCache(teamsManager);
        }

        public TeamsManager Manager { get; set; }

        public async Task<User> GetUser(string email)
        {
            return await userCache.GetResource(email);
        }

        #region Cache Classes

        /// <summary>
        /// Base implementation for Graph object caches. Used to cache various lookup types. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal abstract class GraphLookupCache<T> where T : class
        {
            protected TeamsManager TeamsManager { get; set; }
            private Dictionary<string, T> ObjectCache { get; set; }
            public GraphLookupCache(TeamsManager teamsManager)
            {
                this.ObjectCache = new Dictionary<string, T>();
                this.TeamsManager = teamsManager;
            }

            /// <summary>
            /// Loads from cache, or if doesn't exist in cache, from DB & adds to cache for next time.
            /// </summary>
            public async Task<T> GetResource(string key)
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException(nameof(key));
                }
                if (!ObjectCache.ContainsKey(key))
                {
                    T dbObj = await LoadFromGraph(key);
                    ObjectCache.Add(key, dbObj);
                }
                return ObjectCache[key];
            }

            public abstract Task<T> LoadFromGraph(string searchKey);
        }

        internal class UserCache : GraphLookupCache<User>
        {
            public UserCache(TeamsManager teamsManager) : base(teamsManager) { }
            public async override Task<User> LoadFromGraph(string emailAddress)
            {
                var user = await base.TeamsManager.Client.Users[emailAddress].Request().GetAsync();
                return user;
            }
        }
        #endregion
    }
}
