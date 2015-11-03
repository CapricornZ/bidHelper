using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tobid.util;
using System.Drawing;
using tobid.rest.position;
using System.IO;

namespace tobid.scheduler.jobs.action {

    public class InputCaptchaAction : IBidAction {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(InputCaptchaAction));

        private IRepository repository;
        public InputCaptchaAction(IRepository repo) {
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

            logger.Info("BEGIN giveCAPTCHA");
            logger.Info("\tBEGIN make INPUT blank");
            logger.DebugFormat("\t\tINPUT BOX({0}, {1})", x + submitPrice.inputBox.x, y + submitPrice.inputBox.y);
            ScreenUtil.SetCursorPos(x + submitPrice.inputBox.x, y + submitPrice.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(50);

            System.Windows.Forms.SendKeys.SendWait("{BACKSPACE 4}");
            System.Windows.Forms.SendKeys.SendWait("{DEL 4}");
            logger.Info("\tEND   make INPUT blank");

            logger.Info("\tBEGIN identify CAPTCHA...");
            logger.DebugFormat("\t\tCAPTURE TIPS({0}, {1})", x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y);
            byte[] binaryCaptcha = new ScreenUtil().screenCaptureAsByte(x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, 128, 28);
            Bitmap bitMap = new Bitmap(new MemoryStream(binaryCaptcha));
            File.WriteAllBytes("AUTO-CAPTCHA.BMP", binaryCaptcha);
            String txtCaptcha = this.repository.orcCaptcha.IdentifyStringFromPic(bitMap);

            logger.DebugFormat("\t\tCAPTURE TIPS({0}, {1})", x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y);
            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y, 112, 16);
            File.WriteAllBytes("AUTO-TIPS.BMP", binaryCaptcha);
            String strActive = this.repository.orcCaptchaTipsUtil.getActive(txtCaptcha, new Bitmap(new System.IO.MemoryStream(binaryTips)));
            logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}.", txtCaptcha, strActive);

            logger.Info("\tBEGIN input CAPTCHA");
            KeyBoardUtil.sendMessage(strActive, this.repository.interval);
            System.Threading.Thread.Sleep(100);
            logger.Info("\tEND   input CAPTCHA");

            logger.Info("END   giveCAPTCHA");

            return true;
        }
    }
}
