using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoBot.Services
{
    internal class BotConstants
    {
        /// <summary>
        /// The prompt audio name for responder notification.
        /// </summary>
        /// <remarks>
        /// message: "There is an incident occured. Press '1' to join the incident meeting. Press '0' to listen to the instruction again. ".
        /// </remarks>
        public const string NotificationPromptName = "NotificationPrompt";

        /// <summary>
        /// The prompt audio name for responder transfering.
        /// </summary>
        /// <remarks>
        /// message: "Your call will be transferred to the incident meeting. Please don't hang off. ".
        /// </remarks>
        public const string TransferingPromptName = "TransferingPrompt";

        /// <summary>
        /// The prompt audio name for bot incoming calls.
        /// </summary>
        /// <remarks>
        /// message: "You are calling an incident application. It's a sample for incoming call with audio prompt.".
        /// </remarks>
        public const string BotIncomingPromptName = "BotIncomingPrompt";

        /// <summary>
        /// The prompt audio name for bot endpoint incoming calls.
        /// </summary>
        /// <remarks>
        /// message: "You are calling an incident application endpoint. It's a sample for incoming call with audio prompt.".
        /// </remarks>
        public const string BotEndpointIncomingPromptName = "BotEndpointIncomingPrompt";
    }
}
