using DigitalTrainingAssistant.Bot.Cards;
using DigitalTrainingAssistant.Bot.Helpers;
using DigitalTrainingAssistant.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalTrainingAssistant.Bot
{
    // https://github.com/microsoft/BotBuilder-Samples/blob/main/samples/csharp_dotnetcore/46.teams-auth/Bots/TeamsBot.cs
    public class TraingOnboardingBot<T> : DialogBot<T> where T : Dialog
    {
        public readonly BotConfig _configuration;
        private readonly BotHelper _helper;
        BotConversationCache _conversationCache = null;

        public TraingOnboardingBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger, BotHelper helper, BotConfig configuration, BotConversationCache botConversationCache)
            : base(conversationState, userState, dialog, logger)
        {
            _helper = helper;
            _conversationCache = botConversationCache;
            _configuration = configuration;
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

        protected override async Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            Logger.LogInformation("Running dialog with signin/verifystate from an Invoke Activity.");

            // The OAuth Prompt needs to see the Invoke Activity in order to complete the login process.

            // Run the Dialog with the new Invoke Activity.
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }



    }
}