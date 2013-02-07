using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkyPhotoSharing
{
    class Photo : INotifyPropertyChanged
    {
        public static readonly string[] SUPPORT_EXT = { ".jpg", ".jpeg", ".bmp", ".png", ".tiff", ".gif" };
        private const int NO_DATA = -1;
        private const double DEFAULT_SCALE = 1.0;
        private const double DEFAULT_ROTATE = 0.0;

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected Photo(Stream stm, string path, Enlister own)
        {
            Path = path;
            FileName = System.IO.Path.GetFileName(Path);
            Owner = own;
            Image = ReadBitmapFromStream(stm);
            BackGroundTop = Colors.Silver;
            BackGroundCenter = Colors.Silver;
            BackGroundBottom = Colors.Silver;
            BlendBackgroundColor();
            _viewPosition = new Point(Image.Width / 2, 0);
            _scale = DEFAULT_SCALE;
            CenterX = Image.PixelWidth / 2;
            CenterY = Image.PixelHeight / 2;
            _rotate = DEFAULT_ROTATE;
            _isSelected = false;
            log.Debug("Create new photo. File:" + FileName + " Size:" + Image.StreamSource.Length);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        
        public Enlister Owner { get; protected set; }
        public string Path { get; protected set; }
        public string FileName { get; protected set;  }
        public BitmapImage Image { get; protected set; }
        public DateTime CreateTime { get; protected set; }
        public DateTime UpdateTime { get; protected set; }
        public DateTime AccessTime { get; protected set; }
        public Color Color { get { return Owner.Color; } }
        public bool CanSave { get; protected set; }
        public Color BackGroundTop { get; protected set; }
        public Color BackGroundCenter { get; protected set; }
        public Color BackGroundBottom { get; protected set; }
        public double CenterX { get; protected set; }
        public double CenterY { get; protected set; }
        private Point _viewPosition;
        public Point ViewPosition 
        {
            get { return _viewPosition; }
            set
            {
                _viewPosition = value;
                OnPropertyChanged("ViewPosition");
            }
        }
        private bool _isSelected;
        public bool IsSelected 
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }
        private double _scale;
        public double Scale
        { 
            get { return _scale; }
            set 
            {
                _scale = value;
                OnPropertyChanged("Scale");
            } 
        }
        private double _rotate;
        public double Rotate
        {
            get {return _rotate;}
            set 
            {
                _rotate = value;
                OnPropertyChanged("Rotate");
            }
        }

        public static Photo Create(string path, Enlister own)
        {
            Photo p = null;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    p = new Photo(fs, path, own);
                    log.Debug(p.Owner.Handle + "'s " + p.Path + " is " + (p.Image != null ? "exist." : "missing."));
                    log.Debug("Bitmap size is x:" + p.Image.PixelWidth + " y:" + p.Image.PixelHeight);
                }
                catch (Exception ex) {
                    log.Error("Local file '" + path + "' can't open.", ex);
                    throw ex;
                }
            }
            p.CreateTime = File.GetCreationTime(path);
            p.UpdateTime = File.GetLastWriteTime(path);
            p.AccessTime = File.GetLastAccessTime(path);
            p.CanSave = true;
            if (path.StartsWith(Configuration.Instance.SaveFolder)) p.CanSave = false;
            return p;
        }

        public static Photo Create(SkypeCargo data, Enlister own)
        {
            Photo p = new Photo(data.GetData(), data.FileName, own);
            p.CreateTime = data.CreateTime;
            p.UpdateTime = data.UpdateTime;
            p.AccessTime = data.AccessTime;
            p.CanSave = true;
            return p;
        }

        public static bool IsCoinsidable(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            return SUPPORT_EXT.Contains(ext);
        }

        public bool IsSame(Photo p)
        {
            if (!FileName.Equals(p.FileName)) return false;
            if (!CreateTime.Equals(p.CreateTime)) return false;
            if (!UpdateTime.Equals(p.UpdateTime)) return false;
            if (Image.StreamSource.Length != p.Image.StreamSource.Length) return false;
            return true;
        }

        public bool IsSame(SkypeMessageUniqueFile f)
        {
            if (!FileName.Equals(f.FileName)) return false;
            if (!CreateTime.Equals(f.CreateTime)) return false;
            if (!UpdateTime.Equals(f.UpdateTime)) return false;
            if (Image.StreamSource.Length != f.Size) return false;
            return true;
        }


        public void Save()
        {
            if (!CanSave) return;
            string p = ConfigureUniqueName(Configuration.Instance.SaveFolder + System.IO.Path.DirectorySeparatorChar + FileName);
            WriteBinary(p);
            AdjustTimestamps(p);
            CanSave = false;
        }

        public void Close()
        {
            Image.StreamSource.Close();
        }

        protected static BitmapImage ReadBitmapFromStream(Stream s)
        {
            BitmapImage r = new BitmapImage();
            r = InitializeBitmap(r, CreateCache(s));
            return r;
        }

        protected static MemoryStream CreateCache(Stream s)
        {
            if (s is MemoryStream) 
            {
                s.Seek(0, SeekOrigin.Begin);
                return s as MemoryStream;
            }
            MemoryStream m = new MemoryStream();
            s.Seek(0, SeekOrigin.Begin);
            using (s)
            {
                try
                {
                    int d = NO_DATA;
                    while ((d = s.ReadByte()) != NO_DATA)
                    {
                        m.WriteByte((byte)d);
                    }
                }
                catch (Exception e)
                {
                    log.Error("Can't create cache.", e);
                    throw e;
                }
            }
            m.Seek(0, SeekOrigin.Begin);
            return m;
        }

        protected static BitmapImage InitializeBitmap(BitmapImage b, MemoryStream s)
        {
            s.Seek(0, SeekOrigin.Begin);
            b.BeginInit();
            b.CacheOption = BitmapCacheOption.None;
            b.StreamSource = s;
            b.EndInit();
            b.Freeze();
            return b;
        }

        private string ConfigureUniqueName(string path)
        {
            if (!File.Exists(path)) return path;
            var d = System.IO.Path.GetDirectoryName(path);
            var f = System.IO.Path.GetFileNameWithoutExtension(path);
            var x = System.IO.Path.GetExtension(path);
            var s = new Byte[4];
            (new Random()).NextBytes(s);
            var t = String.Format(".{0:X2}{0:X2}{0:X2}{0:X2}", s);
            return d + System.IO.Path.PathSeparator + f + t + x;
        }

        private void WriteBinary(string p)
        {
            Stream r = Image.StreamSource;
            r.Seek(0, SeekOrigin.Begin);
            using (FileStream w = File.Create(p))
            {
                try
                {
                    int d = NO_DATA;
                    while ((d = r.ReadByte()) != NO_DATA)
                    {
                        w.WriteByte((byte)d);
                    }
                }
                catch (Exception e)
                {
                    log.Error("ImageFile '" + p + "' can't save.", e);
                }
            }
            r.Seek(0, SeekOrigin.Begin);
        }

        private void AdjustTimestamps(string p)
        {
            File.SetCreationTime(p, CreateTime);
            File.SetLastWriteTime(p, UpdateTime);
            File.SetLastAccessTime(p, AccessTime);
        }
        private void BlendBackgroundColor()
        {
            const byte MINIMUM_ALPHA = 64;
            int sh = Image.PixelHeight * 4;
            int sw = Image.PixelWidth * 4;
            byte[] ri = new byte[sh * sw];
            byte[] le = new byte[sh * sw];
            var s = ConvertToBgra32(Image);
            s.CopyPixels(new Int32Rect(0, 0, 1, Image.PixelHeight), le, sw, 0);
            s.CopyPixels(new Int32Rect((Image.PixelWidth - 1), 0, 1, Image.PixelHeight), ri, sw, 0);
            var rl = new LinkedList<byte>();
            var gl = new LinkedList<byte>();
            var bl = new LinkedList<byte>();

            for (var i = 0; i < sh; i += 4)
            {
                if (le[i + 3] > MINIMUM_ALPHA)
                {
                    bl.AddLast(le[i]);
                    gl.AddLast(le[i + 1]);
                    rl.AddLast(le[i + 2]);
                }
                if (ri[i + 3] > MINIMUM_ALPHA)
                {
                    bl.AddLast(ri[i]);
                    gl.AddLast(ri[i + 1]);
                    rl.AddLast(ri[i + 2]);
                }
            }

            byte rmin = rl.First.Value;
            byte ravg = rl.First.Value;
            byte rmax = rl.First.Value;
            foreach (var r in rl)
            {
                rmin = r < rmin ? r : rmin;
                rmax = rmax < r ? r : rmax;
                ravg = (byte)((ravg + r) / 2);
            }
            byte gmin = gl.First.Value;
            byte gavg = gl.First.Value;
            byte gmax = gl.First.Value;
            foreach (var g in gl)
            {
                gmin = g < gmin ? g : gmin;
                gmax = gmax < g ? g : gmax;
                gavg = (byte)((gavg + g) / 2);
            }
            byte bmin = bl.First.Value;
            byte bavg = bl.First.Value;
            byte bmax = bl.First.Value;
            foreach (var b in bl)
            {
                bmin = b < bmin ? b : bmin;
                bmax = bmax < b ? b : rmax;
                bavg = (byte)((bavg + b) / 2);
            }
            
            BackGroundTop = CreateNewColor(rmin, gmin, bmin);
            BackGroundCenter = CreateNewColor(ravg, gavg, bavg);
            BackGroundBottom = CreateNewColor(rmax, gmax, bmax);
        }

        private BitmapSource ConvertToBgra32(BitmapSource src)
        {
            if (src.Format == PixelFormats.Bgra32) return src;
            var s = new FormatConvertedBitmap(
                    src,
                    PixelFormats.Bgra32,
                    null,
                    0);
            s.Freeze();
            return s;
        }

        private static System.Windows.Media.Color CreateNewColor(byte r, byte g, byte b)
        {
            const byte OPAQUE = 255;
            var c = new Color();
            c.A = OPAQUE;
            c.R = r;
            c.G = g;
            c.B = b;
            return c;
        }
    }
}
