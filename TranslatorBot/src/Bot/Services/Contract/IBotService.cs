
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Client;
using TranslatorBot.Services.Bot;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TranslatorBot.Model.Models;
using System;

namespace TranslatorBot.Services.Contract
{
    /// <summary>
    /// Interface IBotService
    /// Implements the <see cref="RecordingBot.Model.Contracts.IInitializable" />
    /// </summary>
    /// <seealso cref="RecordingBot.Model.Contracts.IInitializable" />
    public interface IBotService : IInitializable
    {
        /// <summary>
        /// Gets the collection of call handlers.
        /// </summary>
        /// <value>The call handlers.</value>
        ConcurrentDictionary<string, CallHandler> CallHandlers { get; }

        /// <summary>
        /// Gets the entry point for stateful bot.
        /// </summary>
        /// <value>The client.</value>
        ICommunicationsClient Client { get; }

        /// <summary>
        /// End a particular call.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns>The <see cref="Task" />.</returns>
        Task EndCallByCallLegIdAsync(string callLegId);

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall" /> that was requested to join.</returns>
        Task<ICall> JoinCallAsync(JoinCallBody joinCallBody);

        ConcurrentDictionary<Guid, ILanguageSettings> CallLanguageSettings { get; }

        void Dispose();
    }
}
