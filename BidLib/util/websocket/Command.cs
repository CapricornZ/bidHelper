using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tobid.util.http.ws {

    public abstract class Command {

        public String category { get; set; }
        public DateTime time { get; set; }
    }

    public class HeartBeat : Command {

        public HeartBeat() {
            this.category = "HEARTBEAT";
            this.time = DateTime.Now;
        }

        public HeartBeat(String content){
            this.category = "HEARTBEAT";
            this.time = DateTime.Now;
            this.content = content;
        }

        public String content { get; set; }
    }

    public class Ready : Command {

        public Ready() {
            this.category = "READY";
            this.time = DateTime.Now;
        }

        public Ready(String user) {
            this.category = "READY";
            this.time = DateTime.Now;
            this.user = user;
        }

        public String user { get; set; }
    }

    public class Message : Command {

        public Message() {
            this.category = "MESSAGE";
            this.time = DateTime.Now;
        }

        public String content { get; set; }
    }

    

    #region 远程验证码
    public class Reply : Command {

        public Reply() {
            this.category = "REPLY";
            this.time = DateTime.Now;
        }

        public String code { get; set; }
        public String uid { get; set; }
        public String from { get; set; }
    }

    public class Retry : Command {

        public Retry() {
            this.category = "RETRY";
            this.time = DateTime.Now;
        }

        public String from { get; set; }
        public String uid { get; set; }
    }
    #endregion
}
