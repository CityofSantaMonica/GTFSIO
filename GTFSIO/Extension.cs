using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GTFSIO
{
    public static class Extension
    {
        public static void ReadCSV(this System.Data.DataTable table, Stream stream, String Delimiters = ",")
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
    }
}
