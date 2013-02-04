using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPhotoSharing
{
    class SkypeMessageUniqueFile : ISkypePostcardMessage
    {
        public string FileName { get; set; }
        public long Size { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }

        public static SkypeMessageUniqueFile CreateFrom(Photo photo)
        {
            var r = new SkypeMessageUniqueFile();
            r.FileName = photo.FileName;
            r.Size = photo.Image.StreamSource.Length;
            r.CreateTime = photo.CreateTime;
            r.UpdateTime = photo.UpdateTime;
            return r;
        }
    }
}
