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
using Newtonsoft.Json;

namespace Rock.Checkr.CheckrApi
{
    /// <summary>
    /// Report Webhook
    /// </summary>
    /// <seealso cref="Rock.Checkr.CheckrApi.GenericWebhook" />
    internal class ReportWebhook : GenericWebhook
    {
        /// <summary>
        /// Gets or sets the Data.
        /// </summary>
        /// <value>
        /// The Data.
        /// </value>
        [JsonProperty( "data" )]
        public ReportData Data { get; set; }
    }

    /// <summary>
    /// Report.data JSON structure.
    /// </summary>
    internal class ReportData
    {
        /// <summary>
        /// Gets or sets the data object.
        /// </summary>
        /// <value>
        /// The Data Object.
        /// </value>
        [JsonProperty( "object" )]
        public ReportDataObject Object { get; set; }
    }

    /// <summary>
    /// Report.data.object JSON structure.
    /// </summary>
    internal class ReportDataObject
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>
        /// The ID.
        /// </value>
        [JsonProperty( "id" )]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        [JsonProperty( "status" )]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the package.
        /// </summary>
        /// <value>
        /// The package.
        /// </value>
        [JsonProperty( "package" )]
        public string Package { get; set; }

        /// <summary>
        /// Gets or sets the candidate ID.
        /// </summary>
        /// <value>
        /// The candidate ID.
        /// </value>
        [JsonProperty( "candidate_id" )]
        public string CandidateId { get; set; }
    }
}
