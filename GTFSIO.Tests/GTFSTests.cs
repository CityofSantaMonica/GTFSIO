using NUnit.Framework;
using System;
using System.IO;

namespace GTFSIO.Tests
{
    [TestFixture]
    public class GTFSTests
    {
        //the directory where this project lives
        static readonly string _baseDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;

        [Test]
        [Category("Read")]
        public void New_Initializes_FeedTables()
        {
            var gtfs = new GTFS();
            Assert.NotNull(gtfs.FeedTables);
        }

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
        public void New_PopulatesFeedTables_FromDirectoryOfGTFSFiles()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "GTFS"));

            GTFS gtfs = new GTFS(di.FullName);

            Assert.NotNull(gtfs);
            Assert.NotNull(gtfs.FeedTables);
            AssertGTFSData(gtfs);
        }

        [Test]
        [Category("Read")]
        public void New_PopulatesFeedTables_FromZipOfGTFSFiles()
        {
            var fi = new FileInfo(Path.Combine(_baseDirectory, "GTFS.zip"));

            GTFS gtfs = new GTFS(fi.FullName);

            Assert.NotNull(gtfs);
            Assert.NotNull(gtfs.FeedTables);
            AssertGTFSData(gtfs);
        }

        //Asserts the existence of data found in Data/GTFS/ and Data/GTFS.zip
        private void AssertGTFSData(GTFS gtfs)
        {
            throw new NotImplementedException();
        }

        [Test]
        [Category("Read")]
        public void New_PopulatesFeedTables_FromDirectoryOfCustomFiles()
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, "Custom"));

            GTFS gtfs = new GTFS(di.FullName);

            Assert.NotNull(gtfs);
            Assert.NotNull(gtfs.FeedTables);
            AssertCustomData(gtfs);
        }

        [Test]
        [Category("Read")]
        public void New_PopulatesFeedTables_FromZipOfCustomFiles()
        {
            var fi = new FileInfo(Path.Combine(_baseDirectory, "Custom.zip"));

            GTFS gtfs = new GTFS(fi.FullName);

            AssertCustomData(gtfs);
        }

        //Asserts the existence of data found in Data/Custom/ and Data/Custom.zip
        private void AssertCustomData(GTFS gtfs)
        {
            var table = gtfs.FeedTables.Tables["test1.csv"];
            Assert.NotNull(table);
            Assert.NotNull(table.Columns["field1"]);
            Assert.NotNull(table.Columns["field2"]);
            Assert.AreEqual(1, table.Rows.Count);
            Assert.AreEqual("value1", table.Rows[0]["field1"]);
            Assert.AreEqual("value2", table.Rows[0]["field2"]);

            table = null;

            table = gtfs.FeedTables.Tables["test2.txt"];
            Assert.NotNull(table);
            Assert.NotNull(table.Columns["fieldA"]);
            Assert.NotNull(table.Columns["fieldB"]);
            Assert.AreEqual(1, table.Rows.Count);
            Assert.AreEqual("valueA", table.Rows[0]["fieldA"]);
            Assert.AreEqual("valueB", table.Rows[0]["fieldB"]);
        }

        [Test]
        [Category("Write")]
        public void Save_WithCustomTables_WritesXsd()
        {
            var di = new DirectoryInfo(_baseDirectory);

            var data = String.Format("{{0}}{0}{{1}}", Environment.NewLine);

            GTFS firstGtfs = new GTFS();

            System.Data.DataTable newTable1 = new System.Data.DataTable("test1.csv");
            newTable1.Columns.Add(new System.Data.DataColumn("field1"));
            newTable1.Columns.Add(new System.Data.DataColumn("field2"));
            newTable1.Rows.Add("value1", "value2");
            firstGtfs.FeedTables.Tables.Add(newTable1);

            System.Data.DataTable newTable2 = new System.Data.DataTable("test2.txt");
            newTable2.Columns.Add(new System.Data.DataColumn("fieldA"));
            newTable2.Columns.Add(new System.Data.DataColumn("fieldB"));
            newTable2.Rows.Add("valueA", "valueB");
            firstGtfs.FeedTables.Tables.Add(newTable2);

            firstGtfs.Save(_baseDirectory);

            System.Data.DataSet newDataSet = new System.Data.DataSet();
            newDataSet.ReadXmlSchema(Path.Combine(_baseDirectory, GTFS.GTFSOptionalSchemaName));
            var table = newDataSet.Tables["test1.csv"];
            Assert.NotNull(table);
            Assert.NotNull(table.Columns["field1"]);
            Assert.NotNull(table.Columns["field2"]);

            table = null;

            table = newDataSet.Tables["test2.txt"];
            Assert.NotNull(table);
            Assert.NotNull(table.Columns["fieldA"]);
            Assert.NotNull(table.Columns["fieldB"]);

        }

        [Test]
        [Category("Read")]
        [Category("Write")]
        public void New_PopulatesFeedTables_RoundTrip()
        {
            var di = new DirectoryInfo(_baseDirectory);

            var data = String.Format("{{0}}{0}{{1}}", Environment.NewLine);

            GTFS firstGtfs = new GTFS();

            System.Data.DataTable newTable1 = new System.Data.DataTable("test1.csv");
            newTable1.Columns.Add(new System.Data.DataColumn("field1"));
            newTable1.Columns.Add(new System.Data.DataColumn("field2"));
            newTable1.Rows.Add("value1", "value2");
            firstGtfs.FeedTables.Tables.Add(newTable1);

            System.Data.DataTable newTable2 = new System.Data.DataTable("test2.txt");
            newTable2.Columns.Add(new System.Data.DataColumn("fieldA"));
            newTable2.Columns.Add(new System.Data.DataColumn("fieldB"));
            newTable2.Rows.Add("valueA", "valueB");
            firstGtfs.FeedTables.Tables.Add(newTable2);

            firstGtfs.Save(_baseDirectory);

            GTFS secondGtfs = new GTFS(_baseDirectory);

            var table = secondGtfs.FeedTables.Tables["test1.csv"];
            Assert.NotNull(table);
            Assert.NotNull(table.Columns["field1"]);
            Assert.NotNull(table.Columns["field2"]);
            Assert.AreEqual(1, table.Rows.Count);
            Assert.AreEqual("value1", table.Rows[0]["field1"]);
            Assert.AreEqual("value2", table.Rows[0]["field2"]);

            table = null;

            table = secondGtfs.FeedTables.Tables["test2.txt"];
            Assert.NotNull(table);
            Assert.NotNull(table.Columns["fieldA"]);
            Assert.NotNull(table.Columns["fieldB"]);
            Assert.AreEqual(1, table.Rows.Count);
            Assert.AreEqual("valueA", table.Rows[0]["fieldA"]);
            Assert.AreEqual("valueB", table.Rows[0]["fieldB"]);
        }
    }
}
