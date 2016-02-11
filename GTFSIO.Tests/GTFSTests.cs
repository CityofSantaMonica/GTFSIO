using NUnit.Framework;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GTFSIO.Tests
{
    [TestFixture]
    public class GTFSTests
    {
        static readonly string _directory = @"c:\test\gtfs";

        [SetUp]
        public void SetUp()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);

            var di = Directory.CreateDirectory(_directory);
            Console.WriteLine(di.FullName);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_directory, true);
            Assert.False(Directory.Exists(_directory));
        }

        [Test]
        public void New_Initializes_FeedTables()
        {
            var gtfs = new GTFS();
            Assert.NotNull(gtfs.FeedTables);
        }

        [Test]
        public void New_Constructs_FromLocalDirectory()
        {
            var di = new DirectoryInfo(_directory);

            GTFS gtfs = null;

            Assert.DoesNotThrow(() => gtfs = new GTFS(di.FullName));
            Assert.NotNull(gtfs);
            Assert.NotNull(gtfs.FeedTables);
        }

        [Test]
        public void New_Constructs_FromLocalZip()
        {
            var di = new DirectoryInfo(_directory);
            var fi = new FileInfo(Path.Combine(di.FullName, "data.zip"));

            using (var za = new ZipArchive(File.OpenWrite(fi.FullName), ZipArchiveMode.Create)) { }

            GTFS gtfs = null;

            Assert.DoesNotThrow(() => gtfs = new GTFS(fi.FullName));
            Assert.NotNull(gtfs);
            Assert.NotNull(gtfs.FeedTables);
        }

        [TestCase("")]
        [TestCase("bla bla 1-2/3?4")]
        [TestCase("C:\nope")]
        [TestCase("C:\nope\not\there.zip")]
        public void New_Constructs_WithNonsense(string nonsense)
        {
            GTFS gtfs = null;

            Assert.DoesNotThrow(() => gtfs = new GTFS(nonsense));
            Assert.NotNull(gtfs);
            Assert.NotNull(gtfs.FeedTables);
        }

        [Test]
        public void New_PopulatesFeedTables_FromDirectoryOfFiles()
        {
            var di = new DirectoryInfo(_directory);

            var data = String.Format("{{0}}{0}{{1}}", Environment.NewLine);

            File.WriteAllText(Path.Combine(di.FullName, "test1.csv"), String.Format(data, "field1,field2", "value1,value2"));
            File.WriteAllText(Path.Combine(di.FullName, "test2.txt"), String.Format(data, "fieldA,fieldB", "valueA,valueB"));

            GTFS gtfs = new GTFS(_directory);

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
    }
}
