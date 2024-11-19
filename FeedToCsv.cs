using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using Exception = System.Exception;

namespace GeotabChallengeCC
{
    /// <summary>
    /// A feed that produces csv data
    /// Saves each type of data as a separate CSV file to the specified directory
    /// </summary>
    class FeedToCsv
    {
        /// <summary>
        /// The GPS data header
        /// </summary>
        public const string GpsDataHeader = "sVehicle Name,sVehicle Serial Number,sVIN,sDate,dLongitude,dLatitude,iSpeed";

        /// <summary>
        /// The file prefix for gps data
        /// </summary>
        public const string GpsPrefix = "Gps_Data";

        /// <summary>
        /// The status data header
        /// </summary>
        public const string StatusDataHeader = "sVehicle Name,sVehicle Serial Number,sVIN,sDate,sDiagnostic Name,iDiagnostic Code,sSource Name,dValue,sUnits";


        /// <summary>
        /// The file prefix for status data
        /// </summary>
        public const string StatusPrefix = "Status_Data";
        public const string BackupDataHeader = "sTimestamp,sEventId,sVehicleName,sVehicleSerialNumber,sVin,dLatitud,dLongitud,iSpeed,iOdometer";

        readonly IList<LogRecord> gpsRecords;
        readonly string path;
        readonly IList<StatusData> statusRecords;
        readonly IDictionary<Id, SortedList<string, object>> vehicleRecords = new Dictionary<Id, SortedList<string, object>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedToCsv" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="gpsRecords">The GPS records.</param>
        /// <param name="statusRecords">The status records.</param>
        public FeedToCsv(string path, IList<LogRecord> gpsRecords = null, IList<StatusData> statusRecords = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            this.path = path;
            if (!Directory.Exists(this.path))
            {
                Directory.CreateDirectory(this.path);
            }
            this.gpsRecords = gpsRecords ?? new List<LogRecord>();
            this.statusRecords = statusRecords ?? new List<StatusData>();
            foreach (var record in this.gpsRecords)
            {
                if (!vehicleRecords.ContainsKey(record.Device.Id))
                {
                    vehicleRecords[record.Device.Id] = new SortedList<string, object>();
                }
                vehicleRecords[record.Device.Id].Add(record.DateTime.ToString() + record.Id, record);
            }

            // Agregar StatusData
            foreach (var status in this.statusRecords)
            {
                if (!vehicleRecords.ContainsKey(status.Device.Id))
                {
                    vehicleRecords[status.Device.Id] = new SortedList<string, object>();
                }
                vehicleRecords[status.Device.Id].Add(status.DateTime.ToString() + status.Id, status);
            }
        }

        /// <summary>
        /// Runs the instance.
        /// </summary>
        public void Run()
        {
            // if (gpsRecords.Count > 0)
            // {
            //     WriteDataToCsv<LogRecord>();
            // }
            // if (statusRecords.Count > 0)
            // {
            //     WriteDataToCsv<StatusData>();
            // }
            if (vehicleRecords.Count > 0)
            {
                WriteDataToCsvByVehicle();
            }
        }

        static void AppendDeviceValues(StringBuilder sb, Device device)
        {
            AppendValues(sb, device.Name.Replace(",", " "));
            AppendValues(sb, device.SerialNumber);
            GoDevice goDevice = device as GoDevice;
            AppendValues(sb, (goDevice == null ? "" : goDevice.VehicleIdentificationNumber ?? "").Replace(",", " "));
        }

        static void AppendDiagnosticValues(StringBuilder sb, Diagnostic diagnostic)
        {
            AppendName(sb, diagnostic);
            AppendValues(sb, diagnostic.Code);
            Source source = diagnostic.Source;
            if (source != null)
            {
                AppendName(sb, source);
            }
            else
            {
                AppendValues(sb, "");
            }
        }

