using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;

namespace SkyPhotoSharing
{

    class PhotoList : INotifyPropertyChanged
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private BindingList<Photo> _photos = new BindingList<Photo>();

        private static PhotoList _instance;

        public PhotoList()
        {
            PropertyChanged += (sender, e) => { };
            _instance = this;
            log.Debug("PhotoList create.");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public IBindingList Photos
        {
            get
            {
                return _photos;
            }
        }

        public Configuration Attribute
        {
            get
            {
                return Configuration.Instance;
            }
        }

        private bool _isControlEnable;
        public bool IsControlEnable
        {
            get 
            {
                return _isControlEnable; 
            }
            protected set 
            {
                _isControlEnable = (0 < Photos.Count);
                OnPropertyChanged("IsControlEnable");
            }
        }

        private bool _isSaveEnable;
        public bool IsSaveEnable
        {
            get 
            {
                return _isSaveEnable; 
            } 
            protected set
            {
                var p = CurrentPhoto;
                if (p == null)
                {
                    _isSaveEnable = false;
                }
                else
                {
                    _isSaveEnable = (IsControlEnable && CurrentPhoto.CanSave);
                }
                OnPropertyChanged("IsSaveEnable");
            }
        }

        public Photo CurrentPhoto
        {
            get
            {
                foreach (var p in _photos)
                {
                    if (p.IsSelected) return p; 
                }
                return null;
            }
        }

        public bool HasSameItem(Photo item)
        {
            foreach (var p in _photos)
            {
                if (p.IsSame(item)) return true;
            }
            return false;
        }

        public Photo GetSameItem(SkypeMessageUniqueFile file)
        {
            foreach (var p in _photos)
            {
                if (p.IsSame(file)) return p;
            }
            return null;
        }

        public void Add(Photo item)
        {
            if (item == null) return;
            if (HasSameItem(item)) return;
            _photos.Add(item);
            UpdateControlFlags();
            AidAutoSave(item);
        }

        public void Remove(Photo item)
        {
            if (item == null) return;
            item.Close();
            _photos.Remove(item);
        }

        public void Close()
        {
            foreach(var p in _photos)
            {
                p.Close();
            }
        }

        public void RemoveAt(int idx)
        {
            if (idx < 0) return;
            if (idx >= _photos.Count) return;
            _photos[idx].Close();
            _photos.RemoveAt(idx);
        }

        public void AddNewLocal(string p)
        {
            if (string.IsNullOrEmpty(p)) return;
            var v = Photo.Create(p, Enlisters.Instance.Owner);
            Add(v);
            Enlisters.Instance.BroadcastPhoto(v);
        }

        public void UpdateControlFlags()
        {
            IsControlEnable = true;
            IsSaveEnable = true;
        }


        public static void AddFromOnline(Photo photo)
        {
            _instance.Add(photo);
        }

        public Photo Last
        {
            get { return _photos.Last(); }
        }

        private void AidAutoSave(Photo item)
        {
            if (!Configuration.Instance.AutoSave) return;
            item.Save();
        }
    }
}
