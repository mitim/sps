using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SkyPhotoSharing
{
    class Configuration
    {
        private static Configuration _instance = Configuration.Create();
        public static Configuration Instance { get{return _instance;} }

        private Configuration()
        {
        }

        public Rect WindowState { get { return new Rect(WindowLeft, WindowTop, WindowWidth, WindowHeight); } }
        public string SaveFolder { get; set; }
        public bool AutoSave { get; set; }
        public bool AutoSelect { get; set; }
        public bool ReflectOriginalTimes { get; set; }
        public double WindowLeft { get; set; }
        public double WindowTop { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }

        private static Configuration Create()
        {
            return LoadJson();
        }

        private static string RegistoryJson { get { return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + System.IO.Path.DirectorySeparatorChar +  "SkyPhotoSharing" + System.IO.Path.DirectorySeparatorChar + "Configuration.json"; } }

        private static Configuration LoadJson()
        {
            if (!File.Exists(RegistoryJson))
            {
                return initializeConfig();
            }
            string t = File.ReadAllText(RegistoryJson);
            return JsonConvert.DeserializeObject<Configuration>(t);
        }

        private static Configuration initializeConfig()
        {
            Configuration c = new Configuration();
            c.WindowLeft = 50.0;
            c.WindowTop = 50.0;
            c.WindowWidth = 960.0;
            c.WindowHeight = 540.0;
            c.SaveFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            c.AutoSave = false;
            c.SaveJson();
            return c;
        }

        public void Save()
        {
            SaveJson();
        }

        private void SaveJson()
        {
            string d = System.IO.Path.GetDirectoryName(RegistoryJson);
            if(!Directory.Exists(d)) Directory.CreateDirectory(d);
            File.WriteAllText(RegistoryJson, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
