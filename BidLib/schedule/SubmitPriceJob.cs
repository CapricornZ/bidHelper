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
    public class SubmitPriceJob : ISchedulerJob
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SubmitPriceJob));
        private static Object lockObj = new Object();

        private static int price = 0;
        private static BidOperation bidOperation;
        private static int executeCount = 1;
        private static Bid operation = null;

        public String EndPoint { get; set; }
        private IOrc m_orcPrice;
        private IOrc m_orcLoading;
        private CaptchaUtil m_captchaUtil;
        private IOrc m_orcCaptcha;

        public SubmitPriceJob(String endPoint, IOrc orcPrice, IOrc orcLoading, CaptchaUtil captchaUtil, IOrc orcCaptcha)
        {
            this.EndPoint = endPoint;
            this.m_orcPrice = orcPrice;
            this.m_orcLoading = orcLoading;
            this.m_captchaUtil = captchaUtil;
            this.m_orcCaptcha = orcCaptcha;
        }

        public static BidOperation getConfig()
        {
            BidOperation ops = new BidOperation();
            ops.expireTime = SubmitPriceJob.bidOperation.expireTime;
            ops.startTime = SubmitPriceJob.bidOperation.startTime;
            ops.content = SubmitPriceJob.bidOperation.content;
            ops.id = SubmitPriceJob.bidOperation.id;
            ops.price = SubmitPriceJob.bidOperation.price;
            ops.type = SubmitPriceJob.bidOperation.type;
            ops.updateTime = SubmitPriceJob.bidOperation.updateTime;
            return ops; 
        }

        public static Bid getPosition()
        {
            return SubmitPriceJob.operation;
        }

        public static Boolean setConfig(int Price, BidOperation operation)
        {
            logger.Info("setConfig {...}");
            Boolean rtn = false;
            if (Monitor.TryEnter(SubmitPriceJob.lockObj, 500))
            {
                if ((null == SubmitPriceJob.bidOperation)
                    || (operation.updateTime > SubmitPriceJob.bidOperation.updateTime))//确保同一个版本（修改）的Operation只被配置并执行一次，避免多次执行
                {
                    logger.DebugFormat("PRICE     : {0}", operation.price);
                    logger.DebugFormat("startTime : {0}", operation.startTime);
                    logger.DebugFormat("expireTime: {0}", operation.expireTime);

                    SubmitPriceJob.price = Price;
                    SubmitPriceJob.executeCount = 0;
                    SubmitPriceJob.bidOperation = operation;
                    SubmitPriceJob.operation = Newtonsoft.Json.JsonConvert.DeserializeObject<Bid>(operation.content);
                    rtn = true;
                }
                Monitor.Exit(SubmitPriceJob.lockObj);
            }
            else
            {
                logger.Error("obtain SubmitPriceJob.lockObj timeout on setConfig(...)");
            }
            return rtn;
        }

        public static Boolean setConfig(BidOperation operation)
        {
            logger.Info("setConfig {...}");
            Boolean rtn = false;
            if (Monitor.TryEnter(SubmitPriceJob.lockObj, 500))
            {
                if ((null == SubmitPriceJob.bidOperation) 
                    || (operation.updateTime > SubmitPriceJob.bidOperation.updateTime))//确保同一个版本（修改）的Operation只被配置并执行一次，避免多次执行
                {
                    logger.DebugFormat("PRICE     : {0}", operation.price);
                    logger.DebugFormat("startTime : {0}", operation.startTime);
                    logger.DebugFormat("expireTime: {0}", operation.expireTime);

                    SubmitPriceJob.price = 0;
                    SubmitPriceJob.executeCount = 0;
                    SubmitPriceJob.bidOperation = operation;
                    SubmitPriceJob.operation = Newtonsoft.Json.JsonConvert.DeserializeObject<Bid>(operation.content);
                    rtn = true;
                }
                Monitor.Exit(SubmitPriceJob.lockObj);
            }
            else
            {
                logger.Error("obtain SubmitPriceJob.lockObj timeout on setConfig(...)");
            }
            return rtn;
        }

        public void Execute()
        {
            DateTime now = DateTime.Now;
            if (null == SubmitPriceJob.bidOperation)
                logger.Debug(String.Format("{{Count:{0}}}", SubmitPriceJob.executeCount));
            else
                logger.Debug(String.Format("{{Start:{0}, Expire:{1}, Count:{2}}}", 
                    SubmitPriceJob.bidOperation.startTime, SubmitPriceJob.bidOperation.expireTime,
                    SubmitPriceJob.executeCount));

            if (Monitor.TryEnter(SubmitPriceJob.lockObj, 500))
            {
                if (null != SubmitPriceJob.bidOperation && 
                    now >= SubmitPriceJob.bidOperation.startTime && now <= SubmitPriceJob.bidOperation.expireTime && SubmitPriceJob.executeCount == 0)
                {
                    //TODO:这里可以加入逻辑，如果this.submit成功，SubmitPriceJob.executeCount++。
                    //这样在下一秒可以自动执行一次未成功的出价。但是DeltaPrice应该-=100，同时需要保证DeltaPrice>=+300
                    SubmitPriceJob.executeCount++;
                    logger.Debug("trigger Fired");

                    Boolean success = false;
                    int submitCount = 0;
                    while (!success && submitCount < 3) {//1次出价，2次重试

                        submitCount++;
                        if (SubmitPriceJob.price != 0)
                            this.givePrice(SubmitPriceJob.operation.give, price: SubmitPriceJob.price);//出价;
                        else{
                            int delta = SubmitPriceJob.bidOperation.price > 300 ? 
                                SubmitPriceJob.bidOperation.price-(submitCount-1)*100 : SubmitPriceJob.bidOperation.price;
                            this.giveDeltaPrice(SubmitPriceJob.operation.give, delta: delta);//出价
                        }
                        success = this.submit(this.EndPoint, SubmitPriceJob.operation.submit);//提交
                    }
                }

                Monitor.Exit(SubmitPriceJob.lockObj);
            }
            else
            {
                logger.Error("obtain SubmitPriceJob.lockObj timeout on Execute(...)");
            }
        }

        /// <summary>
        /// 绝对价格，出价
        /// </summary>
        /// <param name="givePrice">坐标</param>
        /// <param name="price">绝对价</param>
        private void givePrice(GivePrice givePrice, int price)
        {
            logger.InfoFormat("BEGIN givePRICE(price : {0})", price);
            logger.Info("\tBEGIN identify PRICE...");
            String txtPrice = String.Format("{0:D}", price);
            logger.InfoFormat("\tEND   identified PRICE = {0}", txtPrice);

            //INPUT BOX
            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            ScreenUtil.SetCursorPos(givePrice.inputBox.x, givePrice.inputBox.y);
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

            for (int i = 0; i < txtPrice.Length; i++)
            {
                System.Threading.Thread.Sleep(50);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
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

        /// <summary>
        /// 获取当前价格，+delta，出价
        /// </summary>
        /// <param name="givePrice">坐标</param>
        /// <param name="delta">差价</param>
        private void giveDeltaPrice(GivePrice givePrice, int delta)
        {
            logger.InfoFormat("BEGIN givePRICE(delta : {0})", delta);
            logger.Info("\tBEGIN identify PRICE...");
            byte[] content = new ScreenUtil().screenCaptureAsByte(givePrice.price.x, givePrice.price.y, 52, 18);
            String txtPrice = this.m_orcPrice.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
            int price = Int32.Parse(txtPrice);
            price += delta;
            txtPrice = String.Format("{0:D}", price);
            logger.InfoFormat("\tEND   identified PRICE = {0}", txtPrice);

            //INPUT BOX
            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            ScreenUtil.SetCursorPos(givePrice.inputBox.x, givePrice.inputBox.y);
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

            for (int i = 0; i < txtPrice.Length; i++)
            {
                System.Threading.Thread.Sleep(50);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
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

        private Boolean submit(String URL, SubmitPrice submitPoints)
        {
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
            while (isLoading)//重试1秒钟
            {
                logger.InfoFormat("\t try LOADING = {0}", retry++);
                binaryCaptcha = new ScreenUtil().screenCaptureAsByte(submitPoints.captcha[0].x, submitPoints.captcha[0].y, 128, 28);
                File.WriteAllBytes(String.Format("AUTO-LOADING-{0}.BMP", retry), binaryCaptcha);
                String strLoading = this.m_orcLoading.IdentifyStringFromPic(new Bitmap(new MemoryStream(binaryCaptcha)));
                logger.InfoFormat("\t LOADING = {0}", strLoading);
                if ("正在获取校验码".Equals(strLoading))
                {
                    if (retry > 2)
                    {//重试0,1,2,3都在获取校验码
                        logger.InfoFormat("Loading captcha timeout. Abort close & re-open");
                        ScreenUtil.SetCursorPos(submitPoints.buttons[0].x + 188, submitPoints.buttons[0].y);//取消按钮
                        ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                        return false;//放弃本次出价
                    }
                    Thread.Sleep(250);
                }
                else
                    isLoading = false;
            }

            //logger.Info("\t\tBEGIN post CAPTACH");
            //String txtCaptcha = new HttpUtil().postByteAsFile(URL + "/receive/captcha.do", binaryCaptcha);
            //logger.Info("\t\tEND   post CAPTACH");
            File.WriteAllBytes("AUTO-CAPTCHA.BMP", binaryCaptcha);
            String txtCaptcha = this.m_orcCaptcha.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(binaryCaptcha)));
            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(submitPoints.captcha[1].x, submitPoints.captcha[1].y, 112, 16);
            File.WriteAllBytes("AUTO-TIPS.BMP", binaryCaptcha);
            String strActive = this.m_captchaUtil.getActive(txtCaptcha, new Bitmap(new System.IO.MemoryStream(binaryTips)));
            logger.InfoFormat("\tEND   identified CAPTCHA = {0}, ACTIVE = {1}", txtCaptcha, strActive);

            logger.Info("\tBEGIN input CAPTCHA");
            {
                for (int i = 0; i < strActive.Length; i++)
                {
                    ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
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
