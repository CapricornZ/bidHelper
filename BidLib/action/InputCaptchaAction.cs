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
            Boolean isLoading = true;
            int retry = 0;
            byte[] binaryCaptcha = null;
            Bitmap bitMap = null;
            while (isLoading) {//1.5秒后，放弃

                System.Threading.Thread.Sleep(100);

                logger.DebugFormat("\tretry[{2}] CAPTURE CAPTCHA({0}, {1})", x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, ++retry);
                binaryCaptcha = new ScreenUtil().screenCaptureAsByte(x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, 128, 28);
                bitMap = new Bitmap(new MemoryStream(binaryCaptcha));
                File.WriteAllBytes(String.Format("AUTO-CAPTCHA{0}.BMP", retry), binaryCaptcha);

                int count = 0;
                for (int nX = 0; nX < bitMap.Width; nX++)
                    for (int nY = 0; nY < bitMap.Height; nY++) {
                        Color color = bitMap.GetPixel(nX, nY);
                        if (color.R == color.G && color.G == color.B && color.B < 200)
                            count++;
                    }

                System.Console.WriteLine("COUNT:" + count);
                isLoading = count > 500;
                if (isLoading && retry > 15) {
                    logger.InfoFormat("\tLoading captcha timeout.");
                    return false;//放弃本次出价
                }
            }

            String txtCaptcha = this.repository.orcCaptcha.IdentifyStringFromPic(bitMap);

            logger.DebugFormat("\tCAPTURE TIPS({0}, {1})", x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y);
            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y, 112, 16);
            File.WriteAllBytes("AUTO-TIPS.BMP", binaryCaptcha);
            String strActive = this.repository.orcCaptchaTipsUtil.getActive(txtCaptcha, new Bitmap(new System.IO.MemoryStream(binaryTips)));
            logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}.", txtCaptcha, strActive);

            logger.Info("\tBEGIN input CAPTCHA");
            KeyBoardUtil.sendMessage(strActive, this.repository.interval);
            System.Threading.Thread.Sleep(50);

            ScreenUtil.SetCursorPos(x + submitPrice.inputBox.x, y + submitPrice.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero); 
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            System.Threading.Thread.Sleep(50);

            logger.Info("\tEND   input CAPTCHA");
            logger.Info("END   giveCAPTCHA");

            return true;
        }
    }
}
