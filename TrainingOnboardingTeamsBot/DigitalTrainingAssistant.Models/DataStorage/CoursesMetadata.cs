using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Models
{
    /// <summary>
    /// All course data, for everyone.
    /// </summary>
    public class CoursesMetadata
    {
        #region Constructor & Loaders

        public CoursesMetadata()
        { 
        }

        /// <summary>
        /// Load from SharePoint list results
        /// </summary>
        public CoursesMetadata(IListItemsCollectionPage coursesListItems, IListItemsCollectionPage coursesChecklistListItems,
            IListItemsCollectionPage courseAttendanceList, List<SiteUser> allUsers, IListItemsCollectionPage checklistConfirmationsList)
        {

            var allAttendanceItems = new List<CourseAttendance>();
            foreach (var item in courseAttendanceList)
            {
                allAttendanceItems.Add(new CourseAttendance(item, allUsers));
            }

            var allChecklistConfItems = new List<CheckListConfirmation>();
            foreach (var item in checklistConfirmationsList)
            {
                allChecklistConfItems.Add(new CheckListConfirmation(item, allUsers));
            }

            var allCheckListItems = new List<CheckListItem>();
            foreach (var item in coursesChecklistListItems)
            {
                var checkListItem = new CheckListItem(item);
                checkListItem.CompletedUsers = allChecklistConfItems.Where(c => c.CheckListItemId == checkListItem.ID).Select(c => c.User).ToList();
                allCheckListItems.Add(checkListItem);
            }

            foreach (var courseItem in coursesListItems)
            {
                var course = new Course(courseItem, allUsers);
                this.Courses.Add(course);

                var courseCheckListItems = allCheckListItems.Where(l => l.CourseID == course.ID);
                course.CheckListItems.AddRange(courseCheckListItems);
                course.Attendees.AddRange(allAttendanceItems.Where(a => a.CourseId == course.ID));
            }
        }

        /// <summary>
        /// Load metadata from Graph & SharePoint
        /// </summary>
        /// <param name="graphClient">Loader client</param>
        /// <param name="siteId">Graph site ID for SharePoint site with lists</param>
        public static async Task<CoursesMetadata> LoadTrainingSPData(GraphServiceClient graphClient, string siteId)
        {
            try
            {
                var allLists = await graphClient.Sites[siteId]
                    .Lists
                    .Request()
                    .GetAsync();

                var coursesList = allLists.Where(l => l.Name == ModelConstants.ListNameCourses).SingleOrDefault();
                var courseAttendanceList = allLists.Where(l => l.Name == ModelConstants.ListNameCourseAttendance).SingleOrDefault();
                var coursesChecklistList = allLists.Where(l => l.Name == ModelConstants.ListNameCourseChecklist).SingleOrDefault();
                var checklistConfirmationsList = allLists.Where(l => l.Name == ModelConstants.ListNameChecklistConfirmations).SingleOrDefault();

                if (coursesList == null || coursesChecklistList == null || coursesChecklistList == null || checklistConfirmationsList == null)
                {
                    throw new Exception("Missing lists from SharePoint site");
                }

                // Parallel load everything from SP
                var coursesListTask = graphClient.Sites[siteId].Lists[coursesList.Id].Items.Request().Expand("fields").GetAsync();
                var courseAttendanceListTask = graphClient.Sites[siteId].Lists[courseAttendanceList.Id].Items.Request().Expand("fields").GetAsync();
                var coursesChecklistListTask = graphClient.Sites[siteId].Lists[coursesChecklistList.Id].Items.Request().Expand("fields").GetAsync();
                var checklistConfirmationsListTask = graphClient.Sites[siteId].Lists[checklistConfirmationsList.Id].Items.Request().Expand("fields").GetAsync();

                await Task.WhenAll(coursesChecklistListTask, coursesListTask, courseAttendanceListTask, checklistConfirmationsListTask);

                var siteUsers = await LoadSiteUsers(graphClient, siteId);
                var data = new CoursesMetadata(coursesListTask.Result, coursesChecklistListTask.Result, courseAttendanceListTask.Result, siteUsers, checklistConfirmationsListTask.Result);

                return data;
            }
            catch (ServiceException ex)
            {
                throw ex;
            }
        }

        public static async Task<List<SiteUser>> LoadSiteUsers(GraphServiceClient graphClient, string siteId)
        {

            var hiddenUserListId = (await graphClient
                            .Sites[siteId]
                            .Lists
                            .Request()
                            .Filter("displayName eq 'User Information List'")
                            .GetAsync())[0].Id;



            var userItems = await graphClient.Sites[siteId].Lists[hiddenUserListId].Items.Request().Expand("fields").GetAsync();

            var allUsers = new List<SiteUser>();
            foreach (var item in userItems)
            {
                allUsers.Add(new SiteUser(item));
            }

            return allUsers;
        }

        #endregion

        #region Props

        public List<CourseAttendance> AllUsersAllCourses
        {
            get
            {
                var l = new List<CourseAttendance>();

                foreach (var c in Courses)
                {
                    foreach (var a in c.Attendees)
                    {
                        if (!l.Contains(a))
                        {
                            l.Add(a);
                        }
                    }
                }

                return l;
            }
        }

        public List<Course> Courses { get; set; } = new List<Course>();

        #endregion

        /// <summary>
        /// Get outstanding items for all users, all courses.
        /// </summary>
        public PendingUserActions GetUserActionsWithThingsToDo(bool filterByCourseReminderDays)
        {
            return GetUserActionsWithThingsToDo(Courses, filterByCourseReminderDays);       // All courses
        }

        /// <summary>
        /// Get outstanding items for all users for specific courses.
        /// </summary>
        public PendingUserActions GetUserActionsWithThingsToDo(List<Course> courseFitler, bool filterByCourseReminderDays)
        {
            var usersWithStuffToDoStill = new List<PendingUserActionsForCourse>();

            // Look for courses that are in range
            IEnumerable<Course> courses = null;
            if (!filterByCourseReminderDays)
            {
                // Just get courses that haven't started yet
                courses = Courses.Where(c => courseFitler.Contains(c) && c.Start.HasValue && c.Start.Value > DateTime.Today);
            }
            else
            {
                // Get courses that fall in the "days before course start" reminder
                courses = Courses.Where(c => courseFitler.Contains(c) && c.Start.HasValue && c.Start.Value < DateTime.Now.AddDays(c.DaysBeforeToSendReminders));
            }

            foreach (var c in courses)
            {
                foreach (var attendee in c.Attendees)
                {
                    var newThingToDo = new PendingUserActionsForCourse { Course = c, Attendee = attendee };

                    foreach (var thingToDo in c.CheckListItems)
                    {
                        if (!thingToDo.CompletedUsers.Contains(attendee.User))
                        {
                            newThingToDo.PendingItems.Add(thingToDo);
                        }
                    }

                    if (newThingToDo.PendingItems.Count > 0)
                    {
                        usersWithStuffToDoStill.Add(newThingToDo);
                    }
                }
            }

            return new PendingUserActions { Actions = usersWithStuffToDoStill };
        }
    }

}
