using System.Collections.Generic;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;

namespace GeotabChallengeCC
{
    /// <summary>
    /// The result of a Feed call.
    /// </summary>
    class FeedResultData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeedResultData"/> class.
        /// </summary>
        public FeedResultData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedResultData"/> class.
        /// </summary>
        /// <param name="gpsRecords">The <see cref="LogRecord" />s returned by the server.</param>
        /// <param name="statusData">The <see cref="StatusData" /> returned by the server.</param>
        public FeedResultData(IList<LogRecord> gpsRecords, IList<StatusData> statusData)
        {
            GpsRecords = gpsRecords;
            StatusData = statusData;
        }

        /// <summary>
        /// Gets or sets the <see cref="LogRecord"/> collection.
        /// </summary>
        public IList<LogRecord> GpsRecords { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="StatusData" /> collection.
        /// </summary>
        /// <value>
        /// The status data.
        /// </value>
        public IList<StatusData> StatusData { get; set; }
    }
}