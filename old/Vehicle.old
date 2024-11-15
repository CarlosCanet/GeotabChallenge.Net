using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupApplication
{
    public class Vehicle
    {
        public string VehicleId { get; }
        public string Name { get; }
        public string Vin { get; }
        private string FileName { get; }

        public Vehicle(string vehicleId, string name, string vin)
        {
            VehicleId = vehicleId;
            Name = name;
            Vin = vin;
            FileName = Path.Combine(Config.BACKUP_FOLDER, $"{vehicleId}.csv");
            CreateFileAsync().Wait();
        }

        private async Task CreateFileAsync()
        {
            if (!File.Exists(FileName))
            {
                await File.WriteAllTextAsync(FileName, "timestamp,id,vin,lat,lon,speed,odometer\n");
            }
        }

        public async Task BackupDataAsync(IEnumerable<dynamic> data)
        {
            var lines = data.Select(item =>
                $"{item.Timestamp:O},{item.Id},{Vin},{item.Latitude},{item.Longitude},{item.Speed},{item.Odometer}");
            await File.AppendAllLinesAsync(FileName, lines);
        }
    }
}
