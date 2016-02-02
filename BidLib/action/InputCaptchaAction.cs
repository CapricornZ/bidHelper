using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tobid.util;
using System.Drawing;
using tobid.rest.position;
using System.IO;
using System.Collections.Specialized;
using tobid.util.http;

namespace tobid.scheduler.jobs.action {

    /// <summary>
    /// 识别、输入校验码
    /// </summary>
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

            logger.Info("BEGIN input CAPTCHA");
            logger.Info("\tBEGIN identify CAPTCHA...");//识别验证码
            String fileName = DateTime.Now.ToString("MMdd-HHmmss");
            logger.DebugFormat("\tcapture CAPTCHA({0}, {1}, save as Captchas/Captcha-{2}.bmp)", 
                x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, fileName);
            byte[] binaryCaptcha = new ScreenUtil().screenCaptureAsByte(x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, 128, 28);
            Bitmap bitMap = new Bitmap(new MemoryStream(binaryCaptcha));
            {//保存
                if (!System.IO.Directory.Exists("Captchas"))
                    System.IO.Directory.CreateDirectory("Captchas");
                bitMap.Save(String.Format("Captchas/Captcha-{0}.bmp", fileName));
            }

            String txtCaptcha = this.repository.orcCaptcha.IdentifyStringFromPic(bitMap);

