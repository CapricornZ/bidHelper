using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using tobid.util;
using tobid.rest.position;
using tobid.util.orc;
using System.Threading;

namespace tobid.scheduler.jobs.action {

    /// <summary>
    /// 仅点击出价“取消”按钮
    /// </summary>
    public class CancelSubmitCaptchaAction : IBidAction {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(CancelSubmitCaptchaAction));

        private IRepository repository;
        public CancelSubmitCaptchaAction(IRepository repo) {
            this.repository = repo;
        }

        public void notify(string message) {
            logger.Debug(message);
        }

        public bool execute() {

            Point origin = IEUtil.findOrigin();
            int x = origin.X;
            int y = origin.Y;

            SubmitPrice submitPrice = this.repository.submitPrice;

            this.repository.lastSubmit = DateTime.Now;
            logger.Info("BEGIN click BUTTON[取消]");
            logger.DebugFormat("BUTTON[取消]({0}, {1})", x + submitPrice.buttons[1].x, y + submitPrice.buttons[1].y);
            ScreenUtil.SetCursorPos(x + submitPrice.buttons[1].x, y + submitPrice.buttons[1].y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("END   click BUTTON[取消]");

            Thread.Sleep(250);

            return true;
        }
    }
    /// <summary>
    /// 仅点击出价“确定”按钮
    /// </summary>
    public class SubmitCaptchaPureAction : IBidAction{
        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SubmitCaptchaPureAction));

        private IRepository repository;
        public SubmitCaptchaPureAction(IRepository repo){
            this.repository = repo;
        }

        public void notify(string message){
            logger.Debug(message);
        }

        public bool execute(){

            Point origin = IEUtil.findOrigin();
            int x = origin.X;
            int y = origin.Y;

            SubmitPrice submitPrice = this.repository.submitPrice;

            this.repository.lastSubmit = DateTime.Now;
            logger.Info("BEGIN click BUTTON[确定]");
            logger.DebugFormat("\tBUTTON[确定]({0}, {1})", x + submitPrice.buttons[0].x, y + submitPrice.buttons[0].y);
            ScreenUtil.SetCursorPos(x + submitPrice.buttons[0].x, y + submitPrice.buttons[0].y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("END   click BUTTON[确定]");

            return true;
        }
    }

    /// <summary>
    /// 点击出价“确定”按钮后，检测&等待提交结束，并触发F9
    /// </summary>
    public class SubmitCaptchaAction : IBidAction {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SubmitCaptchaAction));

        private IRepository repository;
        public SubmitCaptchaAction(IRepository repo) {
            this.repository = repo;
        }

        public void notify(string message) {
            logger.Debug(message);
        }

        public bool execute() {

            Point origin = IEUtil.findOrigin();
            int x = origin.X;
            int y = origin.Y;

            SubmitPrice submitPrice = this.repository.submitPrice;

            if (!this.repository.isReady) {
                //如果验证码还没ready
                logger.Info("已到提交时间，等待输入验证码");
                return false;
            }

            logger.Info("BEGIN click BUTTON[确定]");
            logger.DebugFormat("\tBUTTON[确定]({0}, {1})", x + submitPrice.buttons[0].x, y + submitPrice.buttons[0].y);
            this.repository.lastSubmit = DateTime.Now;
            ScreenUtil.SetCursorPos(x + submitPrice.buttons[0].x, y + submitPrice.buttons[0].y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("END   click BUTTON[确定]");

            BidStatus status = BidStatus.INPROGRESS;
            DateTime start = this.repository.lastSubmit;
            while (true) {

                System.Threading.Thread.Sleep(250);

                if (status == BidStatus.INPROGRESS) {

                    ScreenUtil screen = new ScreenUtil();
                    byte[] content = screen.screenCaptureAsByte(x + submitPrice.buttons[0].x + 50, y + submitPrice.buttons[1].y - 22, 76, 29);
                    Bitmap bitmap = Bitmap.FromStream(new System.IO.MemoryStream(content)) as Bitmap;
                    status = CaptchaHelper.detectBidStatus(bitmap, submitPrice.retryThreshold);
                    String fileName = DateTime.Now.ToString("MMdd-HHmmss-fff");
                    ScreenUtil.saveAs(String.Format("Captchas/AUTO-BUTTON-{0}.bmp", fileName), content);
                }

                TimeSpan diff = DateTime.Now - start;
                if (status == BidStatus.FINISH) {
                    this.repository.lastCost = diff;
                    System.Console.WriteLine(BidStatus.FINISH);
                    //ScreenUtil.SetCursorPos(x + submitPrice.buttons[0].x + 50 + 38, y + submitPrice.buttons[0].y);

                    logger.DebugFormat("点击[确认]按钮 {x:{0},y:{1}}", x + this.repository.bidStep2.okButton.x, y + this.repository.bidStep2.okButton.y);
                    ScreenUtil.SetCursorPos(x + this.repository.bidStep2.okButton.x, y + this.repository.bidStep2.okButton.y);
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                    break;
                }

                //if (diff.TotalMilliseconds >= 5000)
                //    break;//超过5秒

                if (diff.TotalMilliseconds > 30000){//超过30s也停止
                    logger.Error("30秒超时，停止SubmitCaptchaAction");
                    break;
                }
            }

            System.Threading.Thread.Sleep(100);
            KeyBoardUtil.clickKey("F9", 50);
            return true;
        }
    }

    /// <summary>
    /// 还没有实现逻辑
    /// </summary>
    public class SubmitCaptchaAIAction : IBidAction {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SubmitCaptchaAIAction));

        private IRepository repository;
        public SubmitCaptchaAIAction(IRepository repo) {
            this.repository = repo;
        }

        public void notify(string message) {
            logger.Debug(message);
        }

        public bool execute() {
            return false;
        }
    }
}
