using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading;

using tobid.rest;
using tobid.rest.json;
using tobid.rest.position;
using tobid.util.http;
using tobid.util.orc;
using tobid.util;
using tobid.scheduler;

namespace tobid.scheduler.jobs
{
    /// <summary>
    /// SubmitPrice : 每秒检查，符合条件执行出价Action
    /// </summary>
    public class SubmitPriceStep2Job : ISchedulerJob{

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SubmitPriceStep2Job));
        private static Object lockObj = new Object();

        private static int price = 0;
        private static Step2Operation bidOperation;
        private static int executeCount = 1;
        private static BidStep2 operation = null;

        public String EndPoint { get; set; }
        private IOrc m_orcPrice;
        private IOrc m_orcLoading;
        private CaptchaUtil m_captchaUtil;
        private IOrc m_orcCaptcha;

        public SubmitPriceStep2Job(String endPoint, IOrc orcPrice, IOrc orcLoading, CaptchaUtil captchaUtil, IOrc orcCaptcha) {

            this.EndPoint = endPoint;
            this.m_orcPrice = orcPrice;
            this.m_orcLoading = orcLoading;
            this.m_captchaUtil = captchaUtil;
            this.m_orcCaptcha = orcCaptcha;
        }

        public static Step2Operation getConfig() {

            Step2Operation ops = new Step2Operation();
            ops.expireTime = SubmitPriceStep2Job.bidOperation.expireTime;
            ops.startTime = SubmitPriceStep2Job.bidOperation.startTime;
            ops.content = SubmitPriceStep2Job.bidOperation.content;
            ops.id = SubmitPriceStep2Job.bidOperation.id;
            ops.price = SubmitPriceStep2Job.bidOperation.price;
            ops.type = SubmitPriceStep2Job.bidOperation.type;
            ops.updateTime = SubmitPriceStep2Job.bidOperation.updateTime;
            return ops;
        }

        public static BidStep2 getPosition(){

            return SubmitPriceStep2Job.operation;
        }

        public static Boolean setConfig(int Price, Step2Operation operation) {

            logger.Info("setConfig {...}");
            Boolean rtn = false;
            if (Monitor.TryEnter(SubmitPriceStep2Job.lockObj, 500))
            {
                if ((null == SubmitPriceStep2Job.bidOperation)
                    || (operation.updateTime > SubmitPriceStep2Job.bidOperation.updateTime))//确保同一个版本（修改）的Operation只被配置并执行一次，避免多次执行
                {
                    logger.DebugFormat("PRICE     : {0}", operation.price);
                    logger.DebugFormat("startTime : {0}", operation.startTime);
                    logger.DebugFormat("expireTime: {0}", operation.expireTime);

                    SubmitPriceStep2Job.price = Price;
                    SubmitPriceStep2Job.executeCount = 0;
                    SubmitPriceStep2Job.bidOperation = operation;
                    SubmitPriceStep2Job.operation = Newtonsoft.Json.JsonConvert.DeserializeObject<BidStep2>(operation.content);
                    rtn = true;
                }
                Monitor.Exit(SubmitPriceStep2Job.lockObj);
            }
            else
                logger.Error("obtain SubmitPriceJob.lockObj timeout on setConfig(...)");
            return rtn;
        }

        public static Boolean setConfig(Step2Operation operation) {

            logger.Info("setConfig {...}");
            Boolean rtn = false;
            if (Monitor.TryEnter(SubmitPriceStep2Job.lockObj, 500))
            {
                if ((null == SubmitPriceStep2Job.bidOperation) 
                    || (operation.updateTime > SubmitPriceStep2Job.bidOperation.updateTime))//确保同一个版本（修改）的Operation只被配置并执行一次，避免多次执行
                {
                    logger.DebugFormat("PRICE     : {0}", operation.price);
                    logger.DebugFormat("startTime : {0}", operation.startTime);
                    logger.DebugFormat("expireTime: {0}", operation.expireTime);

                    SubmitPriceStep2Job.price = 0;
                    SubmitPriceStep2Job.executeCount = 0;
                    SubmitPriceStep2Job.bidOperation = operation;
                    SubmitPriceStep2Job.operation = Newtonsoft.Json.JsonConvert.DeserializeObject<BidStep2>(operation.content);
                    rtn = true;
                }
                Monitor.Exit(SubmitPriceStep2Job.lockObj);
            }
            else
                logger.Error("obtain SubmitPriceJob.lockObj timeout on setConfig(...)");

            return rtn;
        }

        public void Execute()
        {
            DateTime now = DateTime.Now;
            if (null == SubmitPriceStep2Job.bidOperation)
                logger.Debug(String.Format("{{Count:{0}}}", SubmitPriceStep2Job.executeCount));
            else
                logger.Debug(String.Format("{{Start:{0}, Expire:{1}, Count:{2}}}", 
                    SubmitPriceStep2Job.bidOperation.startTime, SubmitPriceStep2Job.bidOperation.expireTime,
                    SubmitPriceStep2Job.executeCount));

            if (Monitor.TryEnter(SubmitPriceStep2Job.lockObj, 500))
            {
                if (null != SubmitPriceStep2Job.bidOperation && 
                    now >= SubmitPriceStep2Job.bidOperation.startTime && now <= SubmitPriceStep2Job.bidOperation.expireTime && SubmitPriceStep2Job.executeCount == 0)
                {
                    //TODO:这里可以加入逻辑，如果this.submit成功，SubmitPriceJob.executeCount++。
                    //这样在下一秒可以自动执行一次未成功的出价。但是DeltaPrice应该-=100，同时需要保证DeltaPrice>=+300
                    SubmitPriceStep2Job.executeCount++;
                    logger.Warn("trigger Fired");

                    Boolean success = false;
                    int submitCount = 0;
                    while (!success && submitCount < 2) {//1次出价，1次重试

                        submitCount++;
                        if (SubmitPriceStep2Job.price != 0)
                            this.givePrice(SubmitPriceStep2Job.operation.give, price: SubmitPriceStep2Job.price);//出价;
                        else{

                            //int delta = SubmitPriceStep2Job.bidOperation.price > 300 ?
                            //    SubmitPriceStep2Job.bidOperation.price - (submitCount - 1) * 100 : SubmitPriceStep2Job.bidOperation.price;
                            int delta = submitCount == 1 ? SubmitPriceStep2Job.bidOperation.price : 300;
                            this.giveDeltaPrice(SubmitPriceStep2Job.operation.give, delta: delta);//出价
                        }
                        success = this.submit(this.EndPoint, SubmitPriceStep2Job.operation.submit);//提交
                        logger.WarnFormat("ROUND[{0}] {1}", submitCount, success?"SUCCESS":"FAILED");
                    }
                }

                Monitor.Exit(SubmitPriceStep2Job.lockObj);
            }
            else
                logger.Error("obtain SubmitPriceJob.lockObj timeout on Execute(...)");

        }

        /// <summary>
        /// 绝对价格，出价
        /// </summary>
        /// <param name="givePrice">坐标</param>
        /// <param name="price">绝对价</param>
        private void givePrice(GivePriceStep2 givePrice, int price)
        {
            logger.InfoFormat("BEGIN givePRICE(price : {0})", price);
            
            String txtPrice = String.Format("{0:D}", price);

            //INPUT BOX
            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            logger.DebugFormat("INPUT BOX({0}, {1})", givePrice.inputBox.x, givePrice.inputBox.y);
            ScreenUtil.SetCursorPos(givePrice.inputBox.x, givePrice.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);

            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);

            for (int i = 0; i < txtPrice.Length; i++)
            {
                System.Threading.Thread.Sleep(25);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0x2, 0);
            }
            logger.Info("\tEND   input PRICE");

            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            logger.DebugFormat("BOX[出价]({0}, {1})", givePrice.button.x, givePrice.button.y);
            System.Threading.Thread.Sleep(50);
            ScreenUtil.SetCursorPos(givePrice.button.x, givePrice.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");
        }

        /// <summary>
        /// 获取当前价格，+delta，出价
        /// </summary>
        /// <param name="givePrice">坐标</param>
        /// <param name="delta">差价</param>
        private void giveDeltaPrice(GivePriceStep2 givePrice, int delta)
        {
            logger.InfoFormat("BEGIN givePRICE(delta : {0})", delta);
            //INPUT BOX

            logger.Info("\tBEGIN make PRICE blank...");
            logger.DebugFormat("INPUT BOX({0}, {1})", givePrice.inputBox.x, givePrice.inputBox.y);
            ScreenUtil.SetCursorPos(givePrice.inputBox.x, givePrice.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);

            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            logger.Info("\tEND   make PRICE blank...");

            logger.Info("\tBEGIN identify PRICE...");
            logger.DebugFormat("CAPTURE PRICE({0}, {1})", givePrice.price.x, givePrice.price.y);
            byte[] content = new ScreenUtil().screenCaptureAsByte(givePrice.price.x, givePrice.price.y, 52, 18);
            String txtPrice = this.m_orcPrice.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
            int price = Int32.Parse(txtPrice);
            price += delta;
            logger.InfoFormat("\tEND   identified PRICE = {0}", txtPrice);
            txtPrice = String.Format("{0:D}", price);

            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            for (int i = 0; i < txtPrice.Length; i++)
            {
                System.Threading.Thread.Sleep(25);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0x2, 0);
            }
            logger.Info("\tEND   input PRICE");

            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            logger.DebugFormat("BUTTON[出价]({0}, {1})", givePrice.button.x, givePrice.button.y);
            System.Threading.Thread.Sleep(50);
            ScreenUtil.SetCursorPos(givePrice.button.x, givePrice.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");
        }

        private Boolean submit(String URL, SubmitPrice submitPoints)
        {
            logger.Info("BEGIN giveCAPTCHA");
            logger.Info("\tBEGIN make INPUT blank");
            logger.DebugFormat("INPUT BOX({0}, {1})", submitPoints.inputBox.x, submitPoints.inputBox.y);
            ScreenUtil.SetCursorPos(submitPoints.inputBox.x, submitPoints.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);

            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            logger.Info("\tEND   make INPUT blank");

            logger.Info("\tBEGIN identify CAPTCHA...");
            byte[] binaryCaptcha = null;
            Boolean isLoading = true;
            int retry = 0;
            Thread.Sleep(1000);//等待1秒钟（等待验证码或者“正在获取验证码”字样出来
            logger.DebugFormat("CAPTURE CAPTCHA({0}, {1})", submitPoints.captcha[0].x, submitPoints.captcha[0].y);
            while (isLoading)//重试3.5秒钟
            {   
                binaryCaptcha = new ScreenUtil().screenCaptureAsByte(submitPoints.captcha[0].x, submitPoints.captcha[0].y, 128, 28);
                File.WriteAllBytes(String.Format("AUTO-LOADING-{0}.BMP", retry), binaryCaptcha);
                String strLoading = this.m_orcLoading.IdentifyStringFromPic(new Bitmap(new MemoryStream(binaryCaptcha)));
                logger.InfoFormat("\t LOADING({0}) = {1}", retry++, strLoading);
                if ("正在获取校验码".Equals(strLoading))
                {
                    if (retry > 14)
                    {   
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
                }
                else
                    isLoading = false;
            }

            File.WriteAllBytes("AUTO-CAPTCHA.BMP", binaryCaptcha);
            String txtCaptcha = this.m_orcCaptcha.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(binaryCaptcha)));
            logger.DebugFormat("CAPTURE TIPS({0}, {1})", submitPoints.captcha[1].x, submitPoints.captcha[1].y);
            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(submitPoints.captcha[1].x, submitPoints.captcha[1].y, 112, 16);
            File.WriteAllBytes("AUTO-TIPS.BMP", binaryCaptcha);
            String strActive = this.m_captchaUtil.getActive(txtCaptcha, new Bitmap(new System.IO.MemoryStream(binaryTips)));
            logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}", txtCaptcha, strActive);

            logger.Info("\tBEGIN input CAPTCHA");
            {
                for (int i = 0; i < strActive.Length; i++)
                {   
                    ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0, 0);
                    ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0x2, 0);
                    System.Threading.Thread.Sleep(50);
                }
            }
            logger.Info("\tEND   input CAPTCHA");

            logger.Info("\tBEGIN click BUTTON[确定]");
            logger.DebugFormat("BUTTON[确定]({0}, {1})", submitPoints.buttons[0].x, submitPoints.buttons[0].y);
            ScreenUtil.SetCursorPos(submitPoints.buttons[0].x, submitPoints.buttons[0].y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[确定]");

            ScreenUtil.SetCursorPos(submitPoints.buttons[0].x + 188 / 2, submitPoints.buttons[0].y - 10);//确定按钮
            logger.Info("END   giveCAPTCHA");
            return true;
        }
    }
}
