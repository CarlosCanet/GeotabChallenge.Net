# Geotab Challenge for Carlos Canet
This repository contains the solution developed by Carlos Canet for the Geotab Full Stack Developer challenge. The solution leverages the Geotab SDK and implements several optimizations to meet the challenge requirements.

## Modifications to the Geotab Example
The present solution is based on the [DataFeed example](https://github.com/Geotab/sdk-dotnet-samples/tree/master/DataFeed) from the Geotab SDK with the following modifications:
- **General Changes**:
  - Removed flags for parameters not used.
- **FeedProcessor.cs**:
  - Added functionality to store the last GPS and status versions(tokens) in a configuration file. This ensures consistency in backup files across multiple executions.
- **FeedToCsv.cs**:
  - Introduced a new attribute, `vehicleRecords`, which is a map containing a sorted list (ordered by timestamp and event ID) to combine both types of records (GPS and Status Data).
  - Added a new method, `WriteDataToCsvByVehicle`, to generate a separate file for each vehicle.

## Key Features
- **Automatic Backup Folder Creation**: A folder is created to store backup files when needed.
- **Version Tracking**: 
  - You can specify the starting version for GPS or status data backups. If no version is provided as a parameter, the application will process events sequentially without skipping any. If no version is specified, the backup will start from the events of the last configurable number of hours.
  - Manually provided parameters override version information from the configuration file.
- **Efficient Data Processing**: 
  - All vehicles/devices from the database are preloaded into a map for improved performance.
  - The data processing loop handles API calls, result parsing, and file writing efficiently.
- **Unified API Calls**: The program consolidates API requests for both GPS and status data into a single operation to improve efficiency.
- **Ordered Event Storage**: All records (GPS and status) are processed, merged, and sorted by timestamp before being written to files.

## How to run

To execute the application, use the following command:
```
> dotnet run --s server --d database --u user --p password --gt nnn --st nnn --f file path --c
```
### Command Line Arguments
```
--s  The Server
--d  The Database
--u  The User
--p  The Password
--gt [optional] The last known gps data version
--st [optional] The last known status data version
--f  [optional] The folder to save any output files to, if applicable. Defaults to the current directory.
--c  [optional] Run the feed continuously. Defaults to false.
```

### Example usage:
```
> dotnet run --s "my.geotab.com" --d "database" --u "user@email.com" --p "password" --gt 0000000000000000 --st 0000000000000000 --f Backup_Files
```

## CSV Output
For each vehicle/device in the database, the program generates a dedicated CSV file. Fields without applicable values are represented by a `-` (e.g., odometer value in GPS records or GPS data in status records).
| # | Field name | Description | Example |
|---|---|---|---|
| 1 | Timestamp | The date and time in UTC of the event (GPS postion or odometer reading) | 18/11/2024 03:03:53 |
| 2 | Event id | The identification value for the event | b284DA9 |
| 3 | Vehicle name | The name of vehicle | Demo - 01 |
| 4 | Vehicle Serial Number | The unique serial number printed on the GO device	 | G90000000001 |
| 5 | Vehicle Identification Number (VIN) | The unique vehicle identification number | 1HTMSTAR0KH0000001 |
| 6 | Latitude | The coordinate latitude in decimal degrees. | 43.7611504 |
| 7 | Longitude | The coordinate longitude in decimal degrees. | -79.4690323 |
| 8 | Vehicle Speed | The speed in km/h. | 51 |
| 9 | Odometer value | The value of the odometer in m. | 92271900 |
