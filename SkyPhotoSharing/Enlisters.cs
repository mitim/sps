using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using SKYPE4COMLib;
using System.Threading.Tasks;

namespace SkyPhotoSharing
{
    class Enlisters
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Enlister Owner { protected set; get; }
        private BindingList<Enlister> _enlisters = new BindingList<Enlister>();
        public BindingList<Enlister> ConnectedList { get { return _enlisters; } }

        private static Enlisters _instance = new Enlisters();
        public static Enlisters Instance{ get{return _instance;}}
        
        public bool IsExistEnlister(string handle)
        {
            return SearchForHandle(handle) != null ? true : false;
        }

        public Enlister SearchForHandle(string handle)
        {
            foreach (Enlister e in _enlisters)
            {
                if (e.Handle == handle) return e;
            }
            return null;
        }

        protected Enlisters() 
        {
            Owner = Enlister.Owner;
            log.Debug(Owner.Handle + ":" +Owner.Name + " Color:" + Owner.Color.ToString());
        }

        public void ConnectTo(User usr)
        {
            var re = SearchForHandle(usr.Handle);
            if (re!= null) return;
            log.Debug("Connecting to user. handle:" + usr.Handle);
            SkypeConnection.Instance.PostChatMessage(usr.Handle,
                string.Format(Properties.Resources.CHAT_OPEN, Enlister.GetDisplayName(usr)));
            SkypeConnection.Instance.ConnectTo(usr);
        }

        public void RaiseConnected(User usr)
        {
            var re = SearchForHandle(usr.Handle);
            if (re != null) return;
            var ne = new Enlister(usr);
            ne.Delivery.EventRaiseDisconnect += RaiseDisconnect;
            _enlisters.Add(ne);
            log.Debug("Connected. handle:" + ne.Handle);
        }

        public void BroadcastPhoto(Photo photo)
        {
            Parallel.ForEach(_enlisters, current =>
            {
                current.SendPhoto(photo);
            });
        }

        public void Close()
        {
            foreach (var e in ConnectedList)
            {
                e.Close();
            }
        }

        private void RaiseDisconnect(Enlister en)
        {
            ConnectedList.Remove(en);
        }
    }
}
