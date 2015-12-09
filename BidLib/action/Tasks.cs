using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tobid.scheduler.jobs.action {

    /// <summary>
    /// 
    /// </summary>
    public class Task : IAction, IComparable<Task> {

        public DateTime fireTime { get; set; }
        public DateTime expireTime { get; set; }
        public IBidAction action { get; set; }

        private INotify notify;
        public Task(IBidAction action, INotify notify, String fireTime, String expireTime = null) {

            this.fireTime = Convert.ToDateTime(fireTime);
            if (String.IsNullOrEmpty(expireTime))
                this.expireTime = this.fireTime.AddMinutes(5);
            else
                this.expireTime = Convert.ToDateTime(expireTime);
            this.action = action;
            this.notify = notify;
        }


        public bool execute() {

            Boolean rtn = false;
            DateTime now = DateTime.Now;
            TimeSpan diffFire = now - this.fireTime;
            TimeSpan diffExpire = now - this.expireTime;
            if (diffFire.TotalMilliseconds >= 500) {
                //if (diffFire.TotalSeconds >= 0) {

                if (diffExpire.TotalSeconds <= 0)
                    this.action.execute();
                rtn = true;
            }
            else {

                String msg = String.Format("剩(s):{0:f3}", -diffFire.TotalSeconds);
                if (null != this.notify)
                    this.notify.acceptMessage(String.Format("SECs:{0:f3}", -diffFire.TotalSeconds));
                this.action.notify(msg);
            }
            return rtn;
        }

        public int CompareTo(Task other) {

            TimeSpan diff = this.fireTime - other.fireTime;
            return (int)diff.TotalSeconds;
        }
    }
}
