using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrainingOnboarding.Models
{
    public class CourseTasksUpdateInfo
    {
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
