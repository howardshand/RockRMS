﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Quartz;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;

namespace Rock.Jobs
{

    /// <summary>
    /// Sends RSVP Reminder messages.
    /// </summary>
    /// <seealso cref="Quartz.IJob" />
    #region DataMap Field Attributes
    [GroupTypeField( "Group Type",
        Key = AttributeKey.GroupType,
        Description = "The Group Type to send RSVP reminders for.",
        IsRequired = true,
        Order = 0 )]
    #endregion

    [DisallowConcurrentExecution]
    public class SendRsvpReminders : IJob
    {
        /// <summary>
        /// Keys for DataMap Field Attributes.
        /// </summary>
        private static class AttributeKey
        {
            public const string GroupType = "GroupType";
        }

        /// <summary> 
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public SendRsvpReminders()
        {
        }

        /// <summary>
        /// Job that will send RSVP Reminder messages. Called by the <see cref="IScheduler" /> when an <see cref="ITrigger" />
        /// fires that is associated with the <see cref="IJob" />.
        /// </summary>
        /// <param name="context">The job's execution context.</param>
        public virtual void Execute( IJobExecutionContext context )
        {
            try
            {
                ProcessJob( context );
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, HttpContext.Current );
                throw;
            }
        }

        /// <summary>
        /// Private method called by Execute() to process the job.  This method should be wrapped in a try/catch block to ensure than any exceptions are sent to the
        /// <see cref="ExceptionLogService"/>.
        /// </summary>
        /// <param name="context">The job's execution context.</param>
        private void ProcessJob( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            RockContext rockContext = new RockContext();

            // Make sure GroupType job attribute was assigned.
            Guid? groupTypeGuid = dataMap.GetString( AttributeKey.GroupType ).AsGuidOrNull();
            if ( groupTypeGuid == null )
            {
                context.Result = "Job Exited:  No Group Type has been set.";
                return;
            }

            // Verify GroupType exists.
            var groupType = new GroupTypeService( rockContext ).Get( groupTypeGuid.Value );
            if ( groupType == null )
            {
                context.Result = "Job Exited:  The selected Group Type does not exist.";
                return;
            }

            // Verify GroupType has RSVP enabled.
            if ( !groupType.EnableRSVP )
            {
                context.Result = "Job Exited:  The selected Group Type does not have RSVP enabled.";
                return;
            }

            // Retrieve RSVP settings from GroupType.
            var systemCommunicationService = new SystemCommunicationService( rockContext );
            SystemCommunication groupTypeReminder = GetGroupTypeRsvpReminder( groupType, systemCommunicationService );
            int? groupTypeOffset = GetGroupTypeOffset( groupType );

            // Get RSVP enabled groups which have an RSVP Reminder set, and verify that there is at least one group to process.
            var groups = GetRsvpReminderEligibleGroups( groupType, rockContext, groupType.RSVPReminderSystemCommunicationId.HasValue );
            if ( !groups.Any() )
            {
                context.Result = "Job Exited:  The selected Group Type does not contain any groups with RSVP reminder communications.";
                return;
            }

            // Process groups and get the response.  This is where the actual work starts.
            var sbResults = ProcessGroups( groups, groupTypeReminder, groupTypeOffset, rockContext );

            // Job complete!  Record results of the job to the context.
            var jobResult = sbResults.ToString();
            if ( string.IsNullOrEmpty( jobResult ) )
            {
                jobResult = "Job completed successfully, but no reminders were sent.";
            }
            context.Result = jobResult;
        }

        #region Job Methods

