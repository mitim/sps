using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkyPhotoSharing
{
    class SkypeCargo
    {
        public string FileName { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public DateTime AccessTime { get; set; }
        public long Size { get; set; }
        public string  Data { get; set; }

        public SkypeCargo(Photo photo)
        {
            FileName = photo.FileName;
            CreateTime = photo.CreateTime;
            UpdateTime = photo.UpdateTime;
            AccessTime = photo.AccessTime;
            Size = photo.Image.StreamSource.Length;
            SetData((MemoryStream)photo.Image.StreamSource);
        }

        public SkypeCargo()
        { 
        }

        public static SkypeCargo Parse(string json)
        {
            return JsonConvert.DeserializeObject<SkypeCargo>(json);
        }

        public string ConvertToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        private void SetData(MemoryStream s)
        {
            byte[] m = s.GetBuffer();
            byte[] b = new byte[(int)s.Length];
            Buffer.BlockCopy(m, 0, b, 0, (int)s.Length); 
            Data = Convert.ToBase64String(b, Base64FormattingOptions.None);
        }

        public MemoryStream GetData()
        {
            var m = new MemoryStream(Convert.FromBase64String(Data));
            while (m.Length < Size) Thread.Sleep(100);
            m.Seek(0, SeekOrigin.Begin);
            return m;
        }
    }
}
