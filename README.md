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

```csharp
var gtfs = new GTFS("C:\path\to\gtfs\files");
```

Or a `.zip` file containing the GTFS feed files

```csharp
var gtfs = new GTFS("C:\path\to\gtfs.zip");
```

Now query the feed tables using LINQ and strongly typed
[`DataRows`](https://msdn.microsoft.com/en-us/library/system.data.datarow)

```csharp
var agencyName = gtfs.agency.First().agency_name;

var freeRides = gtfs.fare_attributes.Where(attr => attr.price == 0.0);

var wheelchairStops = gtfs.stops.Where(stop => stop.wheelchair_boarding == "true");
```

### Ad-hoc supplementary data

String indexing is also supported for the feed tables and their columns

```csharp
var agencyName = gtfs["agency.txt"].Rows[0]["agency_name"].ToString();
```

This is useful since `GTFSIO` will create in-memory `DataTables` for each data file in
the input directory/archive, including files that don't correspond to the GTFS spec,
as long as a schema is provided (in the form of a `gtfs.xsd` file).

For example, assuming the following directory:

```
|-- data
    |-- agency.txt
    |-- calendar.txt
    |-- custom.csv
    |-- gtfs.xsd
    |-- routes.txt
    |-- stops.txt
    |-- stop_times.txt
    |-- trips.txt
```

Where the `.txt` files are standard GTFS feed files, and `custom.csv` is a non-spec file
like

| trip_id | extra_data |
| ------- | ---------- |
| 0       | "abcde"    |
| 1       | "fghij"    |
| 2       | "klmno"    |

And `gtfs.xsd` is an xml schema like:

```xml
<?xml version='1.0' encoding='utf-8'?>
<xs:schema targetNamespace='http://tempuri.org/XMLSchema.xsd' xmlns='http://tempuri.org/XMLSchema.xsd' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:element name='custom.csv'>
        <xs:complexType>
            <xs:sequence>
                <xs:element name='trip_id' type='xs:integer'/>
                <xs:element name='extra_data' type='xs:string'/>
            </xs:sequence>
        </xs:complexType>
    </xs:element>
</xs:schema>
```

Then one could read the directory as normal, and have access to both GTFS-spec data and
the custom data

```csharp
var gtfs = new GTFS(".\data");

var joinedTripData =
    //join the trips.txt table
    gtfs.trips.Join(
        //with the DataRows in our custom table
        gtfs["custom.csv"].DataRows(),
        //on the trip_id from trips.txt
        trip => trip.trip_id,
        //and trip_id in the custom table
        custom => custom["trip_id"],
        //create a new anonymous object with the trip data
        //and the id from the custom table
        (trip, custom) => new {
            trip = trip,
            extra = custom["extra_data"]
        }
    );
```
### GTFS Writes

`GTFSIO` can also write the current state of the feed tables to disk. All tables that have at
least 1 row of data will be written.

```csharp
var gtfs = new GTFS(".\data");

//transform or otherwise process the data in memory

//then save it out to a directory
gtfs.Save(".\new_data");

//or a zip archive
gtfs.Save(".\new_data.zip");
```

`GTFSIO` creates the appropriate `gtfs.xsd` file if custom `DataTables` are added
and serialized:

```csharp
var gtfs = new GTFS();

var table = new DataTable();
table.Columns.Add(new DataColumn("trip_id"));
table.Columns.Add(new DataColumn("extra_data"));
table.Rows.Add(0, "abcde");

gtfs.Add(table);
gtfs.Save(".\path");

// ==> ".\path\gtfs.xsd" exists and is similar to the example above.
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