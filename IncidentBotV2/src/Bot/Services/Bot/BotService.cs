
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Skype.Bots.Media;
using TranslatorBot.Model.Models;
using TranslatorBot.Services.Contract;
using TranslatorBot.Services.ServiceSetup;
using TranslatorBot.Services.Util;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TranslatorBot.Model.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Common.Authentication;
using Sample.IncidentBot.Bot;
using System.Collections.Generic;
using Sample.IncidentBot.Data;
using Sample.IncidentBot.IncidentStatus;
using Sample.Common.OnlineMeetings;
using Sample.Common.Utils;
using System.Linq;
using EchoBot.Services;

namespace TranslatorBot.Services.Bot
{
    /// <summary>
    /// Class BotService.
    /// Implements the <see cref="System.IDisposable" />
    /// Implements the <see cref="TranslatorBot.Services.Contract.IBotService" />
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="TranslatorBot.Services.Contract.IBotService" />
    public class BotService : IDisposable, IBotService
    {
        /// <summary>
        /// The Graph logger
        /// </summary>
        private readonly IGraphLogger _graphLogger;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The settings
        /// </summary>
        private readonly AppSettings _settings;

        private readonly AzureSettings _azureSettings;

        /// <summary>
        /// Gets the collection of call handlers.
        /// </summary>
        /// <value>The call handlers.</value>
        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();

        /// <summary>
        /// Gets the entry point for stateful bot.
        /// </summary>
        /// <value>The client.</value>
        public ICommunicationsClient Client { get; private set; }

        /// <summary>
        /// Gets the incident manager.
        /// </summary>
        public IncidentStatusManager IncidentStatusManager { get; }
        public OnlineMeetingHelper OnlineMeetings { get; }

