using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Geotab.Checkmate.ObjectModel.Engine;

/***************************************************************
 * DISCLAIMER: This code example is provided for demonstration *
 * purposes only. Depending on the frequency at which it is   *
 * executed, it may be subject to rate limits imposed by APIs *
 * or other services it interacts with. It is recommended to   *
 * review and adjust the code as necessary to handle rate      *
 * limits or any other constraints relevant to your use case.  *
 ***************************************************************/


namespace GeotabChallengeCC
{
    /// <summary>
    /// Main program
    /// </summary>
    static class Program
    {
        readonly static int backupInterval = 20;   // In seconds
        /// <summary>
        /// This is a console example of obtaining the data feed from the server.
        /// 1) Process command line arguments: Server, Database, User, Password, Options, File Path and Continuous Feed option.
        /// 2) Collect data via download to csv.
        /// 3) Disable comments to enable feed to console and/or feed to BigQuery options and rerun.
        /// A complete Geotab API object and method reference is available at the Geotab Developer page.
        /// </summary>
        /// <param name="args">The command line arguments for the application. Note: When debugging these can be added by: Right click the project &gt; Properties &gt; Debug Tab &gt; Start Options: Command line arguments.</param>
        static void Main(string[] args)
        {
            const string Command = "> dotnet run --s {0} --d {1} --u {2} --p {3} --f {9} --c";
            if (args.Length > 0)
            {
                IList<string> arguments = new List<string>();
                foreach (string s in args)
                {
                    arguments.Add(s.ToLowerInvariant());
                }
                int index = arguments.IndexOf("--u");
                if (index >= 0 && index < args.Length - 1)
                {
                    string user = args[index + 1];
                    index = arguments.IndexOf("--p");
                    if (index >= 0 && index < args.Length - 1)
                    {
                        string password = args[index + 1];
                        index = arguments.IndexOf("--s");
                        if (index >= 0 && index < args.Length - 1)
                        {
                            string server = args[index + 1];
                            index = arguments.IndexOf("--d");
                            if (index >= 0 && index < args.Length - 1)
                            {
                                string database = index >= 0 && index < args.Length - 1 ? args[index + 1] : null;
                                index = arguments.IndexOf("--f");
                                string path = index >= 0 && index < args.Length - 1 ? args[index + 1] : Environment.CurrentDirectory;
                                bool continuous = arguments.IndexOf("--c") >= 0;
                                bool federation = string.IsNullOrEmpty(database);
                                Worker worker = new DatabaseWorker(user, password, database, server, path, backupInterval);
                                var cancellationToken = new CancellationTokenSource();
                                Task[] tasks = new Task[1];
                                // tasks[0] = Task.Run(async () => await worker.DoWorkAsync(continuous));
                                tasks[0] = Task.Run(async () => await worker.DoWorkAsync(continuous));

                                Task.WaitAll(tasks);

                                if (continuous && Console.ReadLine() != null)
                                {
                                    // This task should run async
                                    // Func<Task> function = async () => await worker.DoWorkAsync(continuous);
                                    Func<Task> function = async () => await worker.DoWorkAsync(continuous);
                                    Task task = Task.Run(function, cancellationToken.Token);
                                }

                                if (continuous)

                                {
                                    worker.RequestStop();
                                    cancellationToken.Cancel();
                                }

                                Console.WriteLine();
                                Console.WriteLine("******************************************************");
                                Console.WriteLine("Finished receiving data from " + server + (federation ? "" : "/" + database));
                                Console.WriteLine("******************************************************");
                                Console.WriteLine("Press ENTER to quit.");
                                Console.ReadLine();
                                return;
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Usage:\n");
            Console.WriteLine(Command, "server", "database", "user", "password", "nnn", "nnn", "file path");
            Console.WriteLine("--s  The Server");
            Console.WriteLine("--d  The Database");
            Console.WriteLine("--u  The User");
            Console.WriteLine("--p  The Password");
            Console.WriteLine("--gt The last known gps data Version");
            Console.WriteLine("--st The last known status data Version");
            Console.WriteLine("--f  The folder to save any output files to, if applicable. Defaults to the current directory.");
            Console.WriteLine("--c  Run the feed continuously.");
            Console.ReadLine();
        }
    }
}