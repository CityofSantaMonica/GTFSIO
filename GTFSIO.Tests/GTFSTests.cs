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
        public void New_ReadsDirectory_IntoFeedTables()
        {
            var di = new DirectoryInfo(_directory);

            File.WriteAllText(Path.Combine(di.FullName, "test1.csv"), "field1,field2");
            File.WriteAllText(Path.Combine(di.FullName, "test2.txt"), "fieldA,fieldB");

            GTFS gtfs = new GTFS(_directory);

            Assert.GreaterOrEqual(gtfs.FeedTables.Tables.Count, 2);
            Assert.NotNull(gtfs.FeedTables.Tables["test1.csv"]);
            Assert.NotNull(gtfs.FeedTables.Tables["test2.txt"]);
        }
    }
}
