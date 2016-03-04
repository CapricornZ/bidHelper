using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tobid.util;
using tobid.rest.position;
using System.Drawing;
using System.IO;

namespace tobid.scheduler.jobs.action {
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

            System.Threading.Thread startFire = new System.Threading.Thread(delegate() {
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
                        ScreenUtil.saveAs(String.Format("Captchas/pre-CAPTCHA-LOADING-{0}-{1}.BMP", fileName, retry), binaryCaptcha);
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
                    ScreenUtil.saveAs(String.Format("Captchas/CAPTCHA-REFRESH-{0}-{1}.BMP", fileName, retry), binaryCaptcha);

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
                    } else {
                        rtn = true;
                    }
                    if (retry > 20) {
                        logger.Error("\tRefreshing captcha timeout.");
                        break;
                    }
                }

                logger.Info("\tEND[REFRESH]   identify");
                logger.Info("END   CAPTCHA dialog");
                
                //4. remote CAPTCHA assistance
                System.Threading.Thread.Sleep(500);
                byte[] captcha = new ScreenUtil().screenCaptureAsByteJPEG(x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y,
                    submitPrice.captcha[0].width, submitPrice.captcha[0].height);
                byte[] tip = new ScreenUtil().screenCaptureAsByteJPEG(x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y,
                    submitPrice.captcha[1].width, submitPrice.captcha[1].height);

                String strCaptcha = this.repository.submitCaptcha(captcha: new MemoryStream(captcha), tips: new MemoryStream(tip));
                if (!"ERROR".Equals(strCaptcha)) {

                    if ("RETRY".Equals(strCaptcha)) {

                        logger.Info("BEGIN RETRY");

                            ScreenUtil.SetCursorPos(x + submitPrice.captcha[0].x + 55, y + submitPrice.captcha[0].y + 12);//校验码区域
                            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

                            ScreenUtil.SetCursorPos(x + submitPrice.inputBox.x, y + submitPrice.inputBox.y);//校验码输入框
                            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

                            System.Threading.Thread.Sleep(500);
                            captcha = new ScreenUtil().screenCaptureAsByteJPEG(x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y,
                                submitPrice.captcha[0].width, submitPrice.captcha[0].height);
                            tip = new ScreenUtil().screenCaptureAsByteJPEG(x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y,
                                submitPrice.captcha[1].width, submitPrice.captcha[1].height);

                            strCaptcha = this.repository.submitCaptcha(captcha: new MemoryStream(captcha), tips: new MemoryStream(tip));

                            logger.InfoFormat("BEGIN input captcha {0}", strCaptcha);
                            ScreenUtil.SetCursorPos(x + submitPrice.inputBox.x, y + submitPrice.inputBox.y);
                            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                            System.Threading.Thread.Sleep(25);

                            KeyBoardUtil.sendMessage(strCaptcha, interval: this.repository.interval, needClean: true);
                            logger.Info("END   input captcha");

                            logger.Info("set CAPTCHA READY!");
                            this.repository.isReady = true;

                        logger.Info("END   RETRY");
                    } else {

                        logger.InfoFormat("BEGIN input captcha {0}", strCaptcha);
                        ScreenUtil.SetCursorPos(x + submitPrice.inputBox.x, y + submitPrice.inputBox.y);
                        ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                        System.Threading.Thread.Sleep(25);

                        KeyBoardUtil.sendMessage(strCaptcha, interval: this.repository.interval, needClean: true);
                        logger.Info("END   input captcha");

                        logger.Info("set CAPTCHA READY!");
                        this.repository.isReady = true;
                    }
                }
            });

            startFire.Name = "PreCaptcha";
            startFire.Start();
            return true;
        }
    }
}
