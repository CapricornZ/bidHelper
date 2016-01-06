using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tobid.util;
using System.Drawing;
using tobid.rest.position;
using System.IO;

namespace tobid.scheduler.jobs.action {

    public interface ITask : IAction {
    }

    /// <summary>
    /// 一个成功即成功
    /// </summary>
    public class ComboTask : ITask {

        private List<ITask> m_actions;
        public ComboTask(List<ITask> actions) {
            this.m_actions = actions;
        }

        bool IAction.execute() {

            bool rtn = false;
            for (int i = 0; !rtn && i < this.m_actions.Count; i++)
                rtn = this.m_actions[i].execute();
            return rtn;
        }
    }


    /// <summary>
    /// 当实际提交价格-当前价格==submitReachPrice，需要触发Action
    /// </summary>
    public class TaskPriceBased : ITask {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(TaskPriceBased));

        /// <summary>
        /// 实际提交的价格
        /// </summary>
        public int basePrice { get; set; }
        /// <summary>
        /// 触发差价
        /// </summary>
        public int submitReachPrice { get; set; }
        public IBidAction action { get; set; }
        public IRepository repository { get; set; }

        private InputPriceAction m_inputPriceAction;
        private ScreenUtil m_screenUtil;

        public TaskPriceBased(IBidAction action, InputPriceAction inputPriceAction, int submitReachPrice, IRepository repository) {
            this.action = action;
            this.submitReachPrice = submitReachPrice;
            this.repository = repository;
            this.m_screenUtil = new ScreenUtil();
            m_inputPriceAction = inputPriceAction;
        }

        bool IAction.execute() {

            bool rtn = false;
            BidStep2 step2 = SubmitPriceStep2Job.getPosition();
            Point origin = IEUtil.findOrigin();
            int x = origin.X;
            int y = origin.Y;

            this.basePrice = this.m_inputPriceAction.BasePrice;
            byte[] binary = this.m_screenUtil.screenCaptureAsByte(x + step2.price.x, y+step2.price.y, 80, 20);
            string price = this.repository.orcPriceSM.IdentifyStringFromPic(new Bitmap(new MemoryStream(binary)));
            int currentPrice = Convert.ToInt32(price);
            int delta = this.basePrice - currentPrice;

            String msg = String.Format("出价:{0}, 最低:{1}, 差价:{2}<={3}?", this.basePrice, price, delta, this.submitReachPrice);
            if (delta <= this.submitReachPrice) {
                logger.Warn(msg);
                rtn = this.action.execute();
            } else
                this.action.notify(msg);

            return rtn;
        }
    }

    public class TaskSwitchable : ITask {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(TaskSwitchable));

        private ITask[] tasks;
        private int activeTask;
        public TaskSwitchable(ITask[] tasks) {
            activeTask = 0;
            this.tasks = tasks;
        }

        public void toggle() {
            this.activeTask = (this.activeTask + 1) % this.tasks.Length;
        }

        public bool execute() {
            return this.tasks[this.activeTask].execute();
        }
    }

    /// <summary>
    /// TimeBased
    /// </summary>
    public class TaskTimeBased : ITask, IComparable<TaskTimeBased> {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(TaskTimeBased));

        public DateTime fireTime { get; set; }
        public DateTime expireTime { get; set; }
        public IBidAction action { get; set; }

        private INotify notify;
        public TaskTimeBased(IBidAction action, INotify notify, String fireTime, String expireTime = null) {

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
            String msg = String.Format("剩(s):{0:f3}", -diffFire.TotalSeconds);
            if (diffFire.TotalMilliseconds >= 500) {
                //if (diffFire.TotalSeconds >= 0) {
                logger.Warn(msg);
                if (diffExpire.TotalSeconds <= 0)
                    rtn = this.action.execute();
                //rtn = true;
            }
            else {

                if (null != this.notify)
                    this.notify.acceptMessage(String.Format("SECs:{0:f3}", -diffFire.TotalSeconds));
                this.action.notify(msg);
            }
            return rtn;
        }

        public int CompareTo(TaskTimeBased other) {

            TimeSpan diff = this.fireTime - other.fireTime;
            return (int)diff.TotalSeconds;
        }
    }
}
