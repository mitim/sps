using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SKYPE4COMLib;

namespace SkyPhotoSharing
{
    class SelectableUsers : INotifyPropertyChanged
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IList<User> _list;
        public IList<User> List 
        { 
            get{return _list;} 
            set
            {
                _list = value;
                OnPropertyChanged("List");
            } 
        }

        public SelectableUsers() 
        {
            Reflesh();
            log.Debug("SelectableUsers create. size:" + List.Count);
        }

        public void Reflesh() {
            List = new List<User>();
            var l = SkypeConnection.Instance.OnlineUsers;
            foreach (var i in l)
            {
                if (!Enlisters.Instance.IsExistEnlister(i.Handle)) List.Add(i);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
