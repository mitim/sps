using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SKYPE4COMLib;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;

namespace SkyPhotoSharing
{
    class SkypeDelivery
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public enum State { INTERVAL, SENDING, RECIEVING, DISCONNECTED }

        public delegate void Event_RaiseDisconnect(Enlister enlister);

        #region 配信コミュニケーション

        public const int CommandSize = 4;
        public const int RetryTime = 3;
        #region 配信コマンド
        public const string SEND = "SEND";
        public const string SIZE = "LEN:";
        public const string END = ":FIN";
        public const string DISCONNECT = "DCON";
        #endregion

        #region 応答コマンド

        public const string OK = "ACPT";
        public const string NG = "BUSY";
        #endregion

        #endregion

        private const int PACKET_SIZE = 5120;

        public SkypeDelivery(Enlister target)
        {
            _target = target;
        }

        private Enlister _target;
        public Enlister Target { get { return _target; } }

        private readonly object _connectionSync = new object();
        private State _connectionState = State.INTERVAL;
        public State ConnectionState
        {
            get
            {
                lock (_connectionSync)
                {
                    return _connectionState;
                }
            }
            set
            {
                lock (_connectionSync)
                {
                    _connectionState = value;
                }
            }
        }

        private ConcurrentQueue<string> _receivedPacket = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> ReceivedPacket { get { return _receivedPacket; } }

        public Event_RaiseDisconnect EventRaiseDisconnect { get; set; }
        
        public void PostPacket(string packet)
        {
            ReceivedPacket.Enqueue(packet);
            CheckRecieveRequest();
        }

        public void Disconnect()
        {
            SkypeConnection.Instance.Disconnect(Target);
            ConnectionState = State.DISCONNECTED;
            EventRaiseDisconnect(Target);
        }

        public void CheckRecieveRequest()
        {
            if (ConnectionState != State.INTERVAL) return;
            var r = GetReply();
            if (!SEND.Equals(r)) return;
            switch (r)
            {
                case SEND:
                    {
                        Recieve();
                        break;
                    }
                case DISCONNECT:
                    {
                        EventRaiseDisconnect(Target);
                        break;
                    }
                default:
                    return;

            }
        }

        public void PostChatMessage(string message)
        {
            SkypeConnection.Instance.PostChatMessage(Target.Handle, message);
        }

        public void Send(Photo p)
        {
            log.Debug("Start send. :" + p.FileName);
            var t = Task.Run(() =>
            {
                SkypeCargo s = new SkypeCargo(p);
                WaitForInterval(State.SENDING);
                WritePacket(SEND);
                if (!OK.Equals(GetReply())) 
                {
                    ThrowAndIntervaled(new FileSendException(p.Path, Target));
                }
                var j = s.ConvertToJson();
                var len = j.Length;
                WritePacket(String.Format("{0}{1:D20}",SIZE,len));
                log.Debug("Send size:" + len + " times:" + len / PACKET_SIZE);
                if (!OK.Equals(GetReply())) 
                {
                    ThrowAndIntervaled(new FileSendException(p.Path, Target));
                }
                SendAllData(j, len);
                WritePacket(END);
                ConnectionState = State.INTERVAL;
                log.Debug("End send.");
            });
        }

        public async void Recieve()
        {
            var j = await AsyncRecieve();
            var c = SkypeCargo.Parse(j);
            log.Debug("End recieve. :" + c.FileName);
            var p = Photo.Create(c, Target);
            PhotoList.AddFromOnline(p);
        }

        private Task<string> AsyncRecieve()
        {
            log.Debug("Start recieve.");
            return Task.Run(() =>
            {
                WaitForInterval(State.RECIEVING);
                WritePacket(OK);
                int len = GetFileSize();
                if (len <= 0)
                {
                    ThrowAndIntervaled(new FileRecieveException(Target));
                }
                log.Debug("Recieving data size:" + len);
                WritePacket(OK);
                var s = RecieveAllData(len);
                ConnectionState = State.INTERVAL;
                if (s.Length < len)
                {
                    ThrowAndIntervaled(new FileRecieveException(Target));
                }
                log.Debug("Recieved data size:" + s.Length);
                var r = s.ToString();
                Thread.Sleep(1000);
                return r;
            });
        }

        private void RaiseDisconnect()
        {
            WaitForInterval(State.DISCONNECTED);
            WritePacket(DISCONNECT);
        }

        private void WaitForInterval(State nextState)
        {
            log.Debug("Wait for interval");
            while (ConnectionState != State.INTERVAL) Thread.Sleep(100);
            ConnectionState = nextState;
            log.Debug("Interval end.");
        }

        private string GetReply()
        {
            String r;
            while (!ReceivedPacket.TryDequeue(out r)) Thread.Sleep(100);
            return r;
        }

        private int GetFileSize()
        {
            String s = GetReply();
            if (!s.StartsWith(SIZE)) return 0;
            return int.Parse(s.Substring(SIZE.Length));
        }

        private void SendAllData(string json, int length)
        {
            for (int i = 0; i < length; i += PACKET_SIZE)
            {
                var s = (length >= i + PACKET_SIZE) ? PACKET_SIZE : length - i;
                WritePacket(json.Substring(i, s));
                Thread.Sleep(10);
            }
        }

        private String RecieveAllData(int length)
        {
            var s = new StringBuilder(length);
            while (s.Length <= length)
            {
                var d = GetReply();
                if (END.Equals(d)) break;
                s.Append(d);
            }
            return s.ToString();
        }

        private void WritePacket(string data)
        {
            SkypeConnection.Instance.WritePacket(data, Target);
        }

        private void ThrowAndIntervaled(ApplicationException ex)
        {
            ConnectionState = State.INTERVAL;
            throw ex;
        }
    }

}
