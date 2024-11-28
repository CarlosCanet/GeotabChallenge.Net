using System.Threading.Tasks;
using System;
using Thread = System.Threading.Thread;

namespace GeotabChallengeCC
{
    /// <summary>
    /// Worker base class
    /// </summary>
    abstract class Worker
    {
        readonly string path;
        int backupInterval;
        bool stop;

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        internal Worker(string path)
        {

            this.path = path;

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        internal Worker(string path, int backupInterval)
        {

            this.path = path;
            this.backupInterval = backupInterval;

        }

        /// <summary>
        /// Displays the feed results.
        /// </summary>
        /// <param name="results">The results.</param>
        public async Task DisplayFeedResultsAsync(FeedResultData results)
        {

            // Optionally we can output to csv or google doc:
            new FeedToCsv(path, results.GpsRecords, results.StatusData).Run();
            Console.WriteLine("Backup done. " + (results.GpsRecords.Count + results.StatusData.Count) + " events processed (" + results.GpsRecords.Count + " GPS events and " + results.StatusData.Count + " status events).");
            // Displays feed to console
            // new FeedToConsole(results.GpsRecords,results.StatusData).Run();

            await Task.Delay(backupInterval*1000);
        }

        /// <summary>
        /// Do the work.
        /// </summary>
        /// <param name="obj">The object.</param>
        public async Task DoWorkAsync(bool continuous)
        {
            do
            {
                await WorkActionAsync();
            }
            while (continuous && !stop);
        }

        /// <summary>
        /// Requests to stop.
        /// </summary>
        public void RequestStop()
        {
            stop = true;
        }

        /// <summary>
        /// The work action.
        /// </summary>
        public abstract Task WorkActionAsync();
    }
}