            logger.DebugFormat("\tCAPTURE TIPS({0}, {1})", x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y);
            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y, 112, 16);
            File.WriteAllBytes("AUTO-TIPS.BMP", binaryCaptcha);
            {//保存
                File.WriteAllBytes(String.Format("Captchas/Captcha-TIP-{0}.bmp", fileName), binaryTips);
            }
            String strActive = this.repository.orcCaptchaTipsUtil.getActive(txtCaptcha, new Bitmap(new System.IO.MemoryStream(binaryTips)));
            logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}.", txtCaptcha, strActive);

            logger.Info("\tBEGIN input CAPTCHA");
            KeyBoardUtil.sendMessage(strActive, interval: this.repository.interval, needClean: true);
            System.Threading.Thread.Sleep(50);
            logger.Info("\tEND   input CAPTCHA");

            logger.Info("\tBEGIN double click inputbox");
            ScreenUtil.SetCursorPos(x + submitPrice.inputBox.x, y + submitPrice.inputBox.y);//校验码区域
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(50);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   double click inputbox");
            logger.Info("END   giveCAPTCHA");

            return true;
        }
    }
    /// <summary>
    /// 检测“正在获取校验码”字样，“刷新校验码”按钮
    /// </summary>
    public class PreCaptchaAction : IBidAction {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(PreCaptchaAction));

        private IRepository repository;
        public PreCaptchaAction(IRepository repo) {
            this.repository = repo;
        }

        public void notify(string message) {
            logger.Debug(message);
        }

        /// <summary>
        /// 如果最终刷出验证码，返回true
        /// 如果最终没有识别到验证码，返回false
        /// </summary>
        /// <returns></returns>
        public bool execute() {

            Point origin = IEUtil.findOrigin();
            int x = origin.X;
            int y = origin.Y;

            SubmitPrice submitPrice = this.repository.submitPrice;
            bool rtn = false;

            //1.
            String fileName = DateTime.Now.ToString("MMdd-HHmmss");
            logger.Info("BEGIN CAPTCHA dialog");

            logger.Info("\tBEGIN[0] click INPUT");//点击输入框
            ScreenUtil.SetCursorPos(x + submitPrice.inputBox.x, y + submitPrice.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(50);
            logger.Info("\tEND[0]   click INPUT blank");

            //2.
            logger.Info("\tBEGIN[LOADING] identify ...");
            Boolean isLoading = true;
            Boolean isRefresh = true;
            int retry = 0;
            byte[] binaryCaptcha = null;
            Bitmap bitMap = null;
            while (isLoading) {

                retry++;
                System.Threading.Thread.Sleep(100);

                logger.DebugFormat("\tretry[{2}] capture screen({0}, {1}), save as Captchas/pre-CAPTCHA-LOADING", x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, retry);
                binaryCaptcha = new ScreenUtil().screenCaptureAsByte(x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, 128, 28);
                bitMap = new Bitmap(new MemoryStream(binaryCaptcha));
                {//保存
                    if (!System.IO.Directory.Exists("Captchas"))
                        System.IO.Directory.CreateDirectory("Captchas");
                    File.WriteAllBytes(String.Format("Captchas/pre-CAPTCHA-LOADING-{0}-{1}.BMP", fileName, retry), binaryCaptcha);
                }

                isLoading = tobid.util.orc.CaptchaHelper.isLoading(bitMap);
                logger.DebugFormat("isLoading : {0}", isLoading);
                if (retry > 35) {
                    logger.Error("\tloading CAPTCHA timeout.");
                    break;
                }
            }
            logger.Info("\tEND[LOADING]   identify");

            //3.
            logger.Info("\tBEGIN[REFRESH] identify ...");
            retry = 0;
            Boolean refresh = false;
            while (isRefresh) {

                retry++;
                System.Threading.Thread.Sleep(100);

                logger.DebugFormat("\tretry[{2}] capture screen({0}, {1})", x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, retry);
                binaryCaptcha = new ScreenUtil().screenCaptureAsByte(x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, 128, 28);
                bitMap = new Bitmap(new MemoryStream(binaryCaptcha));
                File.WriteAllBytes(String.Format("Captchas/CAPTCHA-REFRESH-{0}-{1}.BMP", fileName, retry), binaryCaptcha);

                isRefresh = tobid.util.orc.CaptchaHelper.isRefresh(bitMap);
                logger.DebugFormat("isRefresh : {0}", isRefresh);
                if (isRefresh) {

                    if (!refresh) {
                        ScreenUtil.SetCursorPos(x + submitPrice.captcha[0].x + 55, y + submitPrice.captcha[0].y + 12);//校验码区域
                        ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                    }

                    if (refresh) {
                        ScreenUtil.SetCursorPos(x + submitPrice.inputBox.x, y + submitPrice.inputBox.y);//校验码输入框
                        ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                    }
                }
                else {
                    rtn = true;
                }
                if (retry > 20) {
                    logger.Error("\tRefreshing captcha timeout.");
                    break;
                }
            }

            logger.Info("\tEND[REFRESH]   identify");
            logger.Info("END   CAPTCHA dialog");
            System.Threading.Thread.Sleep(250);

            System.Threading.Thread.Sleep(500);
            byte[] captcha = new ScreenUtil().screenCaptureAsByteJPEG(x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y,
                submitPrice.captcha[0].width, submitPrice.captcha[0].height);
            byte[] tip = new ScreenUtil().screenCaptureAsByteJPEG(x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y,
                submitPrice.captcha[1].width, submitPrice.captcha[1].height);

            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("uid", System.Guid.NewGuid().ToString());
            UploadFile uf1 = new UploadFile {
                Name = "captchaImg",
                FileName = "captcha.jpg",
                ContentType = "image/jpeg",
                Stream = new MemoryStream(captcha)
            };
            UploadFile uf2 = new UploadFile {
                Name = "tipImg", FileName = "tip.jpg",
                ContentType = "image/jpeg",
                Stream = new MemoryStream(tip)
            };
            String s = new tobid.util.http.HttpUtil().postFiles("http://10.228.89.102:8080/im/web/home/request", new UploadFile[] { uf1, uf2 }, nvc);
            System.Console.WriteLine("CAPTCHA : " + s);
            return rtn;
        }
    }

    public class ProcessCaptchaAction : IBidAction {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(ProcessCaptchaAction));

        private IRepository repository;
        public ProcessCaptchaAction(IRepository repo) {
            this.repository = repo;
        }

        public void notify(string message) {
            logger.Debug(message);
        }

        public bool execute()
        {

            Point origin = IEUtil.findOrigin();
            int x = origin.X;
            int y = origin.Y;

            SubmitPrice submitPrice = this.repository.submitPrice;

            logger.Info("BEGIN giveCAPTCHA");
            logger.Info("\tBEGIN click INPUT");//点击输入框
            logger.DebugFormat("\t\tINPUT BOX({0}, {1})", x + submitPrice.inputBox.x, y + submitPrice.inputBox.y);
            ScreenUtil.SetCursorPos(x + submitPrice.inputBox.x, y + submitPrice.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(50);
            logger.Info("\tEND   click INPUT blank");

            logger.Info("\tBEGIN identify CAPTCHA...");//识别验证码
            Boolean isLoading = true;
            int retry = 0;
            byte[] binaryCaptcha = null;
            Bitmap bitMap = null;
            while (isLoading)
            {//1.5秒后，放弃

                System.Threading.Thread.Sleep(100);

                logger.DebugFormat("\tretry[{2}] capture CAPTCHA({0}, {1})", x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, ++retry);
                binaryCaptcha = new ScreenUtil().screenCaptureAsByte(x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, 128, 28);
                bitMap = new Bitmap(new MemoryStream(binaryCaptcha));
                File.WriteAllBytes(String.Format("AUTO-CAPTCHA{0}.BMP", retry), binaryCaptcha);

                isLoading = tobid.util.orc.CaptchaHelper.isLoading(bitMap);
                if (isLoading && retry > 15)
                {
                    logger.InfoFormat("\tloading CAPTCHA timeout.");
                    return false;//放弃本次出价
                }
            }

            {//保存
                String fileName = DateTime.Now.ToString("MMdd-HHmmss");
                if (!System.IO.Directory.Exists("Captchas"))
                    System.IO.Directory.CreateDirectory("Captchas");
                bitMap.Save(String.Format("Captchas/process-CAPTCHA-{0}.bmp", fileName));
            }

            String txtCaptcha = this.repository.orcCaptcha.IdentifyStringFromPic(bitMap);

            logger.DebugFormat("\tCAPTURE TIPS({0}, {1})", x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y);
            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y, 112, 16);
            File.WriteAllBytes("AUTO-TIPS.BMP", binaryCaptcha);
            String strActive = this.repository.orcCaptchaTipsUtil.getActive(txtCaptcha, new Bitmap(new System.IO.MemoryStream(binaryTips)));
            logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}.", txtCaptcha, strActive);

            logger.Info("\tBEGIN input CAPTCHA");
            KeyBoardUtil.sendMessage(strActive, interval:this.repository.interval, needClean:true);
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
