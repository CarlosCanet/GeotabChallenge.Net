using System.Threading.Tasks;

namespace GeotabChallengeCC
{
    /// <summary>
    /// Worker for a database
    /// </summary>
    class DatabaseWorker : Worker
    {
        readonly FeedParameters feedParameters;
        readonly FeedProcessor feedService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseWorker" /> class.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="database">The database.</param>
        /// <param name="server">The server.</param>
        /// <param name="gpsToken">The GPS token.</param>
        /// <param name="statusToken">The status token.</param>
        /// <param name="path">The path.</param>
        public DatabaseWorker(string user, string password, string database, string server, long? gpsToken, long? statusToken, string path)
            : base(path)
        {
            feedParameters = new FeedParameters(gpsToken, statusToken);
            feedService = new FeedProcessor(server, database, user, password);
            

        }

        /// <summary>
        /// The work action.
        /// </summary>
        /// <inheritdoc />
        public async override Task WorkActionAsync()
        {
            await DisplayFeedResultsAsync(await feedService.GetAsync(feedParameters));
            
        }
    }
}