using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrainingOnboarding.Bot.Cards;
using TrainingOnboarding.Bot.Helpers;
using TrainingOnboarding.Bot.Models;
using TrainingOnboarding.Models;

namespace TrainingOnboarding.Bot
{
    public class TraingOnboardingBot : TeamsActivityHandler
    {
        public readonly BotConfig _configuration;
        private readonly BotHelper _helper;

        BotConversationCache _conversationCache = null;

        public TraingOnboardingBot(BotHelper helper, BotConfig configuration, BotConversationCache botConversationCache)
        {
            _helper = helper;
            _conversationCache = botConversationCache;
            _configuration = configuration;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            turnContext.Activity.RemoveRecipientMention();
            var text = !string.IsNullOrEmpty(turnContext.Activity.Text?.Trim().ToLower()) ? turnContext.Activity.Text?.Trim().ToLower() : string.Empty;
            var val = turnContext.Activity.Value;

            if (string.IsNullOrEmpty(text) && val != null)
            {
                await HandleCardResponse(val.ToString(), turnContext, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(text))
            {
                await HandleUserTextChat(text, turnContext, cancellationToken);

            }
        }

        private async Task HandleUserTextChat(string text, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (text.Contains("remind"))
            {
                var coursesFound = await _helper.RemindClassMembersWithOutstandingTasks(turnContext, cancellationToken, false);

                await turnContext.SendActivityAsync(MessageFactory.Text(
                    $"Found {coursesFound.Actions.Count} outstanding action(s) for {coursesFound.UniqueUsers.Count} user(s), " +
                    $"across {coursesFound.UniqueCourses.Count} course(s) that you are the trainer for. All users notified."
                    ), cancellationToken);
            }
        }

        /// <summary>
        /// Someone replied via an Adaptive card form
        /// </summary>
        private async Task HandleCardResponse(string submitJson, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Form action
            ActionResponse r = null;

            try
            {
                r = JsonConvert.DeserializeObject<ActionResponse>(submitJson);
            }
            catch (JsonException)
            {
                return;
            }

            // Figure out what was done
            if (r.Action == CardConstants.CardActionValLearnerTasksDone)
            {
                var update = new CourseTasksUpdateInfo(submitJson, turnContext.Activity.From.AadObjectId);
                await update.SendReply(turnContext, cancellationToken, _configuration.MicrosoftAppId, _configuration.MicrosoftAppPassword, _configuration.SharePointSiteId);
                
            }
            else if (r.Action == CardConstants.CardActionValStartIntroduction)
            {
                var spAction = JsonConvert.DeserializeObject<ActionResponseForSharePointItem>(submitJson);

                var token = await AuthHelper.GetToken(turnContext.Activity.Conversation.TenantId, _configuration.MicrosoftAppId, _configuration.MicrosoftAppPassword);
                var graphClient = AuthHelper.GetAuthenticatedClient(token);

                var attendanceInfo = await CourseAttendance.LoadById(graphClient, _configuration.SharePointSiteId, spAction.SPID);
                await turnContext.SendActivityAsync(MessageFactory.Attachment(new AttendeeFixedQuestionsInputCard(attendanceInfo).GetCard()));
            }
            else if (r.Action == CardConstants.CardActionValSaveIntroductionQuestions)
            {
                var introductionData = JsonConvert.DeserializeObject<IntroduceYourselfResponse>(submitJson);

                var token = await AuthHelper.GetToken(turnContext.Activity.Conversation.TenantId, _configuration.MicrosoftAppId, _configuration.MicrosoftAppPassword);
                var graphClient = AuthHelper.GetAuthenticatedClient(token);

                var attendanceInfo = await CourseAttendance.LoadById(graphClient, _configuration.SharePointSiteId, introductionData.SPID);
                if (introductionData.IsValid)
                {
                    // Save intro data
                    attendanceInfo.QACountry = introductionData.Country;
                    attendanceInfo.QAMobilePhoneNumber = introductionData.MobilePhoneNumber;
                    attendanceInfo.QAOrg = introductionData.Org;
                    attendanceInfo.QARole = introductionData.Role;
                    attendanceInfo.QASpareTimeActivities = introductionData.SpareTimeActivities;
                    attendanceInfo.IntroductionDone = true;

                    await attendanceInfo.SaveChanges(graphClient, _configuration.SharePointSiteId);

                    // Send back to user for now
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(new AttendeeFixedQuestionsPublicationCard(attendanceInfo).GetCard()));
                }
                else
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(
                        $"Oops, that doesn't seem right - check the values & try again?"
                        ), cancellationToken);
                }
            }

        }


        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var token = await AuthHelper.GetToken(turnContext.Activity.Conversation.TenantId, _configuration.MicrosoftAppId, _configuration.MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            // Load all course data from lists
            var courseInfo = await CoursesMetadata.LoadTrainingSPData(graphClient, _configuration.SharePointSiteId);

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    // Add current user to conversation reference.
                    await _helper.AddConversationReference(turnContext.Activity as Activity);

                    // Now figure out if user needs to do something
                    var user = _conversationCache.GetCachedUser(turnContext.Activity.GetConversationReference().User.AadObjectId);
                    var pendingTrainingActions = courseInfo.GetUserActionsWithThingsToDo(true).GetActionsByEmail(user.EmailAddress);

                    // Send bot intro if they're on a course
                    if (pendingTrainingActions.Actions.Count > 0)
                    {
                        var introCardAttachment = new BotWelcomeCard(BotConstants.BotName).GetCard();
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(introCardAttachment));

                        // Send outstanding tasks
                        await _helper.SendCourseIntroAndTrainingRemindersToUser(user, turnContext, cancellationToken, pendingTrainingActions, graphClient);
                    }
                }
            }
        }
    }
}