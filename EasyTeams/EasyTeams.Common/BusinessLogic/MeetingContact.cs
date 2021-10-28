using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EasyTeams.Common.BusinessLogic
{
    /// <summary>
    /// Sombody who'll be joining a Teams meeting.
    /// </summary>
    public class MeetingContact
    {
        /// <summary>
        /// Deserialisation constructor only
        /// </summary>
        [JsonConstructor]
        public MeetingContact() { }
        /// <summary>
        /// New internal contact. Throws ArgumentOutOfRangeException if email address is invalid
        /// </summary>
        public MeetingContact(string emailAddress) : this(emailAddress, false)
        {
        }

        /// <summary>
        /// Throws ArgumentOutOfRangeException if email address is invalid
        /// </summary>
        public MeetingContact(string emailAddress, bool externalContact)
        {
            this.IsExternal = externalContact;
            if (IsValidEmailAddress(emailAddress))
            {
                this.Email = emailAddress;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(emailAddress), $"Not a valid email address: '{emailAddress}'");
            }
        }

        public string Email { get; set; }

        /// <summary>
        /// Is this user external to the organisation?
        /// </summary>
        public bool IsExternal { get; set; }

        public static bool IsValidEmailAddress(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        internal async Task<MeetingParticipantInfo> ToMeetingParticipantInfo(TeamsObjectCache teamsCache)
        {
            try
            {
                var user = await teamsCache.GetUser(this.Email);
                return new MeetingParticipantInfo()
                {
                    Identity = new IdentitySet() { User = new Identity() { Id = user.Id } },
                    Upn = user.UserPrincipalName
                };
            }
            catch (ServiceException ex)
            {
                // Is this error because the user doesn't exist in the directory?
                if (ex.Error.Code == EasyTeamsConstants.GRAPH_ERROR_RESOURCE_NOT_FOUND)
                {
                    return new MeetingParticipantInfo() { Upn = this.Email };
                }
                else
                {
                    throw;
                }
            }

        }
    }
}