        /// <summary>
        /// Processes a list of groups, sending any RSVP reminders for those groups and returning a <see cref="StringBuilder"/> with the job result messages.
        /// </summary>
        /// <param name="groups">The list of <see cref="Group"/> objects to process.</param>
        /// <param name="groupTypeReminder">The <see cref="SystemCommunication"/> from the <see cref="GroupType"/>.</param>
        /// <param name="groupTypeOffset">The RSVPReminderOffsetDays property of the <see cref="GroupType"/>.</param>
        /// <param name="rockContext">The <see cref="RockContext"/>.</param>
        /// <returns>A <see cref="StringBuilder"/> with the job result messages.</returns>
        private StringBuilder ProcessGroups( List<Group> groups, SystemCommunication groupTypeReminder, int? groupTypeOffset, RockContext rockContext )
        {
            var sbResults = new StringBuilder();

            foreach ( var group in groups )
            {
                // Select the appropriate RSVP settings (from GroupType or Group) and process RSVP reminders for the group.
                SystemCommunication groupReminder = groupTypeReminder ?? group.RSVPReminderSystemCommunication;
                int rsvpOffset = ( groupTypeOffset ?? group.RSVPReminderOffsetDays ) ?? 0;
                int remindersSent = SendRsvpRemindersForGroup( group, rsvpOffset, groupReminder, rockContext );

                // If a reminder was sent, add a message about it so we can display it in the job status message.
                if ( remindersSent > 0 )
                {
                    sbResults.AppendLine( string.Format( "Sent {0} reminder(s) for group {1}.", remindersSent, group.Name ) );
                }
            }

            return sbResults;
        }

        /// <summary>
        /// Gets the <see cref="SystemCommunication"/> for a <see cref="GroupType"/>.
        /// </summary>
        /// <param name="groupType">The <see cref="GroupType"/>.</param>
        /// <param name="systemCommunicationService">The <see cref="SystemCommunicationService"/>.</param>
        /// <returns>A <see cref="SystemCommunication"/> if one is set on the <see cref="GroupType"/>, otherwise null.</returns>
        private SystemCommunication GetGroupTypeRsvpReminder( GroupType groupType, SystemCommunicationService systemCommunicationService )
        {
            SystemCommunication groupTypeReminder = null;
            if ( groupType.RSVPReminderSystemCommunicationId.HasValue )
            {
                groupTypeReminder = systemCommunicationService.Get( groupType.RSVPReminderSystemCommunicationId.Value );
            }
            return groupTypeReminder;
        }

        /// <summary>
        /// Gets the Offset Days for a <see cref="GroupType"/>.
        /// </summary>
        /// <param name="groupType">The <see cref="GroupType"/>.</param>
        /// <returns>An integer value if one is set on the <see cref="GroupType"/>, otherwise null.</returns>
        private int? GetGroupTypeOffset( GroupType groupType )
        {
            int? groupTypeOffset = null;
            if ( groupType.RSVPReminderOffsetDays.HasValue && groupType.RSVPReminderOffsetDays.Value != 0 )
            {
                groupTypeOffset = groupType.RSVPReminderOffsetDays.Value;
            }
            return groupTypeOffset;
        }

        /// <summary>
        /// Gets a list of RSVP-reminder eligible groups based on the <see cref="GroupType"/>.  Eager loads the RSVPReminderSystemCommunication.
        /// </summary>
        /// <param name="groupType">The <see cref="GroupType"/>.</param>
        /// <param name="rockContext">The <see cref="RockContext"/>.</param>
        /// <param name="groupTypeHasReminder">Should be true if the GroupType.RSVPReminderSystemCommunicationId has a value.</param>
        /// <returns>A list of RSVP-reminder eligible groups.</returns>
        private List<Group> GetRsvpReminderEligibleGroups( GroupType groupType, RockContext rockContext, bool groupTypeHasReminder )
        {
            var groupQuery = new GroupService( rockContext )
                .Queryable( "RSVPReminderSystemCommunication" ).AsNoTracking()
                .Where( g => g.GroupTypeId == groupType.Id );

            // If the GroupType doesn't have a reminder communication set, then the Group must have it's own reminder (or else there's no SystemCommunication to send).
            if ( !groupTypeHasReminder )
            {
                groupQuery = groupQuery.Where( g => g.RSVPReminderSystemCommunicationId.HasValue );
            }

            return groupQuery.ToList();
        }

