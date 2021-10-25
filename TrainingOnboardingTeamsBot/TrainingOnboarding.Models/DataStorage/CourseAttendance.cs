using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrainingOnboarding.Models
{
    /// <summary>
    /// Who is on what course & state of user's info for the course
    /// </summary>
    public class CourseAttendance : BaseSPItemWithUser
    {
        public CourseAttendance() { }
        public CourseAttendance(ListItem item, List<SiteUser> allUsers) : base(item, allUsers, "AssignedUserLookupId")
        {
            this.CourseId = GetFieldValue(item, "CourseattendanceID");
            this.QACountry = GetFieldValue(item, "QACountry");
            this.QARole = GetFieldValue(item, "QARole");
            this.QASpareTimeActivities = GetFieldValue(item, "QASpareTimeActivities");
            this.QAMobilePhoneNumber = GetFieldValue(item, "QAMobileNumber");

            var b = GetFieldValue(item, "BotContacted");
            var contacted = false;
            bool.TryParse(b, out contacted);
            this.BotContacted = contacted;
        }

        #region Props

        public string CourseId { get; set; }

        public string QACountry { get; set; }
        public string QARole { get; set; }
        public string QASpareTimeActivities { get; set; }
        public string QAMobilePhoneNumber { get; set; }
        public bool BotContacted { get; set; }

        #endregion

        public async Task SaveChanges(GraphServiceClient graphClient, string siteId)
        {
            var allLists = await graphClient.Sites[siteId]
                    .Lists
                    .Request()
                    .GetAsync();

            var attendenceList = allLists.Where(l => l.Name == ModelConstants.ListNameCourseAttendance).SingleOrDefault();

            ListItem taskItem = null;
            try
            {
                taskItem = (await graphClient
                    .Sites[siteId]
                    .Lists[attendenceList.Id]
                    .Items[this.ID]
                    .Request()
                    .Expand("fields")
                    .GetAsync());
            }
            catch (ServiceException ex)
            {
                if (ex.IsNotFoundError())
                {
                    throw new ArgumentOutOfRangeException(nameof(this.ID), $"No attendence record with ID {ID} found");
                }
                else
                {
                    throw;
                }
            }

            await graphClient
                        .Sites[siteId]
                        .Lists[attendenceList.Id]
                        .Items[this.ID]
                        .Request()
                        .UpdateAsync(new ListItem
                        {
                            Fields = new FieldValueSet
                            {
                                AdditionalData = new Dictionary<string, object>
                                {
                                    {"QACountry", this.QACountry },
                                    {"QARole", this.QARole},
                                    {"QASpareTimeActivities", this.QASpareTimeActivities},
                                    {"QAMobilePhoneNumber", this.QAMobilePhoneNumber},
                                    {"BotContacted", this.BotContacted}
                                }
                            }
                        });

        }

    }
}
