using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using tobid.util;
using tobid.util.orc;
using tobid.rest;
using tobid.rest.position;

namespace tobid.scheduler.jobs {

    public class SubmitPriceStep1Job : ISchedulerJob {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SubmitPriceStep1Job));
        private static Object lockObj = new Object();

        private static Step1Operation bidOperation = null;
        private static BidStep1 operation = null;
        private static int executeCount = 1;

        private IOrc m_orcLoading;
        private CaptchaUtil m_captchaUtil;
        private IOrc m_orcCaptcha;

        public SubmitPriceStep1Job(IOrc orcLoading, CaptchaUtil captchaUtil, IOrc orcCaptcha) {

            this.m_orcLoading = orcLoading;
            this.m_captchaUtil = captchaUtil;
            this.m_orcCaptcha = orcCaptcha;
        }

        public static Boolean setConfig(Step1Operation operation) {

            logger.Info("setConfig {...}");
            Boolean rtn = false;
            if (Monitor.TryEnter(SubmitPriceStep1Job.lockObj, 500)) {

                if ((null == SubmitPriceStep1Job.bidOperation)
                    || (operation.updateTime > SubmitPriceStep1Job.bidOperation.updateTime)) { //确保同一个版本（修改）的Operation只被配置并执行一次，避免多次执行
                
                    logger.DebugFormat("PRICE     : {0}", operation.price);
                    //logger.DebugFormat("startTime : {0}", operation.startTime);
                    //logger.DebugFormat("expireTime: {0}", operation.expireTime);

                    SubmitPriceStep1Job.executeCount = 0;
                    SubmitPriceStep1Job.bidOperation = operation;
                    SubmitPriceStep1Job.operation = Newtonsoft.Json.JsonConvert.DeserializeObject<BidStep1>(operation.content);
                    rtn = true;
                }
                Monitor.Exit(SubmitPriceStep1Job.lockObj);
            } else
                logger.Error("obtain SubmitPriceJob.lockObj timeout on setConfig(...)");

            return rtn;
        }

        public void Execute() {

            DateTime now = DateTime.Now;
            if (null == SubmitPriceStep1Job.bidOperation)
                logger.Debug(String.Format("{{Count:{0}}}", SubmitPriceStep1Job.executeCount));
            else
                logger.Debug(String.Format("{Count:{2}}}",
                    SubmitPriceStep1Job.executeCount));

            if (Monitor.TryEnter(SubmitPriceStep1Job.lockObj, 500)) {

                if (null != SubmitPriceStep1Job.bidOperation){
                    //&& now >= SubmitPriceStep1Job.bidOperation.startTime && now <= SubmitPriceStep1Job.bidOperation.expireTime && SubmitPriceStep1Job.executeCount == 0) {

                    SubmitPriceStep1Job.executeCount++;
                    logger.Warn("trigger Fired");

                    Boolean success = false;
                    int submitCount = 0;
                    while (!success && submitCount < 2) {//1次出价，1次重试

                        submitCount++;
                        this.givePrice(SubmitPriceStep1Job.operation.give, price: SubmitPriceStep1Job.bidOperation.price);//出价
                        success = this.submit(SubmitPriceStep1Job.operation.submit);//提交
                        logger.WarnFormat("ROUND[{0}] {1}", submitCount, success ? "SUCCESS" : "FAILED");
                    }

                    if (!success) {

                        MessageBoxButtons messButton = MessageBoxButtons.OK;
                        DialogResult dr = MessageBox.Show("重试多次后失败，请手动出价", "出价提醒", messButton, 
                            MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    }
                }

                Monitor.Exit(SubmitPriceStep1Job.lockObj);
            } else
                logger.Error("obtain SubmitPriceStep1Job.lockObj timeout on Execute(...)");

        }

        /// <summary>
        /// 绝对价格，出价
        /// </summary>
        /// <param name="givePrice">坐标</param>
        /// <param name="price">绝对价</param>
        private void givePrice(GivePriceStep1 givePrice, int price) {

            logger.WarnFormat("BEGIN givePRICE(price : {0})", price);

            String txtPrice = String.Format("{0:D}", price);

            //INPUT BOX
            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            for (int i = 0; i < givePrice.inputBox.Length; i++) {

                ScreenUtil.SetCursorPos(givePrice.inputBox[i].x, givePrice.inputBox[i].y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

                System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
                System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
                System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
                System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
                System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);

                System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
                System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
                System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
                System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
                System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);

                for (int j = 0; j < txtPrice.Length; j++) {
                    System.Threading.Thread.Sleep(100);
                    ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[j].ToString()], 0, 0, 0);
                }
            }
            logger.Info("\tEND   input PRICE");

            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            System.Threading.Thread.Sleep(50);
            ScreenUtil.SetCursorPos(givePrice.button.x, givePrice.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");
        }

        private Boolean submit(SubmitPrice submitPoints) {

            logger.Info("BEGIN giveCAPTCHA");
            logger.Info("\tBEGIN make INPUT blank");
            ScreenUtil.SetCursorPos(submitPoints.inputBox.x, submitPoints.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0);

            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0);
            logger.Info("\tEND   make INPUT blank");

            logger.Info("\tBEGIN identify CAPTCHA...");
            byte[] binaryCaptcha = null;
            Boolean isLoading = true;
            int retry = 0;
            Thread.Sleep(5000);//等待5秒钟（等待验证码或者“正在获取验证码”字样出来

            while (isLoading)//重试3.5秒钟
            {
                binaryCaptcha = new ScreenUtil().screenCaptureAsByte(submitPoints.captcha[0].x, submitPoints.captcha[0].y, 128, 28);
                File.WriteAllBytes(String.Format("AUTO-LOADING-STEP1-{0}.BMP", retry), binaryCaptcha);
                String strLoading = this.m_orcLoading.IdentifyStringFromPic(new Bitmap(new MemoryStream(binaryCaptcha)));
                logger.InfoFormat("\t LOADING({0}) = {1}", retry++, strLoading);
                if ("正在获取校验码".Equals(strLoading)) {
                    if (retry > 14) {
                        //重试1,2,3,4     ->1秒
                        //重试5,6,7,8     ->2秒
                        //重试9,10,11,12  ->3秒
                        //重试13,14       ->0.5秒
                        //都在获取校验码
                        logger.InfoFormat("Loading captcha timeout. 放弃 close & re-open");
                        ScreenUtil.SetCursorPos(submitPoints.buttons[0].x + 188, submitPoints.buttons[0].y);//取消按钮
                        ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                        return false;//放弃本次出价
                    }
                    Thread.Sleep(250);
                } else
                    isLoading = false;
            }

            File.WriteAllBytes("AUTO-CAPTCHA-STEP1.BMP", binaryCaptcha);
            String txtCaptcha = this.m_orcCaptcha.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(binaryCaptcha)));
            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(submitPoints.captcha[1].x, submitPoints.captcha[1].y, 112, 16);
            File.WriteAllBytes("AUTO-TIPS-STEP1.BMP", binaryCaptcha);
            String strActive = this.m_captchaUtil.getActive(txtCaptcha, new Bitmap(new System.IO.MemoryStream(binaryTips)));
            logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}", txtCaptcha, strActive);

            logger.Info("\tBEGIN input CAPTCHA");
            {
                for (int i = 0; i < strActive.Length; i++) {
                    ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0, 0);
                    System.Threading.Thread.Sleep(100);
                }
            }
            logger.Info("\tEND   input CAPTCHA");

            logger.Info("\tBEGIN click BUTTON[确定]");
            ScreenUtil.SetCursorPos(submitPoints.buttons[0].x, submitPoints.buttons[0].y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[确定]");

            ScreenUtil.SetCursorPos(submitPoints.buttons[0].x + 188 / 2, submitPoints.buttons[0].y - 10);//确定按钮
            logger.Info("END   giveCAPTCHA");
            return true;
        }
    }
}
