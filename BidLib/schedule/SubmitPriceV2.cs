using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tobid.rest;
using System.Threading;
using System.Drawing;
using tobid.util;
using tobid.rest.position;
using System.IO;

namespace tobid.scheduler.jobs
{
    public class SubmitPriceV2Job : ISchedulerJob
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SubmitPriceV2Job));
        private static Object lockObj = new Object();
        private static int executeCount = 1;
        private static int delay;

        private IRepository orcRepository;
        private INotify notify;
        

        public SubmitPriceV2Job(IRepository repository, INotify notify)
        {
            this.orcRepository = repository;
            this.notify = notify;
        }

        public static Boolean setConfig(Step2Operation operation, int delay, Boolean updatePos = true)
        {

            logger.Info("setConfig {...}");
            Boolean rtn = false;
            if (Monitor.TryEnter(SubmitPriceV2Job.lockObj, 500))
            {
                {
                    logger.DebugFormat("PRICE     : {0}", operation.price);
                    logger.DebugFormat("DELAY     : {0}", delay);
                    //logger.DebugFormat("startTime : {0}", operation.startTime);
                    //logger.DebugFormat("expireTime: {0}", operation.expireTime);

                    SubmitPriceV2Job.delay = delay;
                    SubmitPriceV2Job.executeCount = 0;
                    SubmitPriceStep2Job.bidOperation = operation;
                    //if (updatePos)
                    //    SubmitPriceV2.operation = Newtonsoft.Json.JsonConvert.DeserializeObject<BidStep2>(operation.content);
                    rtn = true;
                }
                Monitor.Exit(SubmitPriceV2Job.lockObj);
            }
            else
                logger.Error("obtain SubmitPriceV2.lockObj timeout on setConfig(...)");

            return rtn;
        }

        public void Fire(int delta)
        {
            //TODO:这里可以加入逻辑，如果this.submit成功，SubmitPriceJob.executeCount++。
            //这样在下一秒可以自动执行一次未成功的出价。但是DeltaPrice应该-=100，同时需要保证DeltaPrice>=+300
            logger.Warn("trigger Fired");

            Point origin = IEUtil.findOrigin();
            Position pos = new Position(origin.X, origin.Y);

            Boolean success = false;
            int submitCount = 0;
            //while (!success && submitCount < 2)
            {//1次出价，1次重试

                //BidStep2 operation = Newtonsoft.Json.JsonConvert.DeserializeObject<BidStep2>(SubmitPriceStep2Job.bidOperation.content);
                BidStep2 operation = SubmitPriceStep2Job.getPosition();
                submitCount++;
                string givePrice = "";
                if (delta == 0)
                    givePrice = this.giveDeltaPrice(pos, operation.give, delta: submitCount == 1 ? SubmitPriceStep2Job.bidOperation.price : 300);//出价
                else
                    givePrice = this.giveDeltaPrice(pos, operation.give, delta: delta);//出价

                byte[] binaryCaptcha = null;
                Boolean isLoading = true;
                Boolean typeCaptcha = false;
                Boolean stop = false;
                int retry = 0;
                for (int idle = 0; !stop && idle < SubmitPriceV2Job.delay; idle += 100)
                {
                    Thread.Sleep(100);
                    byte[] binaryPrice = new ScreenUtil().screenCaptureAsByte(pos.x + operation.price.x, pos.y + operation.price.y, 48, 12);
                    string price = this.orcRepository.orcPriceSM.IdentifyStringFromPic(new Bitmap(new MemoryStream(binaryPrice)));
                    logger.Info("PRICE:" + price);

                    int delta1 = Int32.Parse(givePrice) - Int32.Parse(price);

                    if (idle > 500 && isLoading)
                    {
                        logger.Info("\tBEGIN identify CAPTCHA...");
                        {
                            logger.DebugFormat("CAPTURE CAPTCHA({0}, {1})", pos.x + operation.submit.captcha[0].x, pos.y + operation.submit.captcha[0].y);
                            binaryCaptcha = new ScreenUtil().screenCaptureAsByte(pos.x + operation.submit.captcha[0].x, pos.y + operation.submit.captcha[0].y, 128, 28);
                            File.WriteAllBytes(String.Format("AUTO-LOADING-{0}.BMP", retry), binaryCaptcha);
                            Bitmap bitMap = new Bitmap(new MemoryStream(binaryCaptcha));
                            String strLoading = this.orcRepository.orcCaptchaLoading.IdentifyStringFromPic(bitMap);
                            logger.InfoFormat("\t LOADING({0}) = {1}", retry++, strLoading);
                            if (!"正在获取校验码".Equals(strLoading))
                            {
                                isLoading = false;

                                File.WriteAllBytes("AUTO-CAPTCHA.BMP", binaryCaptcha);
                                String txtCaptcha = this.orcRepository.orcCaptcha.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(binaryCaptcha)));
                                logger.DebugFormat("CAPTURE TIPS({0}, {1})", pos.x + operation.submit.captcha[1].x, pos.y + operation.submit.captcha[1].y);
                                byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(pos.x + operation.submit.captcha[1].x, pos.y + operation.submit.captcha[1].y, 112, 16);
                                File.WriteAllBytes("AUTO-TIPS.BMP", binaryCaptcha);
                                String strActive = this.orcRepository.orcCaptchaTipsUtil.getActive(txtCaptcha, new Bitmap(new System.IO.MemoryStream(binaryTips)));
                                logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}", txtCaptcha, strActive);

                                logger.Info("\tBEGIN input CAPTCHA");
                                {
                                    typeCaptcha = true;
                                    for (int i = 0; i < strActive.Length; i++)
                                    {
                                        System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0, 0);
                                        KeyBoardUtil.sendKeyDown(strActive[i].ToString());
                                        System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0x2, 0);
                                        KeyBoardUtil.sendKeyUp(strActive[i].ToString());
                                    }
                                } System.Threading.Thread.Sleep(50);
                                logger.Info("\tEND   input CAPTCHA");
                            }
                        }
                        logger.Info("\tEND identify CAPTCHA...");
                    }

                    stop = typeCaptcha && delta1 <= 400;
                }
                //success = this.submit(pos, operation.submit);//提交
                logger.Info("\tBEGIN click BUTTON[确定]");
                logger.DebugFormat("BUTTON[确定]({0}, {1})", pos.x + operation.submit.buttons[0].x, pos.y + operation.submit.buttons[0].y);
                ScreenUtil.SetCursorPos(pos.x + operation.submit.buttons[0].x, pos.y + operation.submit.buttons[0].y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                logger.Info("\tEND   click BUTTON[确定]");

                logger.WarnFormat("ROUND[{0}] {1}", submitCount, success ? "SUCCESS" : "FAILED");
            }
        }

        public void Execute()
        {
            DateTime now = DateTime.Now;
            if (null == SubmitPriceStep2Job.bidOperation)
                logger.Error(String.Format("{{Count:{0}}}", SubmitPriceV2Job.executeCount));
            else
            {
                //TimeSpan diff = SubmitPriceStep2Job.bidOperation.startTime - now;
                //if (SubmitPriceV2Job.executeCount == 0)
                //    this.notify.acceptMessage(String.Format("SECS:{0}", (int)diff.TotalSeconds));
            }

            if (Monitor.TryEnter(SubmitPriceV2Job.lockObj, 500))
            {
                if (null != SubmitPriceStep2Job.bidOperation)
                    //&& now >= SubmitPriceStep2Job.bidOperation.startTime && now <= SubmitPriceStep2Job.bidOperation.expireTime && SubmitPriceV2Job.executeCount == 0)
                {
                    SubmitPriceV2Job.executeCount++;
                    this.Fire(0);
                }
                Monitor.Exit(SubmitPriceV2Job.lockObj);
            }
            else
                logger.Error("obtain SubmitPriceV2.lockObj timeout on Execute(...)");
        }

        /// <summary>
        /// 获取当前价格，+delta，出价
        /// </summary>
        /// <param name="givePrice">坐标</param>
        /// <param name="delta">差价</param>
        private string giveDeltaPrice(Position origin, GivePriceStep2 givePrice,  int delta)
        {
            int x = origin.x;
            int y = origin.y;
            logger.InfoFormat("BEGIN givePRICE(delta : {0})", delta);
            //INPUT BOX

            logger.DebugFormat("CAPTURE PRICE({0}, {1})", x + givePrice.price.x, y + givePrice.price.y);
            byte[] content = new ScreenUtil().screenCaptureAsByte(x + givePrice.price.x, y + givePrice.price.y, 52, 18);

            logger.Info("\tBEGIN make PRICE blank...");
            logger.DebugFormat("INPUT BOX({0}, {1})", x + givePrice.inputBox.x, y + givePrice.inputBox.y);
            ScreenUtil.SetCursorPos(x + givePrice.inputBox.x, y + givePrice.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(50);

            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);

            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            logger.Info("\tEND   make PRICE blank...");

            logger.Info("\tBEGIN identify PRICE...");
            String txtPrice = this.orcRepository.orcPrice.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
            int price = Int32.Parse(txtPrice) + delta;
            logger.InfoFormat("\tEND   identified PRICE = {0}", txtPrice);
            txtPrice = String.Format("{0:D}", price);

            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            for (int i = 0; i < txtPrice.Length; i++)
            {
                System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
                KeyBoardUtil.sendKeyDown(txtPrice[i].ToString());
                System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0x2, 0);
                KeyBoardUtil.sendKeyUp(txtPrice[i].ToString());
            }
            logger.Info("\tEND   input PRICE");

            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            logger.DebugFormat("BUTTON[出价]({0}, {1})", x + givePrice.button.x, y + givePrice.button.y);
            System.Threading.Thread.Sleep(50);
            ScreenUtil.SetCursorPos(x + givePrice.button.x, y + givePrice.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");

            return txtPrice;
        }

        public Boolean submit(Position origin, SubmitPrice submitPoints)
        {
            int x = origin.x;
            int y = origin.y;

            logger.Info("BEGIN giveCAPTCHA");
            logger.Info("\tBEGIN make INPUT blank");
            logger.DebugFormat("INPUT BOX({0}, {1})", x + submitPoints.inputBox.x, y + submitPoints.inputBox.y);
            ScreenUtil.SetCursorPos(x + submitPoints.inputBox.x, y + submitPoints.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(50);

            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);

            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(this.orcRepository.interval); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            logger.Info("\tEND   make INPUT blank");

            logger.Info("\tBEGIN identify CAPTCHA...");
            byte[] binaryCaptcha = null;
            Boolean isLoading = true;
            int retry = 0;
            Thread.Sleep(500);//等待0.5秒钟（等待验证码或者“正在获取验证码”字样出来
            logger.DebugFormat("CAPTURE CAPTCHA({0}, {1})", x + submitPoints.captcha[0].x, y + submitPoints.captcha[0].y);
            while (isLoading)//重试3.5秒钟
            {
                binaryCaptcha = new ScreenUtil().screenCaptureAsByte(x + submitPoints.captcha[0].x, y + submitPoints.captcha[0].y, 128, 28);
                File.WriteAllBytes(String.Format("AUTO-LOADING-{0}.BMP", retry), binaryCaptcha);
                Bitmap bitMap = new Bitmap(new MemoryStream(binaryCaptcha));
                String strLoading = this.orcRepository.orcCaptchaLoading.IdentifyStringFromPic(bitMap);
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
                        ScreenUtil.SetCursorPos(x + submitPoints.buttons[0].x + 188, y + submitPoints.buttons[0].y);//取消按钮
                        ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                        return false;//放弃本次出价
                    }
                    Thread.Sleep(250);
                }
                else
                    isLoading = false;
            }

            File.WriteAllBytes("AUTO-CAPTCHA.BMP", binaryCaptcha);
            String txtCaptcha = this.orcRepository.orcCaptcha.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(binaryCaptcha)));
            logger.DebugFormat("CAPTURE TIPS({0}, {1})", x + submitPoints.captcha[1].x, y + submitPoints.captcha[1].y);
            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(x + submitPoints.captcha[1].x, y + submitPoints.captcha[1].y, 112, 16);
            File.WriteAllBytes("AUTO-TIPS.BMP", binaryCaptcha);
            String strActive = this.orcRepository.orcCaptchaTipsUtil.getActive(txtCaptcha, new Bitmap(new System.IO.MemoryStream(binaryTips)));
            logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}", txtCaptcha, strActive);

            logger.Info("\tBEGIN input CAPTCHA");
            {
                for (int i = 0; i < strActive.Length; i++)
                {
                    System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0, 0);
                    KeyBoardUtil.sendKeyDown(strActive[i].ToString());
                    System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0x2, 0);
                    KeyBoardUtil.sendKeyUp(strActive[i].ToString());
                }
            } System.Threading.Thread.Sleep(50);
            logger.Info("\tEND   input CAPTCHA");

            //logger.Info("\tBEGIN click BUTTON[确定]");
            //logger.DebugFormat("BUTTON[确定]({0}, {1})", x + submitPoints.buttons[0].x, y + submitPoints.buttons[0].y);
            //ScreenUtil.SetCursorPos(x + submitPoints.buttons[0].x, y + submitPoints.buttons[0].y);
            //ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            //logger.Info("\tEND   click BUTTON[确定]");

            logger.Info("END   giveCAPTCHA");
            return true;
        }
    }
}
