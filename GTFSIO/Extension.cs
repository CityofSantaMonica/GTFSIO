using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace GTFSIO
{
    public static class Extension
    {
        static Dictionary<Type, Delegate> ParseDictionary
        {
            get
            {
                return new Dictionary<Type, Delegate>()
                {
                    { typeof(Boolean), new Func<String, Boolean>(fieldValue => Convert.ToBoolean(Int32.Parse(fieldValue))) },
                    { typeof(DateTime), new Func<String, DateTime>(fieldValue => new DateTime(Int32.Parse(fieldValue.Substring(0, 4)), Int32.Parse(fieldValue.Substring(4, 2)), Int32.Parse(fieldValue.Substring(6, 2)))) },
                    { typeof(Decimal), new Func<String, Decimal>(fieldValue => Decimal.Parse(fieldValue)) },
                    { typeof(Double), new Func<String, Decimal>(fieldValue => Decimal.Parse(fieldValue)) },
                    { typeof(Int32), new Func<String, Int32>(fieldValue => Int32.Parse(fieldValue)) },
                    { typeof(TimeSpan), new Func<String, Object>(fieldValue => { try { var timeSpanPart = fieldValue.Split(':'); return new TimeSpan(Int32.Parse(timeSpanPart[0]), Int32.Parse(timeSpanPart[1]), Int32.Parse(timeSpanPart[2])); } catch { return DBNull.Value; }}) },
                };
            }
        }

        /// <summary>
        /// Populate this <see cref="DataTable"/> from separated values.
        /// </summary>
        /// <param name="table">The <see cref="DataTable"/> to populate.</param>
        /// <param name="stream">The <see cref="Stream"/> to read data from.</param>
        /// <param name="Delimiters">The delimiter string used to separate data fields.</param>
        public static void ReadCSV(this DataTable table, Stream stream, String Delimiters = ",")
        {
            var textFieldParser = new TextFieldParser(stream);
            textFieldParser.TextFieldType = FieldType.Delimited;
            textFieldParser.SetDelimiters(Delimiters);
            textFieldParser.HasFieldsEnclosedInQuotes = true;
            var fieldReferences = textFieldParser.ReadFields();
            var serviceTable = table.ParentRelations.OfType<DataRelation>().Where(item => item.ParentTable.TableName.Equals("services")).Select(item => item.ParentTable as FeedTables.ServicesDataTable).SingleOrDefault();

            while (!textFieldParser.EndOfData)
            {
                var fields = textFieldParser.ReadFields();
                var newRow = table.NewRow();
                for (var index = 0; index < fieldReferences.Length; index++)
                {
                    var fieldReference = fieldReferences[index];
                    var fieldValue = fields[index];
                    if (table.Columns.Contains(fieldReference))
                    {
                        if (!String.IsNullOrEmpty(fieldValue))
                        {
                            if (ParseDictionary.ContainsKey(table.Columns[fieldReference].DataType))
                                newRow[fieldReference] = ParseDictionary[table.Columns[fieldReference].DataType].DynamicInvoke(fieldValue);
                            else
                                newRow[fieldReference] = fieldValue;
                        }
                    }
                }
                if (serviceTable != null)
                {
                    var service_id = newRow["service_id"].ToString();
                    var service = serviceTable.FindByservice_id(service_id);
                    if (service == null)
                        serviceTable.AddServicesRow(service_id);
                }
                try
                {
                    table.Rows.Add(newRow);
                }
                catch { }
            }
            stream.Close();
        }

        /// <summary>
        /// Serialize this <see cref="DataTable"/> to separated values.
        /// </summary>
        /// <param name="table">The <see cref="DataTable"/> to serialize.</param>
        /// <param name="streamWriter">The <see cref="StreamWriter"/> used to write data.</param>
        /// <param name="Delimiters">The delimiter string used to separate data fields.</param>
        public static void WriteCSV(this DataTable table, StreamWriter streamWriter, String Delimiters = ",")
        {
            var rowFormat =
                String.Join(
                    Delimiters,
                    table.DataColumns()
                         .Select((_, index) => "{" + index.ToString() + "}")
                         .ToArray()
                );

            streamWriter.WriteLine(String.Join(Delimiters, table.DataColumns().Select(column => column.ColumnName).ToArray()));

            table.DataRows().ToList().ForEach(row =>
            {
                var columns = new List<Object>();
                row.DataColumns().Select(column => new { TypeName = column.DataType.Name, column.Ordinal }).ToList().ForEach(column =>
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
                            case "Byte":
                            case "Int16":
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
                            default:
                                break;
                        }
                });
                streamWriter.WriteLine(String.Format(rowFormat, columns.ToArray()));
            });
        }

        /// <summary>
        /// Convenience method for the collection of <see cref="DataColumn"/> associated with this <see cref="DataRow"/>.
        /// </summary>
        public static IEnumerable<DataColumn> DataColumns(this DataRow row)
        {
            return row.Table.DataColumns();
        }

        /// <summary>
        /// Convenience method for the collection of <see cref="DataColumn"/> associated with this <see cref="DataTable"/>.
        /// </summary>
        public static IEnumerable<DataColumn> DataColumns(this DataTable table)
        {
            return table.Columns.OfType<DataColumn>();
        }

        /// <summary>
        /// Convenience method for the collection of <see cref="DataRow"/> associated with this <see cref="DataTable"/>.
        /// </summary>
        public static IEnumerable<DataRow> DataRows(this DataTable table)
        {
            return table.Rows.OfType<DataRow>();
        }
    }
}