        /// <summary>
        /// Processes a group for any RSVP communications that need to be issued.
        /// </summary>
        /// <param name="group">The <see cref="Group"/>.</param>
        /// <param name="offsetDays">The number of days prior to the RSVP occurrence that a reminder should be sent.</param>
        /// <param name="reminder">The <see cref="SystemCommunication"/> to be sent as a reminder.</param>
        /// <param name="rockContext">The <see cref="RockContext"/>.</param>
        /// <returns>The number of reminders sent.</returns>
        private int SendRsvpRemindersForGroup( Group group, int offsetDays, SystemCommunication reminder, RockContext rockContext )
        {
            int remindersSent = 0;

            // Get occurrences (within the correct date range) with positive RSVP responses for this group.
            var rsvpOccurrences = GetOccurrencesForGroup( group, offsetDays, rockContext );

            foreach ( var rsvpOccurrence in rsvpOccurrences )
            {
                // Process each positive RSVP response and send their reminder.
                var rsvpResponders = rsvpOccurrence.Attendees.Where( a => a.RSVP == RSVP.Yes ).ToList();
                foreach ( var rsvpResponder in rsvpResponders )
                {
                    remindersSent += SendReminder( group, rsvpOccurrence, rsvpResponder.PersonAlias.Person, reminder );
                }
            }

            return remindersSent;
        }

        /// <summary>
        /// Gets a list of future occurrences within the date range of the specified offset with RSVP responses for a <see cref="Group"/>.  Eager loads Attendees and
        /// attached Person records.
        /// </summary>
        /// <param name="group">The <see cref="Group"/>.</param>
        /// <param name="offsetDays">The number of days prior to the RSVP occurrence that a reminder should be sent.</param>
        /// <param name="rockContext">The <see cref="RockContext"/>.</param>
        /// <returns>A list of <see cref="AttendanceOccurrence"/> objects.</returns>
        private List<AttendanceOccurrence> GetOccurrencesForGroup( Group group, int offsetDays, RockContext rockContext )
        {
            // Calculate correct date range from the RSVP Offset.
            DateTime startDate = DateTime.Today.AddDays( offsetDays );
            DateTime endDate = startDate.AddDays( 1 ).AddMilliseconds( -1 );

            var rsvpOccurrences = new AttendanceOccurrenceService( rockContext )
                    .Queryable( "Attendees,Attendees.PersonAlias.Person" ).AsNoTracking()
                    .Where( o => o.OccurrenceDate >= startDate ) // OccurrenceDate must be greater than startDate (the beginning of the day from the offset).
                    .Where( o => o.OccurrenceDate <= endDate ) // OccurrenceDate must be less than endDate (the end of the day from the offset).
                    .Where( o => o.Attendees.Where( a => a.RSVP == RSVP.Yes ).Any() ) // Occurrence must have attendees who responded "Yes".
                    .ToList();

            return rsvpOccurrences;
        }

        /// <summary>
        /// Sends an RSVP reminder <see cref="SystemCommunication"/> to an individual attendee.
        /// </summary>
        /// <param name="group">The <see cref="Group"/>.</param>
        /// <param name="occurrence">The <see cref="AttendanceOccurrence"/>.</param>
        /// <param name="person">The <see cref="Person"/>.</param>
        /// <param name="reminder">The <see cref="SystemCommunication"/> to be sent as a reminder.</param>
        private int SendReminder( Group group, AttendanceOccurrence occurrence, Person person, SystemCommunication reminder )
        {
            // Build Lava merge fields.
            Dictionary<string, object> lavaMergeFields = new Dictionary<string, object>();
            lavaMergeFields.Add( "Person", person );
            lavaMergeFields.Add( "Group", group );
            lavaMergeFields.Add( "Occurrence", occurrence );

            // Send the message.
            var recipient = new RockEmailMessageRecipient( person, lavaMergeFields );
            var message = new RockEmailMessage( reminder );
            message.SetRecipients( new List<RockEmailMessageRecipient>() { recipient } );
            message.Send( out List<string> emailErrors );

            if ( !emailErrors.Any() )
            {
                return 1; // No error, this should be counted as a sent reminder.
            }

            return 0;
        }

        #endregion Job Methods

    }
}