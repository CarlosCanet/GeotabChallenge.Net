using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;

namespace GeotabChallengeCC
{
    /// <summary>
    /// Represents a backup service for data.
    /// Periodically retrieves data from Geotab and writes it to CSV files.
    /// </summary>
    class Backup
    {
        bool stop = false;
        readonly API api;
        readonly string path;
        IDictionary<Id, Device> devices = new Dictionary<Id, Device>();
        readonly IDictionary<Id, SortedList<string, object>> vehicleRecords = new Dictionary<Id, SortedList<string, object>>();
        public const string BackupDataHeader = "sTimestamp,sEventId,sVehicleName,sVehicleSerialNumber,sVin,dLatitud,dLongitud,iSpeed,iOdometer";
        const int HoursToBackup = 12; // In hours
        const int BackupInterval = 20; // In seconds
        DateTime lastDate = DateTime.UtcNow.AddHours(-HoursToBackup);
        int gpsRecordsWritten = 0, statusRecordsWritten = 0;

        public Backup(string user, string password, string database, string server, string path)
            : this(new API(user, password, null, database, server))
        {
            this.path = path;
        }

        public Backup(API api)
        {
            this.api = api;
        }

        /// <summary>
        /// Updates the local device cache with the devices from Geotab.
        /// </summary>
        public async Task updateDevices()
        {
            IList<Device> returnedDevices = await api.CallAsync<IList<Device>>("Get", typeof(Device));
            foreach (Device device in returnedDevices)
            {
                if (!devices.ContainsKey(device.Id))
                {
                    devices.Add(device.Id, device);
                }
            }
        }