        /// <summary>
        /// Gets the prompts dictionary.
        /// </summary>
        public Dictionary<string, MediaPrompt> MediaMap { get; } = new Dictionary<string, MediaPrompt>();
        /// <inheritdoc />
        public void Dispose()
        {
            this.Client?.Dispose();
            this.Client = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BotService" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="settings">The settings.</param>
        public BotService(
            IGraphLogger graphLogger,
            ILogger<BotService> logger,
            IOptions<AppSettings> settings,
            IAzureSettings azureSettings)
        {
            _graphLogger = graphLogger;
            _logger = logger;
            _settings = settings.Value;
            _azureSettings = (AzureSettings)azureSettings;

            this.IncidentStatusManager = new IncidentStatusManager();

            var name = this.GetType().Assembly.GetName().Name;

            var authProvider = new AuthenticationProvider(
                name,
                _settings.AadAppId,
                _settings.AadAppSecret,
                graphLogger);
            this.OnlineMeetings = new OnlineMeetingHelper(authProvider, new Uri("https://graph.microsoft.com/v1.0"));
        }

        /// <summary>
        /// Initialize the instance.
        /// </summary>
        public void Initialize()
        {
            _logger.LogInformation("Initializing Bot Service");


            var audioBaseUri = new Uri(_settings.BotBaseUrl);
            this.MediaMap[BotConstants.TransferingPromptName] = new MediaPrompt
            {
                MediaInfo = new MediaInfo
                {
                    Uri = new Uri(audioBaseUri, "audio/responder-transfering.wav").ToString(),
                    ResourceId = Guid.NewGuid().ToString(),
                },
            };

            this.MediaMap[BotConstants.NotificationPromptName] = new MediaPrompt
            {
                MediaInfo = new MediaInfo
                {
                    Uri = new Uri(audioBaseUri, "audio/responder-notification.wav").ToString(),
                    ResourceId = Guid.NewGuid().ToString(),
                },
            };

            this.MediaMap[BotConstants.BotIncomingPromptName] = new MediaPrompt
            {
                MediaInfo = new MediaInfo
                {
                    Uri = new Uri(audioBaseUri, "audio/bot-incoming.wav").ToString(),
                    ResourceId = Guid.NewGuid().ToString(),
                },
            };

            this.MediaMap[BotConstants.BotEndpointIncomingPromptName] = new MediaPrompt
            {
                MediaInfo = new MediaInfo
                {
                    Uri = new Uri(audioBaseUri, "audio/bot-endpoint-incoming.wav").ToString(),
                    ResourceId = Guid.NewGuid().ToString(),
                },
            };

            var name = this.GetType().Assembly.GetName().Name;
            var builder = new CommunicationsClientBuilder(
                name,
                _settings.AadAppId,
                _graphLogger);

            var authProvider = new AuthenticationProvider(
                name,
                _settings.AadAppId,
                _settings.AadAppSecret,
                _graphLogger);

            builder.SetAuthenticationProvider(authProvider);
            builder.SetNotificationUrl(_azureSettings.CallControlBaseUrl);
            builder.SetMediaPlatformSettings(_azureSettings.MediaPlatformSettings);
            builder.SetServiceBaseUrl(new Uri(AppConstants.PlaceCallEndpointUrl));

            this.Client = builder.Build();
            this.Client.Calls().OnIncoming += this.CallsOnIncoming;
            this.Client.Calls().OnUpdated += this.CallsOnUpdated;
        }

        /// <summary>
        /// End a particular call.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns>The <see cref="Task" />.</returns>
        public async Task EndCallByCallLegIdAsync(string callLegId)
        {
            try
            {
                await this.GetHandlerOrThrow(callLegId).Call.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Manually remove the call from SDK state.
                // This will trigger the ICallCollection.OnUpdated event with the removed resource.
                this.Client.Calls().TryForceRemove(callLegId, out ICall _);
            }
        }

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall" /> that was requested to join.</returns>
        public async Task<ICall> JoinCallAsync(JoinCallRequestData joinCallBody, string incidentId = "")
        {
            // A tracking id for logging purposes. Helps identify this call in logs.
            var scenarioId = string.IsNullOrEmpty(joinCallBody.ScenarioId) ? Guid.NewGuid() : new Guid(joinCallBody.ScenarioId);

            Microsoft.Graph.MeetingInfo meetingInfo;
            ChatInfo chatInfo;
            if (!string.IsNullOrWhiteSpace(joinCallBody.VideoTeleconferenceId))
            {
                // Meeting id is a cloud-video-interop numeric meeting id.
                var onlineMeeting = await this.OnlineMeetings
                    .GetOnlineMeetingAsync(joinCallBody.TenantId, joinCallBody.VideoTeleconferenceId, scenarioId)
                    .ConfigureAwait(false);

                meetingInfo = new OrganizerMeetingInfo { Organizer = onlineMeeting.Participants.Organizer.Identity, };
                chatInfo = onlineMeeting.ChatInfo;
            }
            else
            {
                (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(joinCallBody.JoinURL);
            }

            var tenantId =
                joinCallBody.TenantId ??
                (meetingInfo as OrganizerMeetingInfo)?.Organizer.GetPrimaryIdentity()?.GetTenantId();
            var mediaToPrefetch = new List<MediaInfo>();
            //foreach (var m in this.MediaMap)
            //{
            //    mediaToPrefetch.Add(m.Value.MediaInfo);
            //}

            var joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, new[] { Modality.Audio }, mediaToPrefetch)
            {
                TenantId = tenantId,
            };

            var statefulCall = await this.Client.Calls().AddAsync(joinParams, scenarioId).ConfigureAwait(false);

            this.AddCallToHandlers(statefulCall, new IncidentCallContext(IncidentCallType.BotMeeting, incidentId));

            _graphLogger.Info($"Join Call complete: {statefulCall.Id}");

            return statefulCall;
        }



        /// <summary>
        /// Raise an incident.
        /// </summary>
        /// <param name="incidentRequestData">The incident data.</param>
        /// <returns>The task for await.</returns>
        public async Task<ICall> RaiseIncidentAsync(IncidentRequestData incidentRequestData)
        {
            // A tracking id for logging purposes. Helps identify this call in logs.
            var scenarioId = string.IsNullOrEmpty(incidentRequestData.ScenarioId) ? Guid.NewGuid() : new Guid(incidentRequestData.ScenarioId);

            string incidentId = Guid.NewGuid().ToString();

            var incidentStatusData = new IncidentStatusData(incidentId, incidentRequestData);

            var incident = this.IncidentStatusManager.AddIncident(incidentId, incidentStatusData);

            var botMeetingCall = await this.JoinCallAsync(incidentRequestData, incidentId).ConfigureAwait(false);

            // Rehydrates and validates the group call.
            botMeetingCall = await this.RehydrateAndValidateGroupCallAsync(this.Client, botMeetingCall).ConfigureAwait(false);

            foreach (var objectId in incidentRequestData.ObjectIds)
            {
                var makeCallRequestData =
                    new MakeCallRequestData(
                        incidentRequestData.TenantId,
                        objectId,
                        "Application".Equals(incidentRequestData.ResponderType, StringComparison.OrdinalIgnoreCase));
                var responderCall = await this.MakeCallAsync(makeCallRequestData, scenarioId).ConfigureAwait(false);
                this.AddCallToHandlers(responderCall, new IncidentCallContext(IncidentCallType.ResponderNotification, incidentId));
            }

            return botMeetingCall;
        }


        /// <summary>
        /// Add call to call handlers.
        /// </summary>
        /// <param name="call">The call to be added.</param>
        /// <param name="incidentCallContext">The incident call context.</param>
        private void AddCallToHandlers(ICall call, IncidentCallContext incidentCallContext)
        {
            Validator.NotNull(incidentCallContext, nameof(incidentCallContext));

            var statusData = this.IncidentStatusManager.GetIncident(incidentCallContext.IncidentId);

            CallHandler callHandler;
            InvitationParticipantInfo callee;
            switch (incidentCallContext.CallType)
            {
                case IncidentCallType.BotMeeting:
                    // Call to meeting.
                    callHandler = new MeetingCallHandler(call, this, statusData);
                    break;
                case IncidentCallType.ResponderNotification:
                    // call to an user.
                    callee = call.Resource.Targets.First();
                    callHandler = new ResponderCallHandler(call, this, callee.Identity.User.Id, statusData);
                    break;
                case IncidentCallType.BotIncoming:
                    // call from an user.
                    callHandler = new IncomingCallHandler(call, this, null /* The app endpoint ID */);
                    break;
                case IncidentCallType.BotEndpointIncoming:
                    // call from an user to an bot endpoint.
                    callee = call.Resource.Targets.First();
                    callHandler = new IncomingCallHandler(call, this, callee.Identity.GetApplicationInstance().Id);
                    break;
                default:
                    throw new NotSupportedException($"Invalid call type in incident call context: {incidentCallContext.CallType}");
            }

            this.CallHandlers[call.Id] = callHandler;
        }


        /// <summary>
        /// Rehydrates and validates the group call.
        /// </summary>
        /// <param name="client">The communications client.</param>
        /// <param name="call">The call to validate.</param>
        /// <returns>The rehydrated call.</returns>
        private async Task<ICall> RehydrateAndValidateGroupCallAsync(ICommunicationsClient client, ICall call)
        {
            // Wait for roster so we ensure that events were already raised.
            await call.Participants.WaitForParticipantAsync(call.Resource.MyParticipantId).ConfigureAwait(false);

            // Remove call from memory.
            this.Client.Calls().TryForceRemove(call.Id, out ICall removedCall);
            _graphLogger.Info($"Check whether the removed call is desired: {call == removedCall}");
            _graphLogger.Info($"Check whether the call get removed: {this.Client.Calls()[removedCall.Id] == null}");

            // Rehydrate here... after this point all the data should be rebuilt
            // This calls:
            // GET /communications/calls/{id}
            // GET /communications/calls/{id}/participants
            // GET /communications/calls/{id}/audioRoutingGroups
            var tenantId = call.TenantId;
            var scenarioId = call.ScenarioId;
            await client.RehydrateAsync(removedCall.ResourcePath, tenantId, scenarioId).ConfigureAwait(false);

            var rehydratedCall = client.Calls()[removedCall.Id];
            _graphLogger.Info($"Check whether the call get rehydrated: {rehydratedCall != null}");
            _graphLogger.Info($"Check whether the rehydrated call is a new object: {removedCall == rehydratedCall}");

            // deployments and Graph is stripping out some parameters.
            // E2EAssert.IsContentEqual(removedCall.Resource, rehydratedCall.Resource);
            var myParticipant = rehydratedCall.Participants[removedCall.Resource.MyParticipantId];
            _graphLogger.Info($"Check whether myParticipant get rehydrated: {myParticipant != null}");

            // Remove participant from memory.
            rehydratedCall.Participants.TryForceRemove(call.Resource.MyParticipantId, out IParticipant removedParticipant);
            _graphLogger.Info($"Check whether participant get removed from memory: {rehydratedCall.Participants[call.Resource.MyParticipantId] == null}");

            // Rehydrate here... after this point the participant should be rebuilt.
            // This calls:
            // GET /communications/calls/{id}/participants/{id}
            var rehydratedParticipant = await rehydratedCall.Participants.GetAsync(call.Resource.MyParticipantId).ConfigureAwait(false);
            _graphLogger.Info($"Check whether participant get rehydrated: {rehydratedParticipant != null}");
            _graphLogger.Info($"Check whether the rehydrated participant is a new object: {removedParticipant != rehydratedParticipant}");

            return rehydratedCall;
        }

        /// <summary>
        /// Makes outgoing call asynchronously.
        /// </summary>
        /// <param name="makeCallBody">The outgoing call request body.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<ICall> MakeCallAsync(MakeCallRequestData makeCallBody, Guid scenarioId)
        {
            if (makeCallBody == null)
            {
                throw new ArgumentNullException(nameof(makeCallBody));
            }

            if (makeCallBody.TenantId == null)
            {
                throw new ArgumentNullException(nameof(makeCallBody.TenantId));
            }

            if (makeCallBody.ObjectId == null)
            {
                throw new ArgumentNullException(nameof(makeCallBody.ObjectId));
            }

            var target =
                makeCallBody.IsApplication ?
                new InvitationParticipantInfo
                {
                    Identity = new IdentitySet
                    {
                        Application = new Identity
                        {
                            Id = makeCallBody.ObjectId,
                            DisplayName = $"Responder {makeCallBody.ObjectId}",
                        },
                    },
                }
                :
                new InvitationParticipantInfo
                {
                    Identity = new IdentitySet
                    {
                        User = new Identity
                        {
                            Id = makeCallBody.ObjectId,
                        },
                    },
                };

            var mediaToPrefetch = new List<MediaInfo>();
            foreach (var m in this.MediaMap)
            {
                mediaToPrefetch.Add(m.Value.MediaInfo);
            }

            var call = new Call
            {
                Targets = new[] { target },
                MediaConfig = new ServiceHostedMediaConfig { PreFetchMedia = mediaToPrefetch },
                RequestedModalities = new List<Modality> { Modality.Audio },
                TenantId = makeCallBody.TenantId,
            };

            var statefulCall = await this.Client.Calls().AddAsync(call, scenarioId: scenarioId).ConfigureAwait(false);

            _graphLogger.Info($"Call creation complete: {statefulCall.Id}");

            return statefulCall;
        }

        /// <summary>
        /// Adds participants asynchronously.
        /// </summary>
        /// <param name="callLegId">which call to add participants.</param>
        /// <param name="addParticipantBody">The add participant body.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task AddParticipantAsync(string callLegId, AddParticipantRequestData addParticipantBody)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            if (string.IsNullOrEmpty(addParticipantBody.ObjectId))
            {
                throw new ArgumentNullException(nameof(addParticipantBody.ObjectId));
            }

            var target = new IdentitySet
            {
                User = new Identity
                {
                    Id = addParticipantBody.ObjectId,
                },
            };

            await this.Client.Calls()[callLegId].Participants
                .InviteAsync(target, addParticipantBody.ReplacesCallId)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the local media session.
        /// </summary>
        /// <param name="mediaSessionId">The media session identifier.
        /// This should be a unique value for each call.</param>
        /// <returns>The <see cref="ILocalMediaSession" />.</returns>
        private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default)
        {
            try
            {
                // create media session object, this is needed to establish call connections
                return this.Client.CreateMediaSession(
                    new AudioSocketSettings
                    {
                        StreamDirections = StreamDirection.Sendrecv,
                        // Note! Currently, the only audio format supported when receiving unmixed audio is Pcm16K
                        SupportedAudioFormat = AudioFormat.Pcm16K,
                        ReceiveUnmixedMeetingAudio = false //get the extra buffers for the speakers
                    },
                    new VideoSocketSettings
                    {
                        StreamDirections = StreamDirection.Inactive
                    },
                    mediaSessionId: mediaSessionId);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Incoming call handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{TResource}" /> instance containing the event data.</param>
        private void CallsOnIncoming(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            args.AddedResources.ForEach(call =>
            {
                // Get the policy recording parameters.

                // The context associated with the incoming call.
                IncomingContext incomingContext =
                    call.Resource.IncomingContext;

                // The RP participant.
                string observedParticipantId =
                    incomingContext.ObservedParticipantId;

                // If the observed participant is a delegate.
                IdentitySet onBehalfOfIdentity =
                    incomingContext.OnBehalfOf;

                // If a transfer occured, the transferor.
                IdentitySet transferorIdentity =
                    incomingContext.Transferor;

                string countryCode = null;
                EndpointType? endpointType = null;

                // Note: this should always be true for CR calls.
                if (incomingContext.ObservedParticipantId == incomingContext.SourceParticipantId)
                {
                    // The dynamic location of the RP.
                    countryCode = call.Resource.Source.CountryCode;

                    // The type of endpoint being used.
                    endpointType = call.Resource.Source.EndpointType;
                }

                IMediaSession mediaSession = Guid.TryParse(call.Id, out Guid callId)
                    ? this.CreateLocalMediaSession(callId)
                    : this.CreateLocalMediaSession();

                // Answer call
                call?.AnswerAsync(mediaSession).ForgetAndLogExceptionAsync(
                    call.GraphLogger,
                    $"Answering call {call.Id} with scenario {call.ScenarioId}.");
            });
        }

        /// <summary>
        /// Updated call handler & language settings.
        /// </summary>
        /// <param name="sender">The <see cref="ICallCollection" /> sender.</param>
        /// <param name="args">The <see cref="CollectionEventArgs{ICall}" /> instance containing the event data.</param>
        private void CallsOnUpdated(ICallCollection sender, CollectionEventArgs<ICall> args)
        {
            foreach (var call in args.AddedResources)
            {
                var callHandler = new CallHandler(call, this);
                this.CallHandlers[call.Id] = callHandler;
            }

            foreach (var call in args.RemovedResources)
            {
                if (this.CallHandlers.TryRemove(call.Id, out CallHandler handler))
                {
                    handler.Dispose();
                }
            }
        }

        /// <summary>
        /// The get handler or throw.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns>The <see cref="CallHandler" />.</returns>
        /// <exception cref="ArgumentException">call ({callLegId}) not found</exception>
        private CallHandler GetHandlerOrThrow(string callLegId)
        {
            if (!this.CallHandlers.TryGetValue(callLegId, out CallHandler handler))
            {
                throw new ArgumentException($"call ({callLegId}) not found");
            }

            return handler;
        }
    }
}
