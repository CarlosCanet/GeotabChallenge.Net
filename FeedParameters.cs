using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;

namespace GeotabChallengeCC
{
    /// <summary>
    /// Contains latest data tokens and collections to populate during <see cref="FeedProcessor.GetAsync"/> call.
    /// </summary>
    class FeedParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeedParameters"/> class.
        /// </summary>
        /// <param name="lastGpsDataToken">The latest <see cref="LogRecord" /> token</param>
        /// <param name="lastStatusDataToken">The latest <see cref="StatusData" /> token</param>
        public FeedParameters(long? lastGpsDataToken, long? lastStatusDataToken)
        {
            LastGpsDataToken = lastGpsDataToken;
            LastStatusDataToken = lastStatusDataToken;
        }

        /// <summary>
        /// Gets or sets the latest <see cref="LogRecord" /> token.
        /// </summary>
        /// <value>
        /// The last GPS data token.
        /// </value>
        public long? LastGpsDataToken { get; set; }

        /// <summary>
        /// Gets or sets the latest <see cref="StatusData" /> token.
        /// </summary>
        /// <value>
        /// The last status data token.
        /// </value>
        public long? LastStatusDataToken { get; set; }
    }
}