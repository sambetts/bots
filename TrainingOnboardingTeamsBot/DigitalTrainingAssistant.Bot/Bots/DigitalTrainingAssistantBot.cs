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

    public class DigitalTrainingAssistantBot<T> : DialogBot<T> where T : Dialog
    {
        public readonly BotConfig _configuration;
        private readonly BotActionsHelper _helper;
        BotConversationCache _conversationCache = null;

        public DigitalTrainingAssistantBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger, BotActionsHelper helper, BotConfig configuration, BotConversationCache botConversationCache)
            : base(conversationState, userState, dialog, logger)
        {
            _helper = helper;
            _conversationCache = botConversationCache;
            _configuration = configuration;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var token = await AuthHelper.GetToken(_configuration.TenantId, _configuration.MicrosoftAppId, _configuration.MicrosoftAppPassword);
            var graphClient = AuthHelper.GetAuthenticatedClient(token);

            // Load all course data from lists
            var courseInfo = await CoursesMetadata.LoadTrainingSPData(graphClient, _configuration.SharePointSiteId);

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    // Is this an Azure AD user?
                    if (string.IsNullOrEmpty(member.AadObjectId))
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text($"Hi, anonynous user. I only work with Azure AD users in Teams normally..."));
                    }
                    else
                    {
                        // Add current user to conversation reference.
                        await _conversationCache.AddConversationReferenceToCache(turnContext.Activity as Activity);

                        // Now figure out if user needs to do something
                        var user = _conversationCache.GetCachedUser(turnContext.Activity.GetConversationReference().User.Id);
                        var pendingTrainingActions = courseInfo.GetUserActionsWithThingsToDo(true).GetActionsByEmail(user.EmailAddress);

                        // Send bot intro if they're on a course
                        if (pendingTrainingActions.Actions.Count > 0)
                        {
                            var introCardAttachment = new BotIntroductionProactiveCard(BotConstants.BotName).GetCardAttachment();
                            await turnContext.SendActivityAsync(MessageFactory.Attachment(introCardAttachment));

                            // Send outstanding tasks
                            try
                            {
                                await _helper.SendCourseIntroAndTrainingRemindersToUser(user, turnContext, cancellationToken, pendingTrainingActions, graphClient);
                            }
                            catch (GraphAccessException ex)
                            {
                                await turnContext.SendActivityAsync(MessageFactory.Text(ex.Message));
                            }
                        }
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