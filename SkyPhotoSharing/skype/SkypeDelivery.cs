using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SKYPE4COMLib;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using System.Windows.Threading;

namespace SkyPhotoSharing
{
    class SkypeDelivery
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public enum State { INTERVAL, SENDING, RECIEVING, DISCONNECTED }

        public Dictionary<State, string> StateText = new Dictionary<State, string>()
        {
            {State.INTERVAL, "interval"},
            {State.SENDING, "send"},
            {State.RECIEVING, "recieve"},
            {State.DISCONNECTED, "disconnect"},
        };

        public delegate void Event_RaiseDisconnect(Enlister enlister);

        #region 配信コミュニケーション

        public const int CommandSize = 4;
        public const int RetryTime = 3;

        #region 配信コマンド

        public const string SEND = "SEND";
        public const string SIZE = "LEN:";
        public const string END = ":FIN";
        public const string DISCONNECT = "DCON";
        public const string ALIVE = "ALIV";

        #endregion

        #region 応答コマンド

        public const string OK = "ACPT";
        public const string NG = "BUSY";

        #endregion

        // 注)ENDだけは終了マークなので通常パケットとして処理させる。
        private static readonly HashSet<string> COMMANDS = new HashSet<string>(new []{ 
            SEND, DISCONNECT, ALIVE, OK, NG
        }) ;

        #endregion

        private const int PACKET_SIZE = 5120;

        #region コンストラクタ

        public SkypeDelivery(Enlister target)
        {
            _target = target;
            AliveSender = new DispatcherTimer();
        }

        #endregion

        #region 外部用クラスインターフェース

        public static bool IsCommand(string data)
        {
            if (string.IsNullOrEmpty(data) || data.Length < CommandSize) return false;
            if (data.StartsWith(SIZE)) return true;
            return COMMANDS.Contains<string>(data);
        }

        #endregion

        #region プロパティ

