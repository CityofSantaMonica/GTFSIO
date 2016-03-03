using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace GTFSIO
{
    /// <summary>
    /// A map of GTFS file names to the corresponding file streams.
    /// </summary>
    /// <remarks>
    /// <see cref="FeedFiles"/> implements <see cref="IDisposable"/> since it maintains a collection of open <see cref="Stream"/> objects.
    /// Instances should be wrapped in the <code>using() { }</code> construct or cleaned up with an explicit call to <code>Dispose</code>.
    /// </remarks>
    public class FeedFiles : Dictionary<String, Stream>, IDisposable
    {
        private readonly ZipArchive _zipArchive;

        public FeedFiles() { }

        /// <summary>
        /// Create a mapping for files in the provided <see cref="ZipArchive"/>.
        /// </summary>
        public FeedFiles(ZipArchive zipArchive)
        {
            _zipArchive = zipArchive;

            foreach (var entry in zipArchive.Entries)
            {
                Add(entry.Name, entry.Open());
            }
        }

        /// <summary>
        /// Create a mapping for files in the provided <see cref="DirectoryInfo"/>.
        /// </summary>
        public FeedFiles(DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles())
            {
                Add(file.Name, file.Open(FileMode.Open));
            }
        }

        /// <summary>
        /// Cleanup this mapping's <see cref="Stream"/> objects.
        /// </summary>
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
