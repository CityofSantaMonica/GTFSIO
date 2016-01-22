﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GTFSIO
{
    public class GTFS
    {
        public FeedTables FeedTables { get; set; }
        public String Path { get; set; }

        public GTFS()
        {
            FeedTables = new FeedTables();
        }

        public GTFS(String path)
        {
            Path = path;
            FeedTables = new FeedTables();

            var feedFiles = new FeedFiles();

            if (File.Exists(path) && System.IO.Path.GetExtension(path).ToLower().Equals(".zip"))
            {
                feedFiles = new FeedFiles(new ZipArchive(File.OpenRead(path), ZipArchiveMode.Read));
            }
            else if (Directory.Exists(path))
            {
                feedFiles = new FeedFiles(new DirectoryInfo(path));
            }

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

        public String[] TableNamesOrderedByDependency(String[] tableNames)
        {
            var workingNames = tableNames.ToList();
            var queueNames = new Queue<String>();
            while (workingNames.Count > 0)
            {
                foreach (var workingName in workingNames)
                {
                    var feedTable = FeedTables.Tables[workingName];
                    if (feedTable != null)
                    {
                        if (feedTable.ParentRelations.Count == 0 || feedTable.ParentRelations.Cast<DataRelation>().All(item => queueNames.Select(name => name).Contains(item.ParentTable.TableName)))
                        {
                            queueNames.Enqueue(workingName);
                            workingNames.Remove(workingName);
                            break;
                        }
                    }
                    else
                    {
                        workingNames.Remove(workingName);
                        break;
                    }
                }
            }
            return queueNames.ToArray();
        }

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
                }
            }
        }
    }
}