        private DispatcherTimer _aliveSender;
        protected DispatcherTimer AliveSender 
        {
            get { return _aliveSender; } 
            set 
            {
                value.Interval = TimeSpan.FromMinutes(5);
                value.Tick += OnNoticeAlive;
                value.Start();
                _aliveSender = value;
            } 
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

        private ConcurrentQueue<string> _receivedCommand = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> ReceivedCommand { get { return _receivedCommand; } }

        private ConcurrentQueue<string> _receivedPacket = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> ReceivedPacket { get { return _receivedPacket; } }

        public Event_RaiseDisconnect EventRaiseDisconnect { get; set; }

        #endregion

        #region 外部向けオブジェクトインターフェース

        public void Close()
        {
            SkypeConnection.Instance.Disconnect(Target);
            ConnectionState = State.DISCONNECTED;
            EventRaiseDisconnect(Target);
            AliveSender.Stop();
        }

        public void PostCommand(string command)
        {
            log.Debug("Command:" + command + " posted.");
            ReceivedCommand.Enqueue(command);
            CheckRecievedRequest();
        }

        public void PostPacket(string packet)
        {
            log.Debug("Data:" + packet.Length + " bytes posted.");
            ReceivedPacket.Enqueue(packet);
        }

        public void PostChatMessage(string message)
        {
            SkypeConnection.Instance.PostChatMessage(Target.Handle, message);
        }

        public void CheckRecievedRequest()
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
                case ALIVE:
                    {
                        return;
                    }
                default:
                    return;

            }
        }

        public async void Send(Photo photo)
        {
            try
            {
                await AsyncSend(photo);
                log.Debug("End send.");
            }
            catch (FileTransactionException ex)
            {
                SkypeConnection.Instance.NoticeTransferExceptionOccurred(ex);
            }
        }

        public async void Recieve()
        {
            try
            {
                var j = await AsyncRecieve();
                var c = SkypeCargo.Parse(j);
                log.Debug("End recieve. :" + c.FileName);
                var p = Photo.Create(c, Target);
                BindablePhotoList.AddFromOnline(p);
            }
            catch (FileTransactionException ex)
            {
                SkypeConnection.Instance.NoticeTransferExceptionOccurred(ex);
            }
        }

        #endregion

        private Task AsyncSend(Photo p)
        {
            log.Debug("Start send. :" + p.FileName);
            return Task.Run(() =>
            {
                SkypeCargo s = new SkypeCargo(p);
                WaitForInterval(State.SENDING);
                WritePacket(SEND);
                if (!OK.Equals(GetReply()))
                {
                    ThrowAndIntervalize(new FileSendException(p.Path, Target));
                }
                var j = s.ConvertToJson();
                var len = j.Length;
                WritePacket(String.Format("{0}{1:D20}", SIZE, len));
                log.Debug("Send size:" + len + " times:" + len / PACKET_SIZE);
                if (!OK.Equals(GetReply()))
                {
                    ThrowAndIntervalize(new FileSendException(p.Path, Target));
                }
                SendAllData(j, len);
                WritePacket(END);
                ConnectionState = State.INTERVAL;
            });
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
                    ThrowAndIntervalize(new FileRecieveException(Target));
                }
                log.Debug("Recieving data size:" + len);
                WritePacket(OK);
                var s = RecieveAllData(len);
                ConnectionState = State.INTERVAL;
                if (s.Length < len)
                {
                    ThrowAndIntervalize(new FileRecieveException(Target));
                }
                log.Debug("Recieved data size:" + s.Length);
                return s.ToString();
            });
        }

        private void RaiseDisconnect()
        {
            if (ConnectionState == State.DISCONNECTED) return;
            AliveSender.Stop();
            WaitForInterval(State.DISCONNECTED);
            WritePacket(DISCONNECT);
        }

        private void WaitForInterval(State nextState)
        {
            log.Debug("Wait for interval");
            UsingIntervalTimeoutCheck(5, () =>
            { 
                while (ConnectionState != State.INTERVAL) SleepThread();
                ConnectionState = nextState;
                return true;
            });
            log.Debug("Interval end.");
        }

        private string GetReply()
        {
            log.Debug("Wait a reply from " + Target.Handle);
            String r = "";
            UsingTimeoutCheck(5, () =>
            { 
                while (!ReceivedCommand.TryDequeue(out r)) SleepThread();
                return true;
            });
            log.Debug("Got a reply.");
            return r;
        }

        private string GetPacket()
        {
            log.Debug("Wait a packet from " + Target.Handle);
            String r = "";
            UsingTimeoutCheck(() =>
            { 
                while (!ReceivedPacket.TryDequeue(out r)) SleepThread();
                return true;
            });
            log.Debug("Got a packet.");
            return r;
        }

        private void SleepThread()
        {
            Thread.SpinWait(10000);
            if (ConnectionState == State.DISCONNECTED) throw new TransactionDisconnectException(Target);
        }

        private int GetFileSize()
        {
            String s = GetReply();
            if (!s.StartsWith(SIZE)) return 0;
            return int.Parse(s.Substring(SIZE.Length));
        }

        private void SendAllData(string json, int length)
        {
            log.Debug("Start send json to " + Target.Handle + ".");
            UsingTimeoutCheck(() =>
            {
                for (int i = 0; i < length; i += PACKET_SIZE)
                {
                    var s = (length >= i + PACKET_SIZE) ? PACKET_SIZE : length - i;
                    WritePacket(json.Substring(i, s));
                }
                return true;
            });
            log.Debug("End send json.");
        }

        private String RecieveAllData(int length)
        {
            log.Debug("Start recieve json from " + Target.Handle + ".");
            var s = new StringBuilder(length);
            UsingTimeoutCheck(5, () =>
            { 
                while (s.Length <= length)
                {
                    var d = GetPacket();
                    if (END.Equals(d)) break;
                    s.Append(d);
                }
                return true;
            });
            log.Debug("End recieve json.");
            return s.ToString();
        }

        private void WritePacket(string data)
        {
            SkypeConnection.Instance.WritePacket(data, Target);
        }

        private void ThrowAndIntervalize(ApplicationException ex)
        {
            ConnectionState = State.INTERVAL;
            throw ex;
        }

        private bool UsingTimeoutCheck(Func<bool> exec)
        {
            return UsingTimeoutCheck(1, exec);
        }

        private bool UsingTimeoutCheck(int minute, Func<bool> exec)
        {
            var ch = StartTimeoutCheck(minute);
            var r = exec();
            StopTimeoutCheck(ch);
            return r;
        }

        private bool UsingIntervalTimeoutCheck(int minute, Func<bool> exec)
        {
            var ch = StartTimeoutCheck(minute, false);
            var r = exec();
            StopTimeoutCheck(ch);
            return r;
        }


        private Timer StartTimeoutCheck(int minute, bool intervalize = true)
        {
            long w = 60000 * minute;
            var c = new Tick();
            c.Intervalize = intervalize;
            var t = new Timer(OnRaiseTimeout, c, w, w);
            c.Sender = t;
            return t;
        }

        private void StopTimeoutCheck(Timer timer)
        {
            timer.Dispose();
        }

        private void OnRaiseTimeout(object tick)
        {
            Tick t = (Tick)tick;
            StopTimeoutCheck(t.Sender);
            log.Debug("Transfer timeout. target:" + Target.Handle);
            var ex = new FileTimeoutException(Target, StateText[ConnectionState]);
            if (t.Intervalize) {
                ThrowAndIntervalize(ex);
            }
            else
            {
                throw ex;
            }
        }

        private void OnNoticeAlive(object sender, EventArgs e)
        {
            if (Target == Enlister.Owner) return;
            if (ConnectionState != State.INTERVAL) return;
            log.Debug("Alive Send. target:" + Target.Handle);
            SkypeConnection.Instance.WritePacket(ALIVE, Target);
        }

        private class Tick
        {
            public Timer Sender { get; set; }
            public bool Intervalize { get; set; }
        }

    }

}
