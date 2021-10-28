
using EasyTeamsBot.Common;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyTeams.Common.BusinessLogic
{
    /// <summary>
    /// A request for a new meeting
    /// </summary>
    public class NewConferenceCallRequest
    {
        public NewConferenceCallRequest()
        {
            this.Recipients = new List<MeetingContact>();
        }

        #region Props

        public DateTime? Start { get; set; }

        [JsonIgnore()]
        public DateTime? End 
        { 
            get 
            {
                if (Start.HasValue && MinutesLong.HasValue)
                {
                    return Start.Value.AddMinutes(MinutesLong.Value);
                }
                return null;
            } 
        }
        public int? MinutesLong { get; set; }
        public MeetingContact OnBehalfOf { get; set; }
        public string Subject { get; set; }
        public List<MeetingContact> Recipients { get; set; }

        public string TimeZoneName { get; set; }

        #endregion

        /// <summary>
        /// Convert to new Graph OnlineMeeting object
        /// </summary>
        internal async Task<OnlineMeeting> ToNewConfCall(TeamsManager teamsManager)
        {
            if (!this.IsValid())
            {
                string errs = string.Empty;
                foreach (var err in this.GetErrors())
                {
                    errs += $"{err}, ";
                }
                errs = errs.TrimEnd(", ".ToCharArray());
                throw new InvalidOperationException("Invalid request data. Errors: " + errs);
            }
            var meetingParticipants = new MeetingParticipants();
            var participants = new List<MeetingParticipantInfo>();


            // Build attendees list
            foreach (var recipient in Recipients)
            {
                participants.Add(await recipient.ToMeetingParticipantInfo(teamsManager.Cache));
            }
            meetingParticipants.Attendees = participants;
            meetingParticipants.Organizer = await this.OnBehalfOf.ToMeetingParticipantInfo(teamsManager.Cache);
            return new OnlineMeeting()
            {
                StartDateTime = Start.Value,
                EndDateTime = Start.Value.AddMinutes(MinutesLong.Value),
                Subject = Subject,
                Participants = meetingParticipants
            };
        }

        public bool IsValid()
        {
            return GetErrors().Count == 0;
        }

        public List<string> GetErrors()
        {
            List<string> errs = new List<string>();

            if (!this.MinutesLong.HasValue | this.MinutesLong <= 0)
            {
                errs.Add("Duration is not set or <= 0 minutes");
            }
            if (this.OnBehalfOf == null)
            {
                errs.Add($"No value for {nameof(OnBehalfOf)}");
            }
            if (this.Recipients == null || this.Recipients.Count == 0)
            {
                errs.Add($"No value for {nameof(Recipients)}");
            }
            if (string.IsNullOrEmpty(this.Subject))
            {
                errs.Add($"No value for {nameof(Subject)}");
            }
            if (string.IsNullOrEmpty(this.TimeZoneName))
            {
                errs.Add($"No value for {nameof(TimeZoneName)}");
            }
            if (!this.Start.HasValue)
            {
                errs.Add($"No value for {nameof(Start)}");
            }

            return errs;
        }
    }
}