        static void AppendDriverValues(StringBuilder sb, Driver driver)
        {
            AppendName(sb, driver);
            List<Key> keys = driver.Keys;
            if (keys != null)
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append('~');
                    }
                    sb.Append(keys[i].SerialNumber);
                }
            }
            sb.Append(',');
        }

        static void AppendName(StringBuilder sb, NameEntity entity)
        {
            AppendValues(sb, entity.IsSystemEntity() ? entity.GetType().ToString().Replace("Geotab.Checkmate.ObjectModel.", "").Replace(",", " ") : entity.Name.Replace(",", " "));
        }

        static void AppendValues(StringBuilder sb, object o)
        {
            sb.Append(o);
            sb.Append(',');
        }

        static string MakeFileName(string prefix) => prefix + "-" + DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";

        static void Write<T>(TextWriter writer, T entity, Action<StringBuilder, T> action)
        {
            StringBuilder sb = new StringBuilder();
            action(sb, entity);
            writer.WriteLine(sb.ToString().TrimEnd(','));
        }

        void WriteDataToCsv<T>()
            where T : class
        {
            try
            {
                Type type = typeof(T);
                if (type == typeof(LogRecord))
                {
                    using (TextWriter writer = new StreamWriter(Path.Combine(path, MakeFileName(GpsPrefix)), true))
                    {
                        writer.WriteLine(GpsDataHeader);
                        foreach (LogRecord gpsRecord in gpsRecords)
                        {
                            Write(writer, gpsRecord, (StringBuilder sb, LogRecord logRecord) =>
                            {
                                AppendDeviceValues(sb, logRecord.Device);
                                AppendValues(sb, logRecord.DateTime);
                                AppendValues(sb, logRecord.Longitude);
                                AppendValues(sb, logRecord.Latitude);
                                AppendValues(sb, logRecord.Speed);
                            });
                        }
                    }
                }
                else if (type == typeof(StatusData))
                {
                    using (TextWriter writer = new StreamWriter(Path.Combine(path, MakeFileName(StatusPrefix)), true))
                    {
                        writer.WriteLine(StatusDataHeader);
                        foreach (StatusData statusRecord in statusRecords)
                        {
                            Write(writer, statusRecord, (StringBuilder sb, StatusData statusData) =>
                            {
                                AppendDeviceValues(sb, statusData.Device);
                                AppendValues(sb, statusData.DateTime);
                                Diagnostic diagnostic = statusData.Diagnostic;
                                AppendDiagnosticValues(sb, diagnostic);
                                AppendValues(sb, statusData.Data);
                                if (diagnostic is DataDiagnostic dataDiagnostic)
                                {
                                    AppendName(sb, dataDiagnostic.UnitOfMeasure);
                                }
                            });
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException(type.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (e is IOException)
                {
                    // Possiable system out of memory exception or file lock. Log then sleep for a minute and continue.
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
            }
        }

        void WriteDataToCsvByVehicle()
        {
            try
            {
                foreach (var vehicleRecords in vehicleRecords)
                {
                    string deviceId = vehicleRecords.Key.ToString();
                    SortedList<string, object> records = vehicleRecords.Value;
                    string filePath = Path.Combine(path, deviceId + ".csv");
                    bool fileExists = File.Exists(filePath);
                    using (TextWriter writer = new StreamWriter(filePath, true))
                    {
                        if (!fileExists)
                        {
                            writer.WriteLine(BackupDataHeader);
                        }
                        foreach (var record in records)
                        {
                            switch (record.Value)
                            {
                                case LogRecord log:
                                    Write(writer, log, (StringBuilder sb, LogRecord logRecord) =>
                                    {
                                        AppendValues(sb, logRecord.DateTime);
                                        AppendValues(sb, logRecord.Id);
                                        AppendDeviceValues(sb, logRecord.Device);
                                        AppendValues(sb, logRecord.Longitude.ToString(CultureInfo.InvariantCulture));
                                        AppendValues(sb, logRecord.Latitude.ToString(CultureInfo.InvariantCulture));
                                        AppendValues(sb, logRecord.Speed);
                                        AppendValues(sb, "-");

                                    });
                                    break;
                                case StatusData status:
                                    Write(writer, status, (StringBuilder sb, StatusData statusData) =>
                                    {
                                        AppendValues(sb, statusData.DateTime);
                                        AppendValues(sb, statusData.Id);
                                        AppendDeviceValues(sb, statusData.Device);
                                        AppendValues(sb, "-");
                                        AppendValues(sb, "-");
                                        AppendValues(sb, "-");
                                        AppendValues(sb, statusData.Data);
                                    });
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (e is IOException)
                {
                    // Possiable system out of memory exception or file lock. Log then sleep for a minute and continue.
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
            }
        }
    }
}