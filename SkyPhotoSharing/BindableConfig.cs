using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPhotoSharing
{
    class BindableConfig : INotifyPropertyChanged
    {
        public BindableConfig()
        {
            _attribute = Configuration.Instance;
            PropertyChanged += (sender, e) => { };
        }

        private Configuration _attribute;
        public Configuration Attribute 
        {
            get
            { return _attribute; }
            set
            {
                _attribute = value;
                OnPropertyChanged("Attribute");
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
