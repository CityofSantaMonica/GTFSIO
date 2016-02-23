using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GTFSIO
{
    public static class Extension
    {
        public static void ReadCSV(this DataTable table, Stream stream, String Delimiters = ",")
        {
            var timespanExpression = new Regex("([0-9]+):([0-9]+):([0-9]+)");
            var parseDictionary = new Dictionary<Type, Delegate>();
            parseDictionary.Add(typeof(Boolean), new Func<String, Boolean>(fieldValue => Convert.ToBoolean(Int32.Parse(fieldValue))));
            parseDictionary.Add(typeof(DateTime), new Func<String, DateTime>(fieldValue => new DateTime(Int32.Parse(fieldValue.Substring(0, 4)), Int32.Parse(fieldValue.Substring(4, 2)), Int32.Parse(fieldValue.Substring(6, 2)))));
            parseDictionary.Add(typeof(Decimal), new Func<String, Decimal>(fieldValue => Decimal.Parse(fieldValue)));
            parseDictionary.Add(typeof(Double), new Func<String, Decimal>(fieldValue => Decimal.Parse(fieldValue)));
            parseDictionary.Add(typeof(Int32), new Func<String, Int32>(fieldValue => Int32.Parse(fieldValue)));
            parseDictionary.Add(typeof(TimeSpan), new Func<String, Object>(fieldValue => { try { var timeSpanPart = fieldValue.Split(':'); return new TimeSpan(Int32.Parse(timeSpanPart[0]), Int32.Parse(timeSpanPart[1]), Int32.Parse(timeSpanPart[2])); } catch { return DBNull.Value; } }));
            var textFieldParser = new TextFieldParser(stream);
            textFieldParser.TextFieldType = FieldType.Delimited;
            textFieldParser.SetDelimiters(Delimiters);
            textFieldParser.HasFieldsEnclosedInQuotes = true;
            var fieldReferences = textFieldParser.ReadFields();

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
                            if (parseDictionary.ContainsKey(table.Columns[fieldReference].DataType))
                                newRow[fieldReference] = parseDictionary[table.Columns[fieldReference].DataType].DynamicInvoke(fieldValue);
                            else
                                newRow[fieldReference] = fieldValue;
                        }
                    }
                }
                try
                {
                    table.Rows.Add(newRow);
                }
                catch { }
            }
            stream.Close();
        }

        public static void WriteCSV(this DataTable table, StreamWriter streamWriter, String Delimiters = ",")
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
    }
}
