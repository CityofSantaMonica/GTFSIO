using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.VisualBasic.FileIO;

namespace GTFS
{
    public class GTFS
    {
        public GTFS()
        {
            FeedTables = new FeedTables();
        }
        public GTFS(String path)
        {
            Path = path;
            FeedTables = new FeedTables();
            var feedFiles = new FeedFiles();
            //var feedQueue = new Queue<KeyValuePair<String, Stream>>();
            if (File.Exists(path) && System.IO.Path.GetExtension(path).ToLower().Equals(".zip"))
            {
                feedFiles = new FeedFiles(new ZipArchive(File.OpenRead(path), ZipArchiveMode.Read));
            }
            else if (Directory.Exists(path))
            {
                feedFiles = new FeedFiles(new DirectoryInfo(path));
            }
            var orderedNames = TableNamesOrderedByDependancy(feedFiles.Keys.ToArray());
            //while (feedFiles.Count > 0)
            //{
            //    foreach (var feedFile in feedFiles)
            //    {
            //        var feedTable = FeedTables.Tables[feedFile.Key];
            //        if (feedTable != null)
            //        {
            //            if (feedTable.ParentRelations.Count == 0 || feedTable.ParentRelations.Cast<DataRelation>().All(item => feedQueue.Select(a => a.Key).Contains(item.ParentTable.TableName)))
            //            {
            //                feedQueue.Enqueue(feedFile);
            //                feedFiles.Remove(feedFile.Key);
            //                break;
            //            }
            //        }
            //    }
            //}
            foreach (var tableName in orderedNames)
            {
                Console.WriteLine("Reading table {0}", tableName);
                var feedTable = FeedTables.Tables[tableName];
                if (feedTable != null)
                    feedTable.ReadCSV(feedFiles[tableName]);
            }
            //while (feedQueue.Count > 0)
            //{
            //    var feedFile = feedQueue.Dequeue();
            //    var feedTable = FeedTables.Tables[feedFile.Key];
            //    if (feedTable != null)
            //        feedTable.ReadCSV(feedFile.Value);
            //}
            feedFiles.Zip = null;
        }
        public String[] TableNamesOrderedByDependancy(String[] tableNames)
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
        public void WriteCSVStream(StreamWriter streamWriter, DataTable table, String Delimiters = ",")
        {
            var rowFormat = String.Join(Delimiters, table.Columns.OfType<DataColumn>().Select(column => column.DataType.Name).Select((name, index) => "{" + index.ToString() + "}").ToArray());
            streamWriter.WriteLine(String.Join(Delimiters, table.Columns.OfType<DataColumn>().Select(column => column.ColumnName).ToArray()));
            table.Rows.OfType<DataRow>().ToList().ForEach(row =>
            {
                var columns = new List<Object>();
                row.Table.Columns.OfType<DataColumn>().Select(column => new { TypeName = column.DataType.Name, column.Ordinal }).ToList().ForEach(column =>
                {
                    if (row.IsNull(column.Ordinal))
                        columns.Add(String.Empty);
                    else
                        switch (column.TypeName)
                        {
                            case "Boolean":
                                columns.Add(Convert.ToInt32(row[column.Ordinal]));
                                break;
                            case "DateTime":
                                {
                                    var dateTime = (DateTime)row[column.Ordinal];
                                    var year = dateTime.Year;
                                    var month = dateTime.Month;
                                    var day = dateTime.Day;
                                    columns.Add(String.Format("{0:D4}{1:D2}{2:D2}", year, month, day));
                                }
                                break;
                            case "Decimal":
                            case "Double":
                                columns.Add(String.Format("{0:0.######}", row[column.Ordinal]));
                                break;
                            case "Int32":
                            case "String":
                                if (row[column.Ordinal].ToString().Contains(',') || row[column.Ordinal].ToString().Contains('"'))
                                {
                                    if (row[column.Ordinal].ToString().Contains('"'))
                                        row[column.Ordinal] = row[column.Ordinal].ToString().Replace("\"", "\"\"");
                                    row[column.Ordinal] = "\"" + row[column.Ordinal].ToString() + "\"";
                                }
                                columns.Add(row[column.Ordinal]);
                                break;
                            case "TimeSpan":
                                {
                                    var timeSpan = (TimeSpan)row[column.Ordinal];
                                    var hours = timeSpan.TotalHours;
                                    var minutes = timeSpan.Minutes;
                                    var seconds = timeSpan.Seconds;
                                    columns.Add(String.Format("{0:D2}:{1:D2}:{2:D2}", Convert.ToInt32(Math.Floor(hours)), minutes, seconds));
                                }
                                break;
                        }
                });
                streamWriter.WriteLine(String.Format(rowFormat, columns.ToArray()));
            });
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
                                WriteCSVStream(streamWriter, table);
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
                            WriteCSVStream(streamWriter, table);
                        }
                    }
                }
            }
        }
        public String Path { get; set; }
        public FeedTables FeedTables { get; set; }
    }
}
