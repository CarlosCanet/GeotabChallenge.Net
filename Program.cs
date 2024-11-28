using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GeotabChallengeCC
{
    /// <summary>
    /// Main program.
    /// </summary>
    static class Program
    {
        public static void Main(string[] args)
        {
            const string Command = "> dotnet run --s {0} --d {1} --u {2} --p {3} --f {4} --c";
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
                                Backup backup = new Backup(user, password, database, server, path);

                                var cancellationToken = new CancellationTokenSource();
                                Task[] tasks = new Task[1];
                                tasks[0] = Task.Run(async () => await backup.work(continuous));
                                Task.WaitAll(tasks);

                                if (continuous && Console.ReadLine() != null)
                                {
                                    // This task should run async
                                    Func<Task> function = async () => await backup.work(continuous);
                                    Task task = Task.Run(function, cancellationToken.Token);
                                }

                                if (continuous)
                                {
                                    backup.RequestStop();
                                    cancellationToken.Cancel();
                                }

                                Console.WriteLine();
                                Console.WriteLine("******************************************************");
                                Console.WriteLine($"Finished receiving data from {server}/{database}");
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
            Console.WriteLine(Command, "server", "database", "user", "password", "file path");
            Console.WriteLine("--s  The Server");
            Console.WriteLine("--d  The Database");
            Console.WriteLine("--u  The User");
            Console.WriteLine("--p  The Password");
            Console.WriteLine("--f  The folder to save any output files to, if applicable. Defaults to the current directory.");
            Console.WriteLine("--c  Run the feed continuously.");
            Console.ReadLine();
        }
    }
}