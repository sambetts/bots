using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrainingOnboarding.Models
{

    public class CoursesMetadata
    {
        /// <summary>
        /// Load from SharePoint list results
        /// </summary>
        public CoursesMetadata(IListItemsCollectionPage coursesListItems, IListItemsCollectionPage coursesChecklistListItems,
            IListItemsCollectionPage courseAttendanceList, IListItemsCollectionPage usersList, IListItemsCollectionPage checklistConfirmationsList)
        {
            var allUsers = new List<CourseContact>();
            foreach (var item in usersList)
            {
                allUsers.Add(new CourseContact(item));
            }

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
                course.Attendees.AddRange(allAttendanceItems.Where(a => a.CourseId == course.ID).Select(a => a.User));
            }
        }

        public static async Task<CoursesMetadata> LoadTrainingSPData(GraphServiceClient graphClient, string siteId)
        {
            try
            {
                var allLists = await graphClient.Sites[siteId]
                    .Lists
                    .Request()
                    .GetAsync();

                var coursesList = allLists.Where(l => l.Name == "Courses").SingleOrDefault();
                var courseAttendanceList = allLists.Where(l => l.Name == "Course Attendance").SingleOrDefault();
                var coursesChecklistList = allLists.Where(l => l.Name == "Course Checklist").SingleOrDefault();
                var checklistConfirmationsList = allLists.Where(l => l.Name == "Checklist Confirmations").SingleOrDefault();

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

                var users = await LoadSiteUsers(graphClient, siteId);
                var data = new CoursesMetadata(coursesListTask.Result, coursesChecklistListTask.Result, courseAttendanceListTask.Result, users, checklistConfirmationsListTask.Result);

                return data;
            }
            catch (ServiceException ex)
            {
                throw ex;
            }
        }


        static async Task<IListItemsCollectionPage> LoadSiteUsers(GraphServiceClient graphClient, string siteId)
        {

            var hiddenUserListId = (await graphClient
                            .Sites[siteId]
                            .Lists
                            .Request()
                            .Filter("displayName eq 'User Information List'")
                            .GetAsync())[0].Id;

            return await graphClient.Sites[siteId].Lists[hiddenUserListId].Items.Request().Expand("fields").GetAsync();
        }

        #region Props

        public List<CourseContact> AllUsersAllCourses
        {
            get
            {
                var l = new List<CourseContact>();

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

        public PendingUserActions GetUserActionsWithThingsToDo()
        {
            return GetUserActionsWithThingsToDo(Courses);       // All
        }
        public PendingUserActions GetUserActionsWithThingsToDo(List<Course> courseFitler)
        {
            var usersWithStuffToDoStill = new List<PendingUserActionsForCourse>();


            foreach (var c in Courses.Where(c => courseFitler.Contains(c) && c.Start.HasValue && c.Start.Value > DateTime.Today))
            {
                foreach (var attendee in c.Attendees)
                {
                    var newThingToDo = new PendingUserActionsForCourse { Course = c, User = attendee };

                    foreach (var thingToDo in c.CheckListItems)
                    {
                        if (!thingToDo.CompletedUsers.Contains(attendee))
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
