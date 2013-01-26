using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SKYPE4COMLib;
using System.Windows.Media;

namespace SkyPhotoSharing
{
    class Enlister
    {
        public string Handle { get; protected set; }
        public string Name { get; protected set; }
        public Color Color { get; protected set; }
        public SkypeDelivery Delivery { get; protected set; }

        public bool IsConnecting
        {
            get { return Delivery != null ? true : false; }
        }

        private Brush _brush;
        public Brush Brush { get { return _brush; } }

        public SkypeDelivery.State ConnectionState { get { return Delivery.ConnectionState; } } 

        public Enlister(User skypeUser)
        {
            Handle = skypeUser.Handle;
            Name = GetDisplayName(skypeUser);
            Color = NextColor();
            _brush = new SolidColorBrush(Color);
            Delivery = new SkypeDelivery(this);
        }

        private static Enlister _owner = null;
        public static Enlister Owner
        {
            get
            {
                if (_owner != null) return _owner;
                _owner = new Enlister(SkypeConnection.Instance.Skype.CurrentUser);
                _owner.Color = Colors.Blue;
                return _owner;
            }
        }

        public void Close()
        {
            if (!IsConnecting) return;
            Delivery.PostChatMessage(string.Format(Properties.Resources.CHAT_CLOSE, Owner.Name));
            Delivery.Disconnect();
            Delivery = null;
        }

        public void SendPhoto(Photo photo)
        {
            Delivery.Send(photo);
            Delivery.PostChatMessage(string.Format(Properties.Resources.CHAT_SENT_PHOTO, Owner.Name, photo.FileName));
        }

        public void PostPacket(string packet)
        {
            Delivery.PostPacket(packet);
        }

        public static string GetDisplayName(User skypeUser)
        {
            if (!string.IsNullOrEmpty(skypeUser.DisplayName)) return skypeUser.DisplayName;
            else if (!string.IsNullOrEmpty(skypeUser.FullName)) return skypeUser.FullName;
            else return skypeUser.Handle;
        }

        private static readonly Queue<Color> COLOR_TABLE = new Queue<Color>(new[] { Colors.Black, Colors.Red, Colors.Green, Colors.Cyan, Colors.Magenta, Colors.Yellow, Colors.DodgerBlue, Colors.MediumSpringGreen, Colors.Crimson, Colors.Aquamarine, Colors.Purple, Colors.Orange });
        private static Color NextColor()
        {
            Color r = COLOR_TABLE.Dequeue();
            COLOR_TABLE.Enqueue(r);
            return r;
        }
    }
}
