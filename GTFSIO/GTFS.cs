using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GTFSIO
{
    /// <summary>
    /// An in-memory representation of GTFS data tables.
    /// </summary>
    public class GTFS
    {
        public static string ExcludeFromDataExportKey { get { return "ExcludeFromDataExport"; } }
        public static string ExcludeFromSchemaExportKey { get { return "ExcludeFromSchemaExport"; } }
        public static string OptionalSchemaName { get { return "gtfs.xsd"; } }
        

        public FeedTables FeedTables { get; set; }
        public string Path { get; set; }

        public FeedTables.AgencyDataTable agency
        {
            get { return FeedTables.agency; }
        }
        public FeedTables.CalendarDataTable calendar
        {
            get { return FeedTables.calendar; }
        }
        public FeedTables.CalendarDatesDataTable calendar_dates
        {
            get { return FeedTables.calendar_dates; }
        }
        public FeedTables.FareAttributesDataTable fare_attributes
        {
            get { return FeedTables.fare_attributes; }
        }
        public FeedTables.FareRulesDataTable fare_rules
        {
            get { return FeedTables.fare_rules; }
        }
        public FeedTables.FeedInfoDataTable feed_info
        {
            get { return FeedTables.feed_info; }
        }
        public FeedTables.FrequenciesDataTable frequencies
        {
            get { return FeedTables.frequencies; }
        }
        public FeedTables.RoutesDataTable routes
        {
            get { return FeedTables.routes; }
        }
        public FeedTables.ShapesDataTable shapes
        {
            get { return FeedTables.shapes; }
        }
        public FeedTables.ServicesDataTable services
        {
            get { return FeedTables.services; }
        }
        public FeedTables.StopsDataTable stops
        {
            get { return FeedTables.stops; }
        }
        public FeedTables.StopTimesDataTable stop_times
        {
            get { return FeedTables.stop_times; }
        }
        public FeedTables.TransfersDataTable transfers
        {
            get { return FeedTables.transfers; }
        }
        public FeedTables.TripsDataTable trips
        {
            get { return FeedTables.trips; }
        }

        public DataTable this[int index]
        {
            get { return FeedTables.Tables[index]; }
        }
        public DataTable this[string index]
        {
            get { return FeedTables.Tables[index]; }
        }

        public IEnumerable<DataTable> DataTables
        {
            get { return FeedTables.Tables.OfType<DataTable>(); }
        }

        public GTFS()
        {
            FeedTables = new FeedTables();
        }

        /// <summary>
        /// Create a new GTFS object using data found in the given path.
        /// </summary>
        /// <param name="path">The full path to a readable .zip file or directory.</param>
        public GTFS(String path) : this()
        {
            Path = path;

            var feedFiles = new FeedFiles();

            if (File.Exists(path) && System.IO.Path.GetExtension(path).ToLower().Equals(".zip"))
            {
                feedFiles = new FeedFiles(new ZipArchive(File.OpenRead(path), ZipArchiveMode.Read));
            }
            else if (Directory.Exists(path))
            {
                feedFiles = new FeedFiles(new DirectoryInfo(path));
            }

            //if a schema file was provided
            //merge it with the standard FeedTables schema
            if (feedFiles.Keys.Contains(OptionalSchemaName))
            {
                var tempDataSet = new DataSet();
                tempDataSet.ReadXmlSchema(feedFiles[OptionalSchemaName]);
                FeedTables.Merge(tempDataSet);
            }

            //get an ordering for import that maintains foreign key relationships
            //and discards tables that can't be imported
            var orderedNames = TableNamesOrderedByDependency(feedFiles.Keys.ToArray());

            foreach (var tableName in orderedNames)
            {
                Console.WriteLine("Reading table {0}", tableName);

                var feedTable = this[tableName];

                if (feedTable != null)
                    feedTable.ReadCSV(feedFiles[tableName]);
            }

            feedFiles.Dispose();
        }

        /// <summary>
        /// Add a <see cref="DataTable"/> to this object's collection.
        /// </summary>
        /// <param name="table">The <see cref="DataTable"/> to add.</param>
        public void Add(DataTable table)
        {
            FeedTables.Tables.Add(table);
        }

        /// <summary>
        /// Write the current state of <see cref="FeedTables"/> to the given path.
        /// </summary>
        /// <param name="path">The full path to a .zip file or writeable directory.</param>
        public void Save(String path)
        {
            if (path.ToLower().EndsWith(".zip"))
            {
                using (var archive = new ZipArchive(File.OpenWrite(path), ZipArchiveMode.Create))
                {
                    foreach (var table in TablesForDataExport())
                    {
                        var entry = archive.CreateEntry(table.TableName);
                        using (var entryStream = entry.Open())
                        {
                            using (var streamWriter = new StreamWriter(entryStream))
                            {
                                table.WriteCSV(streamWriter);
                            }
                        }
                    }
                    if (ShouldCreateSchema())
                    {
                        var entry = archive.CreateEntry(OptionalSchemaName);
                        using (var entryStream = entry.Open())
                        {
                            using (var streamWriter = new StreamWriter(entryStream))
                            {
                                DataSetForSchemaExport().WriteXmlSchema(streamWriter);
                            }
                        }
                    }
                }
            }
            else
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                if (Directory.Exists(path))
                {
                    foreach (var table in TablesForDataExport())
                    {
                        using (var streamWriter = File.CreateText(System.IO.Path.Combine(path, table.TableName)))
                        {
                            table.WriteCSV(streamWriter);
                        }
                    }
                    if (ShouldCreateSchema())
                    {
                        using (var streamWriter = File.CreateText(System.IO.Path.Combine(path, OptionalSchemaName)))
                        {
                            DataSetForSchemaExport().WriteXmlSchema(streamWriter);
                        }
                    }
                }
            }
        }

        //topological sort on the given table names to adhere to foreign key relationships during import
        private String[] TableNamesOrderedByDependency(String[] tableNames)
        {
            var workingNames = tableNames.ToList();
            var sortedNames = new Queue<String>();

            // front-load services and calendar.txt and/or calendar_dates.txt so that service_ids are available first
            sortedNames.Enqueue("services");
            foreach (var workingName in new String[] { "calendar.txt", "calendar_dates.txt" })
            {
                if (workingNames.Contains(workingName))
                {
                    sortedNames.Enqueue(workingName);
                    workingNames.Remove(workingName);
                }
            }

            while (workingNames.Count > 0)
            {
                foreach (var workingName in workingNames)
                {
                    var feedTable = this[workingName];
                    if (feedTable != null)
                    {
                        //if this table has no relations to other tables
                        //or its dependent tables are already accounted for in the sort
                        var parentRelations = feedTable.ParentRelations.Cast<DataRelation>();
                        if (!parentRelations.Any() || parentRelations.All(rel => sortedNames.Contains(rel.ParentTable.TableName)))
                        {
                            //append this table to the sort
                            sortedNames.Enqueue(workingName);
                            workingNames.Remove(workingName);
                            break;
                        }
                    }
                    else
                    {
                        //this table doesn't exist in FeedTables
                        //there's no way to import data into it
                        workingNames.Remove(workingName);
                        break;
                    }
                }
            }

            sortedNames.Dequeue();
            return sortedNames.ToArray();
        }

        //get the collection of tables containing data that should be exported
        private IEnumerable<DataTable> TablesForDataExport()
        {
            return DataTables.Where(table => table.Rows.Count > 0 && (!table.ExtendedProperties.ContainsKey(ExcludeFromDataExportKey) || table.ExtendedProperties[ExcludeFromDataExportKey].ToString().Equals("false", StringComparison.OrdinalIgnoreCase)));
        }
        
        //determine, based on the current state of the FeedTables, if a schema file should be written
        private bool ShouldCreateSchema()
        {
            //tables in FeedTables with structure defined in FeedTables.xsd
            //have an extended property with the following key
            string key = "GtfsSpecTable";

            return !DataTables.All(item => item.ExtendedProperties.ContainsKey(key));
        }

        //get a representation of this GTFS dataset appropriate for schema export
        private DataSet DataSetForSchemaExport()
        {
            //get clones of the tables for export (structure only)
            var tablesForExport = DataTables.Where(table => !table.ExtendedProperties.ContainsKey(ExcludeFromSchemaExportKey) || table.ExtendedProperties[ExcludeFromSchemaExportKey].ToString().Equals("false", StringComparison.OrdinalIgnoreCase))
                                            .Select(table => table.Clone())
                                            .ToArray();

            var exportDataSet = new DataSet(FeedTables.DataSetName);
            exportDataSet.Tables.AddRange(tablesForExport);

            return exportDataSet;
        }
    }
}
