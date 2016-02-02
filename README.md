# GTFSIO

Reading and writing GTFS data files in .NET

Available via [Nuget](https://www.nuget.org/)

```bat
PM> Install-Package CSM.GtfsIO
```

License: [MIT](LICENSE.txt)

## Examples

[GTFSR.Services](../GTFSR.Services) is an example of the use of `GTFSIO` in a production
system. The following are some simple code samples.

### GTFS reads

Read a directory containing a set of
[GTFS feed files](https://developers.google.com/transit/gtfs/reference?hl=en#feed-files)

```charp
var gtfs = new GTFS("C:\path\to\gtfs\files\");
```

Or a `.zip` file containing the GTFS feed files

```charp
var gtfs = new GTFS("C:\path\to\gtfs.zip");
```

Now query the feed tables using LINQ and strongly typed
[`DataRows`](https://msdn.microsoft.com/en-us/library/system.data.datarow)

```charp
var feedTables = gtfs.FeedTables;

var agencyName = feedTables._agency_txt.First().agency_name;

var wheelChairFriendlyStopNames =
    feedTables._stops_txt
              .Where(stop => stop.wheelchair_boarding == "true")
              .Select(stop => stop.stop_name);
```

### Ad-hoc supplementary data

String indexing is also supported for the feed tables and their columns

```charp
var agencyName =
    feedTables.Tables["agency_txt"]
              .Rows[0]["agency_name"].ToString();
```

This is useful since `GTFSIO` will create in-memory `DataTables` for each data file in
the input directory/archive; including files that don't correspond to the GTFS spec.

For example, assuming the following directory:

```
|-- data
    |-- agency.txt
    |-- stops.txt
    |-- routes.txt
    |-- trips.txt
    |-- stop_times.txt
    |-- calendar.txt
    |-- custom.csv
```

Where the `.txt` files are standard GTFS feed files, and `custom.csv` is a non-spec file
like

| id | field_a | field_b |
| -- | ------- | ------- |
| 0  | 01234   | "abcde" |
| 1  | 56789   | "fghij" |
| 2  | 01234   | "fghij" |

Then one could read the directory as normal, and have access to both GTFS-spec data and
the custom data

```csharp
var gtfs = new GTFS(".\data\");

var joinedTripData =
    //join the trips_txt table
    gtfs.FeedTables._trips_txt.Join(
        //with the DataRows in our custom table
        custom_data.Rows.OfType<DataRow>(),
        //on the trip id from trips_txt
        trip => trip.trip_id,
        //and field_b in the custom table
        custom => custom["field_b"],
        //create a new anonymous object with the trip data
        //and field_a from the custom table
        (trip, custom) => new {
            trip = trip,
            custom_id = custom["field_a"]
        }
    );
```

### GTFS Writes

`GTFSIO` can also write the current state of the feed tables to disk. All tables that have at
least 1 row of data will be written.

```charp
var gtfs = new GTFS(".\data\");

//transform or otherwise process the data in memory

//then save it out to a directory
gtfs.Save(".\new_data\");

//or a zip archive
gtfs.Save(".\new_data.zip");
```

## Tests

[`NUnit`](http://www.nunit.org/) is used for testing.

To run the tests, download the [NUnit Test Adapter](https://github.com/nunit/nunit3-vs-adapter/wiki)
Visual Studio extension. This extension allows you to see and run all NUnit tests from
within Visual Studio.

Find the *Test Explorer* under **Test > Windows > Test Explorer**:

![test-explorer](https://visualstudiogallery.msdn.microsoft.com/6ab922d0-21c0-4f06-ab5f-4ecd1fe7175d/image/file/66176/16/screenshot.png)

## Nuget Packaging

To build the Nuget package, select the `Nuget` build configuration and rebuild the solution.
**Note** `nuget.exe` should be available on your `PATH`.