        /// <summary>
        /// Retrieves the devices info if not stored and updates the device cache.
        /// After that it retrieves the GPS and odometer records from Geotab, merge them and sort them, storing the backup info.
        /// </summary>
        public async Task getDataAndBackup()
        {
            try
            {
                if (devices.Count == 0)
                {
                    await updateDevices();
                }
                LogRecordSearch logRecordSearch = new()
                {
                    FromDate = lastDate
                };
                StatusDataSearch statusDataSearch = new()
                {
                    DiagnosticSearch = new DiagnosticSearch(KnownId.DiagnosticOdometerId),
                    FromDate = lastDate
                };

                IList<LogRecord> logRecordData = await api.CallAsync<IList<LogRecord>>("Get", typeof(LogRecord), new { search = logRecordSearch });
                IList<StatusData> statusData = await api.CallAsync<IList<StatusData>>("Get", typeof(StatusData), new { search = statusDataSearch });
                lastDate = DateTime.UtcNow;

                foreach (var record in logRecordData)
                {
                    Id vehicleId = record.Device.Id;
                    if (vehicleId != null)
                    {
                        record.Device = devices[vehicleId];
                        if (!vehicleRecords.ContainsKey(vehicleId))
                        {
                            vehicleRecords[vehicleId] = new SortedList<string, object>();
                        }
                        vehicleRecords[vehicleId].Add(record.DateTime.ToString() + record.Id, record);
                    }
                }
                foreach (var record in statusData)
                {
                    Id vehicleId = record.Device.Id;
                    if (vehicleId != null)
                    {
                        record.Device = devices[vehicleId];
                        if (!vehicleRecords.ContainsKey(vehicleId))
                        {
                            vehicleRecords[vehicleId] = new SortedList<string, object>();
                        }
                        vehicleRecords[vehicleId].Add(record.DateTime.ToString() + record.Id, record);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (e is HttpRequestException)
                {
                    await Task.Delay(5000);
                }
                if (e is DbUnavailableException)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            }
            WriteDataToCsv();
            await Task.Delay(BackupInterval * 1000);
        }

        /// <summary>
        /// Writes the collected data to CSV files.
        /// </summary>
        public void WriteDataToCsv()
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (vehicleRecords.Count > 0)
            {
                gpsRecordsWritten = 0;
                statusRecordsWritten = 0;
                foreach (var vehicleRecord in vehicleRecords)
                {
                    SortedList<string, object> records = vehicleRecord.Value;
                    string filePath = Path.Combine(path, vehicleRecord.Key.ToString() + ".csv");
                    bool fileExist = File.Exists(filePath);
                    DateTime lastDateVehicle = DateTime.MinValue;
                    using (TextWriter writer = new StreamWriter(filePath, true))
                    {
                        if (!fileExist)
                        {
                            writer.WriteLine(BackupDataHeader);
                        }
                        else
                        {
                            string lastLine = ReadLastLine(filePath);
                            if (!string.IsNullOrWhiteSpace(lastLine))
                            {
                                DateTime.TryParse(lastLine.Split(",")[0], out lastDateVehicle);
                            }
                        }
                        foreach (var record in records)
                        {
                            writeRecordInFile(writer, record.Value, lastDateVehicle);
                        }
                    }
                }
            }
            Console.WriteLine($"Backup done. {gpsRecordsWritten + statusRecordsWritten} events stored ({gpsRecordsWritten} log events and {statusRecordsWritten} status events) ");
            vehicleRecords.Clear();
            lastDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Writes a device record to the specified CSV file, ensuring that only records newer than the last recorded date are written.
        /// </summary>
        /// <param name="writer">TextWriter object used to write to the CSV file.</param>
        /// <param name="record">GPS or status record to be written.</param>
        /// <param name="lastDateVehicle">Last recorded date for this device.</param>
        private void writeRecordInFile(TextWriter writer, object record, DateTime lastDateVehicle)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                switch (record)
                {
                    case LogRecord logRecord:
                        if (logRecord.DateTime > lastDateVehicle)
                        {
                            AppendValues(sb, logRecord.DateTime);
                            AppendValues(sb, logRecord.Id);
                            AppendDeviceValues(sb, logRecord.Device);
                            AppendValues(sb, logRecord.Latitude.ToString(CultureInfo.InvariantCulture));
                            AppendValues(sb, logRecord.Longitude.ToString(CultureInfo.InvariantCulture));
                            AppendValues(sb, logRecord.Speed);
                            AppendValues(sb, "-");
                            writer.WriteLine(sb.ToString().TrimEnd(','));
                            gpsRecordsWritten++;
                        }
                        break;
                    case StatusData statusData:
                        if (statusData.DateTime > lastDateVehicle)
                        {
                            AppendValues(sb, statusData.DateTime);
                            AppendValues(sb, statusData.Id);
                            AppendDeviceValues(sb, statusData.Device);
                            AppendValues(sb, "-");
                            AppendValues(sb, "-");
                            AppendValues(sb, "-");
                            AppendValues(sb, statusData.Data);
                            writer.WriteLine(sb.ToString().TrimEnd(','));
                            statusRecordsWritten++;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                if (e is IOException)
                {
                    // Possiable system out of memory exception or file lock. Log then sleep for a minute and continue.
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
            }
        }

        /// <summary>
        /// Efficiently reads the last line of a file, reading the file backwards.
        /// Based in a IA code.
        /// </summary>
        /// <param name="filePath">Relative path of the file.</param>
        /// <param name="encoding">Encoding of the file.</param>
        /// <returns>Last line of the file.</returns>
        private string ReadLastLine(string filePath, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8; // Using UTF-8 as default

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                long fileSize = fs.Length;

                // Handling empty files
                if (fileSize == 0)
                    return null;

                long position = fileSize - 1; // Starting at the end of file
                int bufferSize = 1; // Reading byte to byte
                byte[] buffer = new byte[bufferSize];
                StringBuilder lineBuilder = new StringBuilder();

                // Reading backwards until a carriage return character is found
                while (position >= 0)
                {
                    fs.Seek(position, SeekOrigin.Begin);
                    fs.Read(buffer, 0, bufferSize);

                    // Stopping the search if we found a carriage return character
                    if (buffer[0] == '\n' && lineBuilder.Length > 0)
                    {
                        break;
                    }

                    // Add character to StringBuilder's start
                    lineBuilder.Insert(0, encoding.GetString(buffer));

                    position--;
                }

                return lineBuilder.ToString().TrimEnd('\r', '\n'); // Remove carriage return characters
            }
        }

        /// <summary>
        /// Appends a formatted value to the specified StringBuilder.
        /// Cloned from Geotab example.
        /// </summary>
        /// <param name="sb">Stringbuilder to append the value to.</param>
        /// <param name="o">The value to be appended.</param>
        private void AppendValues(StringBuilder sb, object o)
        {
            sb.Append(o);
            sb.Append(',');
        }

        /// <summary>
        /// Appends device information (name, serial number and VIN) to the specified StringBuilder.
        /// Cloned from Geotab example.
        /// </summary>
        /// <param name="sb">Stringbuilder to append the information.</param>
        /// <param name="device">Device object containint the information to be appended.</param>
        private void AppendDeviceValues(StringBuilder sb, Device device)
        {
            AppendValues(sb, device.Name.Replace(",", " "));
            AppendValues(sb, device.SerialNumber);
            GoDevice goDevice = device as GoDevice;
            AppendValues(sb, (goDevice == null ? "" : goDevice.VehicleIdentificationNumber ?? "").Replace(",", " "));
        }

        /// <summary>
        /// Requests to stop.
        /// </summary>
        public void RequestStop()
        {
            stop = true;
        }

        /// <summary>
        /// The work action to getData and store in CSV files.
        /// </summary>
        /// <param name="continuous">Boolean to make the backup periodically.</param>
        public async Task work(bool continuous)
        {
            do
            {
                await getDataAndBackup();
            }
            while (continuous && !stop);
        }
    }
}