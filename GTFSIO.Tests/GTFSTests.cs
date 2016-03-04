using NUnit.Framework;
using System;
using System.Data;
using System.IO;

namespace GTFSIO.Tests
{
    [TestFixture]
    public class GTFSTests
    {
        //the directory where this project lives
        static readonly string _baseDirectory = Directory.GetParent(TestContext.CurrentContext.TestDirectory).Parent.FullName;

        [TestCase("")]
        [TestCase("bla bla 1-2/3?4")]
        [TestCase("C:\nope")]
        [TestCase("C:\nope\not\there.zip")]
        [Category("Read")]
        public void New_Constructs_WithNonsense(string nonsense)
        {
            GTFS gtfs = null;

            Assert.DoesNotThrow(() => gtfs = new GTFS(nonsense));
            Assert.NotNull(gtfs);
            Assert.NotNull(gtfs.FeedTables);
        }

        [Test]
        [Category("Read")]
        public void New_Default_InitializesFeedTables()
        {
            var gtfs = new GTFS();

            Assert.NotNull(gtfs.FeedTables);
        }

        [Test]
        [Category("Read")]
        public void New_PopulatesFeedTables_FromDirectoryOfCustomFiles()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "Custom"));

            var gtfs = new GTFS(di.FullName);

            Assert.NotNull(gtfs);
            Assert.NotNull(gtfs.FeedTables);

            var table1 = gtfs.FeedTables.Tables["test1.csv"];
            var table2 = gtfs.FeedTables.Tables["test2.txt"];

            AssertCustomTables(table1, table2, schemaOnly: false);
        }

        [Test]
        [Category("Read")]
        public void New_PopulatesFeedTables_FromDirectoryOfGTFSFiles()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "GTFS"));

            GTFS gtfs = new GTFS(di.FullName);

            Assert.NotNull(gtfs);
            Assert.NotNull(gtfs.FeedTables);

            AssertSpecGTFS(gtfs);
        }

        [Test]
        [Category("Read")]
        public void New_PopulatesFeedTables_FromZipOfCustomFiles()
        {
            var fi = new FileInfo(Path.Combine(_baseDirectory, "Data", "Custom.zip"));

            var gtfs = new GTFS(fi.FullName);

            Assert.NotNull(gtfs);
            Assert.NotNull(gtfs.FeedTables);

            var table1 = gtfs.FeedTables.Tables["test1.csv"];
            var table2 = gtfs.FeedTables.Tables["test2.txt"];

            AssertCustomTables(table1, table2, schemaOnly: false);
        }

        [Test]
        [Category("Read")]
        public void New_PopulatesFeedTables_FromZipOfGTFSFiles()
        {
            var fi = new FileInfo(Path.Combine(_baseDirectory, "Data", "GTFS.zip"));

            var gtfs = new GTFS(fi.FullName);

            Assert.NotNull(gtfs);
            Assert.NotNull(gtfs.FeedTables);

            AssertSpecGTFS(gtfs);
        }

        [Test]
        [Category("Write")]
        public void Save_WithCustomTables_WritesXsd()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Data", "Save"));

            try
            {
                //create the GTFS with custom tables and save it out to the directory
                var gtfs = CreateGTFSWithCustomTables();
                gtfs.Save(di.FullName);

                //assert that the XSD was written
                string xsdPath = Path.Combine(di.FullName, GTFS.GTFSOptionalSchemaName);
                Assert.True(File.Exists(xsdPath));

                //and validate the expected table structure
                var newDataSet = new DataSet();
                newDataSet.ReadXmlSchema(xsdPath);

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
        [Category("Read")]
        [Category("Write")]
        public void GTFS_Can_RoundTrip()
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
                var table1 = secondGtfs.FeedTables.Tables["test1.csv"];
                var table2 = secondGtfs.FeedTables.Tables["test2.txt"];

                AssertCustomTables(table1, table2, schemaOnly: false);
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
            gtfs.FeedTables.Tables.Add(table1);
            gtfs.FeedTables.Tables.Add(table2);

            return gtfs;
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
            Assert.AreEqual(13, gtfs.FeedTables.Tables.Count);

            AssertAgency(gtfs);
            AssertCalendar(gtfs);
            AssertCalendarDates(gtfs);
            AssertFareAttributes(gtfs);
            AssertFareRules(gtfs);
            AssertFeedInfo(gtfs);
            AssertRoutes(gtfs);
            AssertShapes(gtfs);
            AssertStops(gtfs);
            AssertStopTimes(gtfs);
            AssertTrips(gtfs);
        }

        private void AssertAgency(GTFS gtfs)
        {
            var table = gtfs.FeedTables._agency_txt;

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

        readonly string[] knownServices = { "1", "2", "3" };

        private void AssertCalendar(GTFS gtfs)
        {
            var table = gtfs.FeedTables._calendar_txt;

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
            var table = gtfs.FeedTables._calendar_dates_txt;

            Assert.NotNull(table);
            Assert.AreEqual(3, table.Count);
        }

        private void AssertFareAttributes(GTFS gtfs)
        {
            var table = gtfs.FeedTables._fare_attributes_txt;

            Assert.NotNull(table);
            Assert.AreEqual(1, table.Count);

            var row = table[0];

            Assert.AreEqual("1", row.fare_id);
            Assert.AreEqual(1.25, row.price);
            Assert.AreEqual("USD", row.currency_type);
        }

        private void AssertFareRules(GTFS gtfs)
        {
            var table = gtfs.FeedTables._fare_rules_txt;

            Assert.NotNull(table);
            Assert.AreEqual(3, table.Count);

            foreach(var row in table)
            {
                Assert.AreEqual("1", row.fare_id);
            }
        }

        private void AssertFeedInfo(GTFS gtfs)
        {
            var table = gtfs.FeedTables._feed_info_txt;

            Assert.NotNull(table);
            Assert.AreEqual(1, table.Count);

            var row = table[0];

            Assert.AreEqual("Test", row.feed_publisher_name);
            Assert.AreEqual("http://example.com", row.feed_publisher_url);
            Assert.AreEqual("en", row.feed_lang);
        }

        private void AssertRoutes(GTFS gtfs)
        {
            var table = gtfs.FeedTables._routes_txt;

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

        private void AssertShapes(GTFS gtfs)
        {
            var table = gtfs.FeedTables._shapes_txt;

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
            var table = gtfs.FeedTables._stops_txt;

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
            var table = gtfs.FeedTables._stop_times_txt;

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
            var table = gtfs.FeedTables._trips_txt;

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
