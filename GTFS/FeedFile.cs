using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFS
{
    public  class FeedFile
    {
        public String Filename { get; set; }
        public Stream stream { get; set; }
        ~FeedFile()
        {
            stream.Close();
        }
    }
}
