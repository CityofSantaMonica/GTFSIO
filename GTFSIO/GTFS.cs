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
        public static String GTFSOptionalSchemaName { get { return "gtfs.xsd"; } }
        public FeedTables FeedTables { get; set; }
        public String Path { get; set; }

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
            if (feedFiles.Keys.Contains(GTFSOptionalSchemaName))
            {
                var tempDataSet = new System.Data.DataSet();
                tempDataSet.ReadXmlSchema(feedFiles[GTFSOptionalSchemaName]);
                FeedTables.Merge(tempDataSet);
            }

            //get an ordering for import that maintains foreign key relationships
            //and discards tables that can't be imported
            var orderedNames = TableNamesOrderedByDependency(feedFiles.Keys.ToArray());
            
            foreach (var tableName in orderedNames)
            {
                Console.WriteLine("Reading table {0}", tableName);

                var feedTable = FeedTables.Tables[tableName];

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
                    foreach (var table in FeedTables.Tables.OfType<DataTable>().Where(item => item.Rows.Count > 0))
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
                        var entry = archive.CreateEntry(GTFSOptionalSchemaName);
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
                    foreach (var table in FeedTables.Tables.OfType<DataTable>().Where(item => item.Rows.Count > 0))
                    {
                        using (var streamWriter = File.CreateText(System.IO.Path.Combine(path, table.TableName)))
                        {
                            table.WriteCSV(streamWriter);
                        }
                    }
                    if (ShouldCreateSchema())
                    {
                        using (var streamWriter = File.CreateText(System.IO.Path.Combine(path, GTFSOptionalSchemaName)))
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
                    var feedTable = FeedTables.Tables[workingName];
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
            return !FeedTables.Tables.Cast<DataTable>().All(item => item.ExtendedProperties.ContainsKey(UserGeneratedTableKey));
        }
    }
}
