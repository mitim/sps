﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SKYPE4COMLib;
using System.Threading;

namespace SkyPhotoSharing
{
    class SkypeConnection
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Skype _skype = new Skype();
        public Skype Skype { get { return _skype; } }
        private SKYPE4COMLib.Application _skyApp = null;
        protected SKYPE4COMLib.Application SkyApp 
        {
            get {
                if (_skyApp != null) return _skyApp; 
                _skype.ApplicationConnecting += new _ISkypeEvents_ApplicationConnectingEventHandler(OnSkypeApplicationConnecting);
                _skype.ApplicationStreams += new _ISkypeEvents_ApplicationStreamsEventHandler(OnSkypeApplicationStreams);
                _skype.ApplicationReceiving += new _ISkypeEvents_ApplicationReceivingEventHandler(OnSkypeApplicationReceiving);
                _skype.ApplicationDatagram += new _ISkypeEvents_ApplicationDatagramEventHandler(OnApplicationDatagram);
                _skype.Error += new _ISkypeEvents_ErrorEventHandler(OnSkypeError);
                _skype.Attach();
                _skyApp = _skype.Application[App.APPNAME];
                _skyApp.Create();
                return _skyApp;
            }
        }

        private static SkypeConnection _instance = new SkypeConnection();
        public static SkypeConnection Instance { get { return _instance; } }

        public IList<User> OnlineUsers
        {
            get
            {
                IList<User> l = new List<User>();
                foreach (User u in _skype.Friends)
                {
                    if (u.OnlineStatus == TOnlineStatus.olsOffline || u.OnlineStatus == TOnlineStatus.olsSkypeOut || u.OnlineStatus == TOnlineStatus.olsUnknown)
                    {
                        continue;
                    }
                    l.Add(u);
                }
                return l;
            }
        }

        private SkypeConnection()
        {
            log.Debug("Connection create start.");
            if (_skype.Client.IsRunning) return;
            _skype.Client.Start();
        }

        public void WaitSkypeRunning()
        {
            log.Debug("Wait for Skype.");
            while (!_skype.Client.IsRunning)
            {
                Thread.Sleep(100);
            }
            log.Debug("Skype startup.");
        }

        public void ConnectTo(User usr)
        {
            log.Debug("Connecting to new user. :" + usr.Handle);
            int i = SkyApp.Streams.Count;
            SkyApp.Connect(usr.Handle, true);
        }

        public void Disconnect(Enlister partner)
        {
            log.Debug("Disconnect at user. :" + partner.Handle);
            GetConnectedStream(partner.Handle).Disconnect();
        }

        public void WritePacket(string data, Enlister partner)
        {
            log.Debug("WritePacket to user. :" + partner.Handle + " data-length:" + data.Length);
            GetConnectedStream(partner.Handle).Write(data);
        }

        private const string UNKNOWN = "<<Unknown User>>";
        public static string GetViewableName(User user) {
            if (!string.IsNullOrEmpty(user.FullName)) return user.FullName;
            if (!string.IsNullOrEmpty(user.DisplayName)) return user.DisplayName;
            if (!string.IsNullOrEmpty(user.Handle)) return user.Handle;
            return UNKNOWN;
        }

        public void PostChatMessage(string handle, string message)
        {
            _skype.SendMessage(handle, Properties.Resources.CHAT_HEAD + message);
        }

        private ApplicationStream GetConnectedStream(string handle)
        {
            foreach (ApplicationStream s in SkyApp.Streams)
            {
                log.Debug("getConnectedStream target:" + handle + " in stream Handle:" + s.Handle + " Partner:" + s.PartnerHandle);
                if (s.Handle.StartsWith(handle)) return s;
            }
            return null;
        }
        
        #region Skypeイベントハンドラ

        private void OnSkypeApplicationConnecting(IApplication app, IUserCollection users)
        {
            if (log.IsDebugEnabled)
            {
                String v = "";
                foreach (User u in users)
                {
                    v += " [User :" + u.Handle + "]";
                }
                log.Debug(app.Name + " on connecting :" + v);
            }
        }

        private void OnSkypeApplicationStreams(IApplication app, IApplicationStreamCollection streams)
        {
            if (log.IsDebugEnabled)
            {
                String v = "";
                foreach (ApplicationStream s in streams)
                {
                    v += " [stream :" + s.ApplicationName + " " + s.DataLength + "bytes handle:" + s.Handle + " Partner:" + s.PartnerHandle + "]";
                    var u = ParseHandle(s.Handle);
                    if (!Enlisters.Instance.IsExistEnlister(u))
                    {
                        Enlisters.Instance.RaiseConnected(_skype.User[u]);
                    }
                }
                log.Debug(app.Name + " on streams :" + v);
            }
        }

        private void OnSkypeApplicationReceiving(IApplication app, IApplicationStreamCollection streams)
        {
            if (log.IsDebugEnabled)
            {
                String v = "";
                foreach (ApplicationStream s in streams)
                {
                    v += " [stream :" + s.ApplicationName + " " + s.DataLength + "bytes handle:" + s.Handle + " Partner:" + s.PartnerHandle + "]";
                }
                log.Debug(app.Name + " on receiving :" + v);
            }

            foreach (ApplicationStream s in streams)
            {
                if (s.DataLength <= 0) continue;
                var en = Enlisters.Instance.SearchForHandle(ParseHandle(s.Handle));
                en.PostPacket(s.Read());
            }
       }

        private void OnApplicationDatagram(Application app, ApplicationStream stream, string data)
        {
            log.Debug(app.Name + " on datagram. stream:" + stream.Handle + " data:[" + data + "]"); 
        }

        private void OnSkypeError(ICommand command, int code, string description)
        {
            var s = "Skype API error occurred in:" + command.Command + " code:" + code + " (" + description + ")";
            log.Debug(s);
            throw new SkypeApiException(s);
        }

        private string ParseHandle(string t)
        {
            var p = t.LastIndexOf(":");
            return t.Substring(0, p);
        }

        #endregion
    }
}