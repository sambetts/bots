using Microsoft.Bot.Builder;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TrainingOnboarding.Models
{
    public class CourseTasksUpdateInfo
    {
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

        public async Task SendReply(ITurnContext<Microsoft.Bot.Schema.IMessageActivity> turnContext, CancellationToken cancellationToken, string appId, string appPassword, string siteId)
        {
            if (this.HasChanges)
            {
                var token = await AuthHelper.GetToken(turnContext.Activity.Conversation.TenantId, appId, appPassword);
                var graphClient = AuthHelper.GetAuthenticatedClient(token);

                // Save to SP list
                var updateCount = await this.SaveChanges(graphClient, siteId);

                await turnContext.SendActivityAsync(MessageFactory.Text(
                    $"Updated {updateCount} tasks as complete - thanks for getting ready!"
                ), cancellationToken);
            }
            else
            {

                await turnContext.SendActivityAsync(MessageFactory.Text(
                    $"Nothing finished from this list?"
                ), cancellationToken);
            }
        }

        public bool HasChanges => this.ConfirmedTaskIds.Count > 0;

        public List<int> ConfirmedTaskIds { get; set; } = new List<int>();

        public string UserAadObjectId { get; set; }


        public async Task<int> SaveChanges(GraphServiceClient graphClient, string siteId)
        {
            if (string.IsNullOrEmpty(UserAadObjectId))
            {
                throw new ArgumentNullException(nameof(UserAadObjectId));
            }

            var allLists = await graphClient.Sites[siteId]
                    .Lists
                    .Request()
                    .GetAsync();

            var checklistConfirmationsList = allLists.Where(l => l.Name == ModelConstants.ListNameChecklistConfirmations).SingleOrDefault();
            var hiddenUserListId = (await graphClient
                .Sites[siteId]
                .Lists
                .Request()
                .Filter($"displayName eq '{ModelConstants.ListNameUserInformationList}'")
                .GetAsync())[0].Id;

            // Doesn't work: var hiddenUserListId = allLists.Where(l => l.DisplayName == ModelConstants.ListNameUserInformationList).SingleOrDefault().Id;
            var checkListItemListId = allLists.Where(l => l.DisplayName == ModelConstants.ListNameCourseChecklist).SingleOrDefault().Id;

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
                    if (ex.IsNotFoundError())
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

                await graphClient
                    .Sites[siteId]
                    .Lists["Checklist Confirmations"]
                    .Items
                    .Request()
                    .AddAsync(confirmationItem);
            }

            return ConfirmedTaskIds.Count;
        }
    }
}
