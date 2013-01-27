using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SkyPhotoSharing
{
    class SkypePostcard
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const int COMMAND_LENGTH = 4;

        public const string RAISE_SELECT_FILE = "SLCT";

        public string Command { get; set; }
        public ISkypePostcardMessage Message { get; set; }

        public string Serialize()
        {
            string r = Command + JsonConvert.SerializeObject(Message);
            log.Debug(r);
            return r;
        }

        public static SkypePostcard Deserialize(string card)
        {
            var p = new SkypePostcard();
            p.Command = card.Substring(0, COMMAND_LENGTH);
            switch (p.Command)
            {
                case RAISE_SELECT_FILE:
                    {
                        p.Message = JsonConvert.DeserializeObject<SkypeMessageUniqueFile>(card.Substring(COMMAND_LENGTH));
                        break;
                    }
                default:
                    {
                        p.Message = new SkypeMessageEmpty();
                        break;
                    }
            }
            return p;
        }

        public static SkypePostcard CreateRaiseSelectFileCard(Photo photo)
        {
            var r = new SkypePostcard();
            r.Command = RAISE_SELECT_FILE;
            r.Message = SkypeMessageUniqueFile.CreateFrom(photo);
            return r;
        }
    }
}
