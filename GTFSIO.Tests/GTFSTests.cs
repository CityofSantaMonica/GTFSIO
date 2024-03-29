﻿using NUnit.Framework;
using System;
using System.Data;
using System.IO;
using System.Linq;

namespace GTFSIO.Tests
{
    [TestFixture]
    [Category("GTFS")]
    public class GTFSTests
    {
        //the directory where this project lives
        static string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        [Test]
        [Category("Read")]
        public void FeedTables_Initialized_ByDefault()
        {
            var gtfs = new GTFS();

            AssertFeedTablesInitialized(gtfs);
        }

        [TestCase("")]
        [TestCase("bla bla 1-2/3?4")]
        [TestCase("C:\nope")]
        [TestCase("C:\nope\not\there.zip")]
        [Category("Read")]
        public void FeedTables_Initialized_WithNonsense(string nonsense)
        {
            GTFS gtfs = null;

            Assert.DoesNotThrow(() => gtfs = new GTFS(nonsense));

            AssertFeedTablesInitialized(gtfs);
        }

        [Test]
        [Category("Read")]
        public void FeedTables_NotPopulated_FromDirectoryOfCustomFiles_WhenXsdMissing()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "CustomBad"));

            var gtfs = new GTFS(di.FullName);

            AssertFeedTablesInitialized(gtfs);

            var table1 = gtfs["test1.csv"];
            Assert.IsNull(table1);

            var table2 = gtfs["test2.txt"];
            Assert.IsNull(table2);
        }

        [Test]
        [Category("Read")]
        public void FeedTables_NotPopulated_FromZipOfCustomFiles_WhenXsdMissing()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "CustomBad.zip"));

            var gtfs = new GTFS(di.FullName);

            AssertFeedTablesInitialized(gtfs);

            var table1 = gtfs["test1.csv"];
            Assert.IsNull(table1);

            var table2 = gtfs["test2.txt"];
            Assert.IsNull(table2);
        }

        [Test]
        [Category("Read")]
        public void FeedTables_Populated_FromDirectoryOfCombinedFiles_WithXsd()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "Combined"));

            var gtfs = new GTFS(di.FullName);

            AssertSpecGTFS(gtfs);

            var table1 = gtfs["test1.csv"];
            var table2 = gtfs["test2.txt"];

            AssertCustomTables(table1, table2, schemaOnly: false);
        }

        [Test]
        [Category("Read")]
        public void FeedTables_Populated_FromDirectoryOfCustomFiles_WithXsd()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "Custom"));

            var gtfs = new GTFS(di.FullName);

            AssertFeedTablesInitialized(gtfs);

            var table1 = gtfs["test1.csv"];
            var table2 = gtfs["test2.txt"];

            AssertCustomTables(table1, table2, schemaOnly: false);
        }

        [Test]
        [Category("Read")]
        public void FeedTables_Populated_FromDirectoryOfGTFSFiles()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "GTFS"));

            GTFS gtfs = new GTFS(di.FullName);

            AssertSpecGTFS(gtfs);
        }

        [Test]
        [Category("Read")]
        public void FeedTables_Populated_FromZipOfCombinedFiles_WithXsd()
        {
            var fi = new FileInfo(Path.Combine(_baseDirectory, "Data", "Combined.zip"));

            var gtfs = new GTFS(fi.FullName);

            AssertSpecGTFS(gtfs);

            var table1 = gtfs["test1.csv"];
            var table2 = gtfs["test2.txt"];

            AssertCustomTables(table1, table2, schemaOnly: false);
        }

        [Test]
        [Category("Read")]
        public void FeedTables_Populated_FromZipOfCustomFiles_WithXsd()
        {
            var fi = new FileInfo(Path.Combine(_baseDirectory, "Data", "Custom.zip"));

            var gtfs = new GTFS(fi.FullName);

            AssertFeedTablesInitialized(gtfs);

            var table1 = gtfs["test1.csv"];
            var table2 = gtfs["test2.txt"];

            AssertCustomTables(table1, table2, schemaOnly: false);
        }

        [Test]
        [Category("Read")]
        public void FeedTables_Populated_FromZipOfGTFSFiles()
        {
            var fi = new FileInfo(Path.Combine(_baseDirectory, "Data", "GTFS.zip"));

            var gtfs = new GTFS(fi.FullName);

            AssertSpecGTFS(gtfs);
        }

        [Test]
        [Category("Read")]
        public void FeedTables_CanBeIndexed_WithStringNames()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "GTFS"));

            GTFS gtfs = new GTFS(di.FullName);

            var strongTable = gtfs.trips;
            var dataTable = gtfs["trips.txt"];

            Assert.NotNull(dataTable);
            Assert.AreEqual(strongTable.GetType(), dataTable.GetType());
            Assert.AreEqual(strongTable.Count, dataTable.Rows.Count);

            foreach (DataRow dataRow in dataTable.Rows)
            {
                //trip_id is the primary key
                var strongRow = strongTable.Single(strong => String.Equals(strong.trip_id, dataRow["trip_id"]));
                Assert.AreEqual(strongRow.GetType(), dataRow.GetType());
                Assert.AreEqual(strongRow.route_id, dataRow["route_id"]);
                Assert.AreEqual(strongRow.service_id, dataRow["service_id"]);
                Assert.AreEqual(strongRow.trip_headsign, dataRow["trip_headsign"]);
            }
        }

        [Test]
        public void Add_ExcludeFromDataExport_SetsExtendedProperty()
        {
            var gtfs = new GTFS();
            var table = new DataTable("test");

            gtfs.Add(table, excludeFromDataExport: true);

            Assert.True(table.ExtendedProperties.Contains(GTFS.ExcludeFromDataExportKey));
            StringAssert.AreEqualIgnoringCase("true", table.ExtendedProperties[GTFS.ExcludeFromDataExportKey].ToString());
        }

        [Test]
        public void Add_ExcludeFromSchemaExport_SetsExtendedProperty()
        {
            var gtfs = new GTFS();
            var table = new DataTable("test");

            gtfs.Add(table, excludeFromSchemaExport: true);

            Assert.True(table.ExtendedProperties.Contains(GTFS.ExcludeFromSchemaExportKey));
            StringAssert.AreEqualIgnoringCase("true", table.ExtendedProperties[GTFS.ExcludeFromSchemaExportKey].ToString());
        }

        [Test]
        [Category("Read")]
        [Category("Write")]
        public void Save_CanRoundTrip_CustomTables()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "Save"));

            try
            {
                //create the GTFS with custom tables and save it out to the directory
                var firstGtfs = CreateGTFSWithCustomTables();
                firstGtfs.Save(di.FullName);

                //now read a new GTFS in from the directory
                var secondGtfs = new GTFS(di.FullName);

                //and validate the expected table structure
                AssertFeedTablesInitialized(secondGtfs);

                var table1 = secondGtfs["test1.csv"];
                var table2 = secondGtfs["test2.txt"];

                AssertCustomTables(table1, table2, schemaOnly: false);
            }
            finally
            {
                Directory.Delete(di.FullName, true);
            }
        }

        [Test]
        [Category("Write")]
        public void Save_ExcludesTableData_WithExtendedProperty()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "Save"));

            try
            {
                var originalGtfs = CreateGTFSWithCustomTables();

                //create tables with the exclusion property, add some data
                var excludedTable1 = new DataTable("exclude.csv");
                excludedTable1.ExtendedProperties.Add(GTFS.ExcludeFromDataExportKey, "true");
                excludedTable1.Columns.Add(new DataColumn("field1"));
                excludedTable1.Rows.Add("value1");

                originalGtfs.Add(excludedTable1);

                //save this GTFS
                originalGtfs.Save(di.FullName);
                //and read the saved copy back into a new GTFS
                var newGtfs = new GTFS(di.FullName);

                Assert.AreEqual(0, newGtfs[excludedTable1.TableName].Rows.Count);

            }
            finally
            {
                Directory.Delete(di.FullName, true);
            }
        }

        [Test]
        [Category("Write")]
        public void Save_WithCustomTables_WritesSchema()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "Save"));

            try
            {
                //create the GTFS with custom tables and save it out to the directory
                var gtfs = CreateGTFSWithCustomTables();

                gtfs.Save(di.FullName);

                //assert that the schema was written
                string schemaPath = Path.Combine(di.FullName, GTFS.OptionalSchemaName);
                Assert.True(File.Exists(schemaPath));

                //and validate the expected table structure
                var newDataSet = new DataSet();
                newDataSet.ReadXmlSchema(schemaPath);

                var table1 = newDataSet.Tables["test1.csv"];
                var table2 = newDataSet.Tables["test2.txt"];

                AssertCustomTables(table1, table2, schemaOnly: true);
            }
            finally
            {
                Directory.Delete(di.FullName, true);
            }
        }

        [Test]
        [Category("Write")]
        public void Save_ExcludesTablesFromSchema_WithExtendedProperty()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "Save"));

            try
            {
                //create the GTFS with custom tables and save it out to the directory
                var gtfs = CreateGTFSWithCustomTables();

                //this table should not be represented in the resulting schema
                var excludedTable = new DataTable("exclude.csv");
                excludedTable.ExtendedProperties.Add(GTFS.ExcludeFromSchemaExportKey, "true");
                gtfs.Add(excludedTable);

                gtfs.Save(di.FullName);

                string schemaPath = Path.Combine(di.FullName, GTFS.OptionalSchemaName);

                //and validate the expected table structure
                var newDataSet = new DataSet();
                newDataSet.ReadXmlSchema(schemaPath);

                Assert.IsNull(newDataSet.Tables[excludedTable.TableName]);
            }
            finally
            {
                Directory.Delete(di.FullName, true);
            }
        }

        private GTFS CreateGTFSWithCustomTables()
        {
            var table1 = new DataTable("test1.csv");
            table1.Columns.Add(new DataColumn("field1"));
            table1.Columns.Add(new DataColumn("field2"));
            table1.Rows.Add("value1", "value2");

            var table2 = new DataTable("test2.txt");
            table2.Columns.Add(new DataColumn("fieldA"));
            table2.Columns.Add(new DataColumn("fieldB"));
            table2.Rows.Add("valueA", "valueB");

            var gtfs = new GTFS();
            gtfs.Add(table1);
            gtfs.Add(table2);

            return gtfs;
        }

        //Asserts that the GTFS tables are not null
        private void AssertFeedTablesInitialized(GTFS gtfs)
        {
            Assert.NotNull(gtfs.FeedTables);
            Assert.NotNull(gtfs.agency);
            Assert.NotNull(gtfs.calendar);
            Assert.NotNull(gtfs.calendar_dates);
            Assert.NotNull(gtfs.fare_attributes);
            Assert.NotNull(gtfs.fare_rules);
            Assert.NotNull(gtfs.feed_info);
            Assert.NotNull(gtfs.frequencies);
            Assert.NotNull(gtfs.routes);
            Assert.NotNull(gtfs.services);
            Assert.NotNull(gtfs.shapes);
            Assert.NotNull(gtfs.stops);
            Assert.NotNull(gtfs.stop_times);
            Assert.NotNull(gtfs.transfers);
            Assert.NotNull(gtfs.trips);
        }

        //Asserts the structure and optionally existence of data found in Data/Custom/ and Data/Custom.zip
        private void AssertCustomTables(DataTable table1, DataTable table2, bool schemaOnly)
        {
            Assert.NotNull(table1);
            Assert.NotNull(table1.Columns["field1"]);
            Assert.NotNull(table1.Columns["field2"]);

            Assert.NotNull(table2);
            Assert.NotNull(table2.Columns["fieldA"]);
            Assert.NotNull(table2.Columns["fieldB"]);

            if (!schemaOnly)
            {
                Assert.AreEqual(1, table1.Rows.Count);
                Assert.AreEqual("value1", table1.Rows[0]["field1"]);
                Assert.AreEqual("value2", table1.Rows[0]["field2"]);

                Assert.AreEqual(1, table2.Rows.Count);
                Assert.AreEqual("valueA", table2.Rows[0]["fieldA"]);
                Assert.AreEqual("valueB", table2.Rows[0]["fieldB"]);
            }
        }

        //Asserts the existence of (GTFS spec) data found in Data/GTFS/ and Data/GTFS.zip
        private void AssertSpecGTFS(GTFS gtfs)
        {
            AssertAgency(gtfs);
            AssertCalendar(gtfs);
            AssertCalendarDates(gtfs);
            AssertFareAttributes(gtfs);
            AssertFareRules(gtfs);
            AssertFeedInfo(gtfs);
            AssertRoutes(gtfs);
            AssertServices(gtfs);
            AssertShapes(gtfs);
            AssertStops(gtfs);
            AssertStopTimes(gtfs);
            AssertTrips(gtfs);
        }

        private void AssertAgency(GTFS gtfs)
        {
            var table = gtfs.agency;

            Assert.NotNull(table);
            Assert.AreEqual(1, table.Count);

            var row = table[0];

            Assert.AreEqual("1", row.agency_id);
            Assert.AreEqual("Test Agency", row.agency_name);
            Assert.AreEqual("http://www.example.com", row.agency_url);
            Assert.AreEqual("America/Los_Angeles", row.agency_timezone);
            Assert.AreEqual("en", row.agency_lang);
            Assert.AreEqual("310-555-5555", row.agency_phone);
            Assert.AreEqual("http://www.example.com/fares", row.agency_fare_url);
        }

        readonly string[] knownServices = { "1", "2", "3", "4" };

        private void AssertCalendar(GTFS gtfs)
        {
            var table = gtfs.calendar;

            Assert.NotNull(table);
            Assert.AreEqual(3, table.Count);

            foreach(var row in table)
            {
                CollectionAssert.Contains(knownServices, row.service_id);

                switch (row.service_id)
                {
                    case "1":
                        Assert.True(row.saturday);
                        Assert.False(row.sunday || row.monday || row.tuesday || row.wednesday || row.thursday || row.friday);
                        break;
                    case "2":
                        Assert.True(row.sunday);
                        Assert.False(row.saturday || row.monday || row.tuesday || row.wednesday || row.thursday || row.friday);
                        break;
                    case "3":
                        Assert.True(row.monday && row.tuesday && row.wednesday && row.thursday && row.friday);
                        Assert.False(row.saturday || row.sunday);
                        break;
                }
            }
        }

        private void AssertCalendarDates(GTFS gtfs)
        {
            var table = gtfs.calendar_dates;

            Assert.NotNull(table);
            Assert.AreEqual(4, table.Count);

            foreach (var row in table)
            {
                CollectionAssert.Contains(knownServices, row.service_id);
            }
        }

        private void AssertFareAttributes(GTFS gtfs)
        {
            var table = gtfs.fare_attributes;

            Assert.NotNull(table);
            Assert.AreEqual(1, table.Count);

            var row = table[0];

            Assert.AreEqual("1", row.fare_id);
            Assert.AreEqual(1.25, row.price);
            Assert.AreEqual("USD", row.currency_type);
        }

        private void AssertFareRules(GTFS gtfs)
        {
            var table = gtfs.fare_rules;

            Assert.NotNull(table);
            Assert.AreEqual(3, table.Count);

            foreach(var row in table)
            {
                Assert.AreEqual("1", row.fare_id);
            }
        }

        private void AssertFeedInfo(GTFS gtfs)
        {
            var table = gtfs.feed_info;

            Assert.NotNull(table);
            Assert.AreEqual(1, table.Count);

            var row = table[0];

            Assert.AreEqual("Test", row.feed_publisher_name);
            Assert.AreEqual("http://example.com", row.feed_publisher_url);
            Assert.AreEqual("en", row.feed_lang);
        }

        private void AssertRoutes(GTFS gtfs)
        {
            var table = gtfs.routes;

            Assert.NotNull(table);
            Assert.AreEqual(3, table.Count);

            foreach (var row in table)
            {
                Assert.AreEqual("1", row.agency_id);
                Assert.IsNotEmpty(row.route_short_name);
                Assert.IsNotEmpty(row.route_long_name);
                
                //route_type == 'Bus'
                Assert.AreEqual("3", row.route_type);

                Assert.AreEqual(String.Format("http://example.com/routes/{0}", row.route_short_name), row.route_url);
                Assert.IsNotEmpty(row.route_color);
                Assert.IsNotEmpty(row.route_text_color);
            }
        }

        private void AssertServices(GTFS gtfs)
        {
            var table = gtfs.services;

            Assert.NotNull(table);
            Assert.AreEqual(4, table.Count);

            foreach (var row in table)
            {
                CollectionAssert.Contains(knownServices, row.service_id);
            }
        }

        private void AssertShapes(GTFS gtfs)
        {
            var table = gtfs.shapes;

            Assert.NotNull(table);
            Assert.Greater(table.Count, 1);

            foreach (var row in table)
            {
                Assert.GreaterOrEqual(row.shape_pt_lat, 33.0);
                Assert.LessOrEqual(row.shape_pt_lon, -118.0);
                Assert.GreaterOrEqual(row.shape_pt_sequence, 1);
                Assert.GreaterOrEqual(row.shape_dist_traveled, 0.0);
            }
        }

        private void AssertStops(GTFS gtfs)
        {
            var table = gtfs.stops;

            Assert.NotNull(table);
            Assert.Greater(table.Count, 1);

            foreach (var row in table)
            {
                Assert.IsNotEmpty(row.stop_code);
                Assert.IsNotEmpty(row.stop_name);
                Assert.IsNotEmpty(row.stop_desc);
                Assert.GreaterOrEqual(row.stop_lat, 33.0);
                Assert.LessOrEqual(row.stop_lon, -118.0);
            }
        }

        readonly string[] knownTrips = { "110", "111", "120", "121", "130", "131",
                                         "210", "211", "220", "221", "230", "231",
                                         "310", "311", "320", "321", "330", "331" };

        private void AssertStopTimes(GTFS gtfs)
        {
            var table = gtfs.stop_times;

            Assert.NotNull(table);
            Assert.Greater(table.Count, 1);

            foreach (var row in table)
            {
                CollectionAssert.Contains(knownTrips, row.trip_id);

                //regularly scheduled pick up
                Assert.AreEqual("0", row.pickup_type);

                //regularly scheduled drop off
                Assert.AreEqual("0", row.drop_off_type);
            }
        }

        readonly string[] knownBlocks = { "01", "02", "03", "04", "05", "06",
                                          "07", "08", "09", "10", "11", "12",
                                          "13", "14", "15", "16", "17", "18" };

        private void AssertTrips(GTFS gtfs)
        {
            var table = gtfs.trips;

            Assert.NotNull(table);
            Assert.AreEqual(knownTrips.Length, table.Count);

            foreach (var row in table)
            {
                CollectionAssert.Contains(knownServices, row.service_id);
                CollectionAssert.Contains(knownTrips, row.trip_id);
                CollectionAssert.Contains(knownBlocks, row.block_id);

                Assert.IsNotEmpty(row.trip_headsign);
            }
        }
    }
}
