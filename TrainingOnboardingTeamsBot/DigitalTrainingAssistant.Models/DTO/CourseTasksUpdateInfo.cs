using Microsoft.Bot.Builder;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Models
{
    public class CourseTasksUpdateInfo : ActionResponse
    {
        #region Constructors

        public CourseTasksUpdateInfo() { }
        public CourseTasksUpdateInfo(string json, string userAadObjectId) 
        {
            this.UserAadObjectId = userAadObjectId;

            // Enum the JSon dynamically to discover properties
            var d = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
            foreach (var item in d)
            {
                if (item.Key != null && item.Key.StartsWith("chk-"))
                {
                    var requirementdIdString = item.Key.TrimStart("chk-".ToCharArray());
                    var requirementdId = 0;
                    var done = false;
                    bool.TryParse(item.Value, out done);
                    int.TryParse(requirementdIdString, out requirementdId);
                    if (done && requirementdId != 0)
                    {
                        this.ConfirmedTaskIds.Add(requirementdId);
                    }
                }
            }
            
        }
        #endregion

        public async Task SendReply(ITurnContext turnContext, CancellationToken cancellationToken, string appId, string appPassword, string tenantId, string siteId)
        {
            if (this.HasChanges)
            {
                var token = await AuthHelper.GetToken(tenantId, appId, appPassword);
                var graphClient = AuthHelper.GetAuthenticatedClient(token);

                // Save to SP list
                await this.SaveChanges(graphClient, siteId);

                await turnContext.SendActivityAsync(MessageFactory.Text(
                    $"Updated tasks as complete - thanks for getting ready!"
                ), cancellationToken);
            }
            else
            {

                await turnContext.SendActivityAsync(MessageFactory.Text(
                    $"Nothing finished from this list?"
                ), cancellationToken);
            }
        }

        #region Props

        public bool HasChanges => this.ConfirmedTaskIds.Count > 0;

        public List<int> ConfirmedTaskIds { get; set; } = new List<int>();

        public string UserAadObjectId { get; set; }

        #endregion

        /// <summary>
        /// Save to SharePoint
        /// </summary>
        public async Task SaveChanges(GraphServiceClient graphClient, string siteId)
        {
            if (string.IsNullOrEmpty(UserAadObjectId))
            {
                throw new ArgumentNullException(nameof(UserAadObjectId));
            }

            var spCache = new SPCache(siteId, graphClient);

            var hiddenUserListId = (await graphClient
                .Sites[siteId]
                .Lists
                .Request()
                .Filter($"displayName eq '{ModelConstants.ListNameUserInformationList}'")
                .GetAsync())[0].Id;


            var checkListItemList = await spCache.GetList(ModelConstants.ListNameCourseChecklist);
            var checkListItemListId = checkListItemList.Id;

            var user = await graphClient.Users[UserAadObjectId].Request().GetAsync();
            var userLookupId = (await graphClient
                .Sites[siteId]
                .Lists[hiddenUserListId]
                .Items
                .Request()
                .Header("Prefer", "HonorNonIndexedQueriesWarningMayFailRandomly")
                .Filter($"fields/UserName eq '{user.UserPrincipalName}'")
                .GetAsync())[0].Id;

            foreach (var taskIdCompleted in ConfirmedTaskIds)
            {
                ListItem taskItem = null;
                try
                {
                    taskItem = (await graphClient
                        .Sites[siteId]
                        .Lists[checkListItemListId]
                        .Items[taskIdCompleted.ToString()]
                        .Request()
                        .Expand("fields")
                        .GetAsync());
                }
                catch (ServiceException ex)
                {
                    if (ex.IsItemNotFoundError())
                    {
                        throw new ArgumentOutOfRangeException(nameof(this.ConfirmedTaskIds), $"No task with ID {taskIdCompleted} found");
                    }
                    else
                    {
                        throw;
                    }
                }
                

                var task = new CheckListItem(taskItem);

                var confirmationItem = new ListItem
                {
                    Fields = new FieldValueSet
                    {
                        AdditionalData = new Dictionary<string, object>
                        {
                            {"Title", $"{user.DisplayName} confirmed task '{task.Requirement}' has been done"},
                            {"CheckListID", taskIdCompleted},
                            {"DoneByLookupId", userLookupId}
                        }
                    }
                };

                var checkListConfirmationList = await spCache.GetList(ModelConstants.ListNameChecklistConfirmations);
                var checkListConfirmationListId = checkListConfirmationList.Id;

                await graphClient
                    .Sites[siteId]
                    .Lists[checkListConfirmationListId]
                    .Items
                    .Request()
                    .AddAsync(confirmationItem);
            }
        }
    }
}
