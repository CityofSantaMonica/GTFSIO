using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFS
{
    public class FeedFiles : Dictionary<String, Stream>
    {
        public FeedFiles()
        {

        }
        public FeedFiles(ZipArchive zipArchive)
        {
            this.Zip = zipArchive;
            foreach (var entry in zipArchive.Entries)
            {
                this.Add(entry.Name, entry.Open());
            }
        }
        //public FeedFiles(DirectoryInfo directory)
        //{
        //    foreach (var file in directory.GetFiles("*.txt"))
        //    {
        //        this.Add(file.Name, file.Open(FileMode.Open));
        //    }
        //}
        public FeedFiles(DirectoryInfo directory, String pattern = "*.txt")
        {
            foreach (var file in directory.GetFiles(pattern))
            {
                this.Add(file.Name, file.Open(FileMode.Open));
            }
        }
        public ZipArchive Zip { get; set; }
    }
}
