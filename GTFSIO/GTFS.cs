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
        public static String OptionalSchemaName { get { return "gtfs.xsd"; } }
        public FeedTables FeedTables { get; set; }
        public String Path { get; set; }

        public FeedTables._agency_txtDataTable Agency
        {
            get { return FeedTables._agency_txt; }
        }
        public FeedTables._calendar_txtDataTable Calendar
        {
            get { return FeedTables._calendar_txt; }
        }
        public FeedTables._calendar_dates_txtDataTable CalendarDates
        {
            get { return FeedTables._calendar_dates_txt; }
        }
        public FeedTables._fare_attributes_txtDataTable FareAttributes
        {
            get { return FeedTables._fare_attributes_txt; }
        }
        public FeedTables._fare_rules_txtDataTable FareRules
        {
            get { return FeedTables._fare_rules_txt; }
        }
        public FeedTables._feed_info_txtDataTable FeedInfo
        {
            get { return FeedTables._feed_info_txt; }
        }
        public FeedTables._frequencies_txtDataTable Frequencies
        {
            get { return FeedTables._frequencies_txt; }
        }
        public FeedTables._routes_txtDataTable Routes
        {
            get { return FeedTables._routes_txt; }
        }
        public FeedTables._shapes_txtDataTable Shapes
        {
            get { return FeedTables._shapes_txt; }
        }
        public FeedTables._stops_txtDataTable Stops
        {
            get { return FeedTables._stops_txt; }
        }
        public FeedTables._stop_times_txtDataTable StopTimes
        {
            get { return FeedTables._stop_times_txt; }
        }
        public FeedTables._transfers_txtDataTable Transfers
        {
            get { return FeedTables._transfers_txt; }
        }
        public FeedTables._trips_txtDataTable Trips
        {
            get { return FeedTables._trips_txt; }
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
        /// Write the current state of <see cref="FeedTables"/> to the given path.
        /// </summary>
        /// <param name="path">The full path to a .zip file or writeable directory.</param>
        public void Save(String path)
        {
            if (path.ToLower().EndsWith(".zip"))
            {
                using (var archive = new ZipArchive(File.OpenWrite(path), ZipArchiveMode.Create))
                {
                    foreach (var table in DataTables.Where(item => item.Rows.Count > 0))
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
                                FeedTables.WriteXmlSchema(streamWriter);
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
                    foreach (var table in DataTables.Where(item => item.Rows.Count > 0))
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
                            FeedTables.WriteXmlSchema(streamWriter);
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

            return sortedNames.ToArray();
        }

        //any table in FeedTables that isn't defined in FeedTables.xsd
        //will have a property with the following key
        private static readonly String UserGeneratedTableKey = "Generator_UserTableName";

        //determine, based on the current state of the FeedTables, if a schema file should be written
        private bool ShouldCreateSchema()
        {
            return !DataTables.All(item => item.ExtendedProperties.ContainsKey(UserGeneratedTableKey));
        }
    }
}
