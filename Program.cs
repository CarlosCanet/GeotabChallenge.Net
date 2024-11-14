using System.Threading.Tasks;

namespace BackupApplication
{
    class Program
    {
        static async Task Main(string[] args)
        {
            BackupManager backupManager = new BackupManager();
            await backupManager.InitializeAsync();
        }
    }
}
