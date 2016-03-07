using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharp;
using Newtonsoft.Json.Converters;
using System.Threading;
using tobid.rest;

namespace tobid.util.http.ws {

    public interface IBidRepository {

        Config config { get; }
    }

    public class KeepAliveHandler {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(KeepAliveHandler));

        public String user { get; set; }
        public int interval { get; set; }
        public SocketClient session { get; set; }
        public Thread thread { get; set; }
        public IBidRepository bidRepository { get; set; }

        public void abort() { this.interval = 0; }

        #region KeepAlive Thread
        public static void keepAliveThread(object obj) {

            KeepAliveHandler handler = (KeepAliveHandler)obj;
            logger.InfoFormat("KeepAlive initialize.", handler.interval);
            while (handler.interval > 0) {

                logger.DebugFormat("PUSH Heartbeat && SLEEP {0}s", handler.interval);
                if(handler.bidRepository.config == null)
                    handler.session.send(new HeartBeat());
                else
                    handler.session.send(new HeartBeat(String.Format("{0}[{1}]", 
                        handler.bidRepository.config.pname, handler.bidRepository.config.no)));
                Thread.Sleep(handler.interval * 1000);
            }
            logger.InfoFormat("KeepAlive terminated!", handler.interval);
        }
        #endregion
    }

    public class SocketClient : IDisposable {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SocketClient));

        public void Dispose() { this.stop(); }

        public delegate void ProcessMessage(Command command);
        public delegate void ProcessConnect();
        public delegate void ProcessError();
        public delegate void ProcessClose();

        public static String USER = "USER";
        public static int MAX_RECONNECT = 5;

        private String url;
        private String user;
        private IBidRepository bidRepository;
        private WebSocket webSocket;
        private ProcessMessage processMessage;
        private ProcessError processError;
        private ProcessConnect processConnect;
        private ProcessClose processClose;

        public int interval { get; set; }

        public SocketClient(String url, String user, IBidRepository bidRepo, 
            ProcessMessage processMessage, ProcessConnect processConnect = null, ProcessError processError = null, ProcessClose processClose = null) {

            this.url = url;
            this.user = user;
            this.bidRepository = bidRepo;
            this.processMessage = processMessage;
            this.processConnect = processConnect;
            this.processError = processError;
            this.processClose = processClose;
        }

        public void start(int interval = 15) {

            logger.DebugFormat("connecting to {0}", this.url);
            logger.InfoFormat("START(keepAlive : {0})", interval);
            this.interval = interval;
            this.connect();
        }

        public void stop() {

            logger.Info("STOP!");
            if (this.webSocket.ReadyState == WebSocketState.Open || this.webSocket.ReadyState == WebSocketState.Connecting) {
                this.webSocket.Close(CloseStatusCode.Normal, "USER CLOSED");
            }
            ((IDisposable)this.webSocket).Dispose();
        }

        public void send(Command command) {

            String value = Newtonsoft.Json.JsonConvert.SerializeObject(command, new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
            this.webSocket.Send(value);
        }

        private void OnError(Object sender, ErrorEventArgs msg) {

            logger.ErrorFormat("ERROR : {0}", msg.Message);
            //TODO:重连
            if (this.processError != null)
                this.processError();
        }

        private void OnMessage(Object sender, MessageEventArgs msg) {

            logger.Info("ON MESSAGE");
            if (msg.IsPing) {

                logger.Debug("PING");
            } else {

                logger.DebugFormat("ON MESSAGE : {1}", this.user, msg.Data);
                Command command = Newtonsoft.Json.JsonConvert.DeserializeObject<Command>(msg.Data, new CommandConvert());
                this.processMessage(command);
            }
        }

        private void OnClose(Object sender, CloseEventArgs msg) {

            logger.InfoFormat("ON CLOSE code:{0} - {1}", msg.Code, msg.Reason);
            this.stop();
            if (null != this.processClose)
                this.processClose();
        }

        private void OnOpen(Object sender, EventArgs e) {

            logger.InfoFormat("ON CONNECT : {0}", this.user);
            if (this.processConnect != null)
                this.processConnect();
        }

        private void connect() {

            this.webSocket = new WebSocket(this.url);
            this.webSocket.OnOpen += new EventHandler(OnOpen);
            this.webSocket.OnClose += new EventHandler<CloseEventArgs>(OnClose);
            this.webSocket.OnError += new EventHandler<ErrorEventArgs>(OnError);
            this.webSocket.OnMessage += new EventHandler<MessageEventArgs>(OnMessage);

            this.webSocket.SetCredentials("host", "Pass2010", true);
            this.webSocket.SetCookie(new WebSocketSharp.Net.Cookie(SocketClient.USER, String.IsNullOrEmpty(this.user)?"DEFAULT WSocket":this.user));
            this.webSocket.Log.Level = LogLevel.Debug;
            this.webSocket.Connect();
        }
    }
}
