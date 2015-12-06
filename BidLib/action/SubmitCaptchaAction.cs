using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using tobid.util;
using tobid.rest.position;

namespace tobid.scheduler.jobs.action {
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

            logger.Info("BEGIN click BUTTON[确定]");
            logger.DebugFormat("BUTTON[确定]({0}, {1})", x + submitPrice.buttons[0].x, y + submitPrice.buttons[0].y);
            ScreenUtil.SetCursorPos(x + submitPrice.buttons[0].x, y + submitPrice.buttons[0].y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("END   click BUTTON[确定]");

            return true;
        }
    }

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
