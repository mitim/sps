using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPhotoSharing
{
    class SharingEnlisters : INotifyPropertyChanged
    {

        public SharingEnlisters()
        {
        }

        public BindingList<Enlister> List { get { return Enlisters.Instance.ConnectedList; } }
        
        public Enlister Owner { get {return Enlisters.Instance.Owner;} }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public void Close()
        {
            Enlisters.Instance.Close();
        }
    }
}
