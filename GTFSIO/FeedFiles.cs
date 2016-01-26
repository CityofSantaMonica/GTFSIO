using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace GTFSIO
{
    public class FeedFiles : Dictionary<String, Stream>, IDisposable
    {
        private readonly ZipArchive _zipArchive;

        public FeedFiles() { }

        public FeedFiles(ZipArchive zipArchive)
        {
            _zipArchive = zipArchive;

            foreach (var entry in zipArchive.Entries)
            {
                Add(entry.Name, entry.Open());
            }
        }

        public FeedFiles(DirectoryInfo directory, String pattern = "*.txt")
        {
            foreach (var file in directory.GetFiles(pattern))
            {
                Add(file.Name, file.Open(FileMode.Open));
            }
        }

        public void Dispose()
        {
            foreach (var stream in Values)
            {
                stream.Dispose();
            }

            if (_zipArchive != null)
            {
                _zipArchive.Dispose();
            }
        }
    }
}
