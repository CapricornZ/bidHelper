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
    public interface IRepository {
        
        /// <summary>
        /// 是否用加价功能
        /// </summary>
        Boolean deltaPriceOnUI { get; set; }
        Point TimePos { get; set; }
        IOrc orcTitle { get; }
        IOrc orcCaptcha { get; }
        IOrc orcPrice { get; }
        IOrc orcPriceSM { get; }
        IOrc orcTime { get; }
        IOrc orcCaptchaLoading { get; }
        IOrc[] orcCaptchaTip { get; }
        CaptchaUtil orcCaptchaTipsUtil { get; }

        int interval { get; }
        String endPoint { get; }
        String category { get; }
        Entry[] entries { get; }

        GivePriceStep2 givePriceStep2 { get; }
        SubmitPrice submitPrice { get; }
        BidStep2 bidStep2 { get; }

        /// <summary>
        /// 上一次提交验证码的时间
        /// </summary>
        DateTime lastSubmit { get; set; }
        /// <summary>
        /// 上一次提交验证码到别服务器接受或返回的耗时
        /// </summary>
        TimeSpan lastCost { get; set; }
        /// <summary>
        /// Operator是否空格（输完验证码）标志
        /// </summary>
        Boolean isReady { get; set; }

        /// <summary>
        /// 验证码远程接口
        /// </summary>
        String assistantEndPoint { get; set; }
        String submitCaptcha(Stream captcha, Stream tips);
    }

    public interface INotify {
        void acceptMessage(String msg);
    }

    /// <summary>
    /// SubmitPrice : 每秒检查，符合条件执行出价Action
    /// </summary>
    public class SubmitPriceStep2Job : ISchedulerJob{

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SubmitPriceStep2Job));
        private static Object lockObj = new Object();

        private static int price = 0;
        public static Step2Operation bidOperation;
        private static Boolean priceOnly;
        private static int executeCount = 1;
        private static BidStep2 operation = null;

        private IRepository orcRepository;
        private INotify notify;

        public SubmitPriceStep2Job(IRepository repository, INotify notify)
        {
            this.orcRepository = repository;
            this.notify = notify;
        }

        public static Step2Operation getConfig() {

            Step2Operation ops = new Step2Operation();
            //ops.expireTime = SubmitPriceStep2Job.bidOperation.expireTime;
            //ops.startTime = SubmitPriceStep2Job.bidOperation.startTime;
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

        public static void setPosition(BidStep2 value) {

            SubmitPriceStep2Job.operation = value;
        }

        public static Boolean setConfig(int Price, Step2Operation operation, Boolean priceOnly = false) {

            logger.Info("setConfig {...}");
            Boolean rtn = false;
            if (Monitor.TryEnter(SubmitPriceStep2Job.lockObj, 500))
            {
                if ((null == SubmitPriceStep2Job.bidOperation)
                    || (operation.updateTime > SubmitPriceStep2Job.bidOperation.updateTime))//确保同一个版本（修改）的Operation只被配置并执行一次，避免多次执行
                {
                    logger.DebugFormat("PRICE     : {0}", operation.price);
                    //logger.DebugFormat("startTime : {0}", operation.startTime);
                    //logger.DebugFormat("expireTime: {0}", operation.expireTime);

                    SubmitPriceStep2Job.priceOnly = priceOnly;
                    SubmitPriceStep2Job.price = Price;
                    SubmitPriceStep2Job.executeCount = 0;
                    SubmitPriceStep2Job.bidOperation = operation;
                    SubmitPriceStep2Job.operation = Newtonsoft.Json.JsonConvert.DeserializeObject<BidStep2>(operation.content);
                    rtn = true;
                }
                Monitor.Exit(SubmitPriceStep2Job.lockObj);
            }
            else
                logger.Warn("obtain SubmitPriceJob.lockObj timeout on setConfig(...)");
            return rtn;
        }

        public static Boolean setConfig(Step2Operation operation, Boolean priceOnly = false, Boolean updatePos = true) {

            logger.Info("setConfig {...}");
            Boolean rtn = false;
            if (Monitor.TryEnter(SubmitPriceStep2Job.lockObj, 500))
            {
                //if ((null == SubmitPriceStep2Job.bidOperation) 
                //    || (operation.updateTime > SubmitPriceStep2Job.bidOperation.updateTime))//确保同一个版本（修改）的Operation只被配置并执行一次，避免多次执行
                {
                    logger.DebugFormat("PRICE     : {0}", operation.price);
                    //logger.DebugFormat("startTime : {0}", operation.startTime);
                    //logger.DebugFormat("expireTime: {0}", operation.expireTime);

                    SubmitPriceStep2Job.priceOnly = priceOnly;
                    SubmitPriceStep2Job.price = 0;
                    SubmitPriceStep2Job.executeCount = 0;
                    SubmitPriceStep2Job.bidOperation = operation;
                    if(updatePos)
                        SubmitPriceStep2Job.operation = Newtonsoft.Json.JsonConvert.DeserializeObject<BidStep2>(operation.content);
                    rtn = true;
                }
                Monitor.Exit(SubmitPriceStep2Job.lockObj);
            }
            else
                logger.Warn("obtain SubmitPriceJob.lockObj timeout on setConfig(...)");

            return rtn;
        }

        public void Fire(int delta) {

            //TODO:这里可以加入逻辑，如果this.submit成功，SubmitPriceJob.executeCount++。
            //这样在下一秒可以自动执行一次未成功的出价。但是DeltaPrice应该-=100，同时需要保证DeltaPrice>=+300
            logger.Warn("trigger Fired");
                    
            Point origin = IEUtil.findOrigin();
            Position pos = new Position(origin.X, origin.Y);

            Boolean success = false;
            int submitCount = 0;
            while (!success && submitCount < 2) {//1次出价，1次重试

                submitCount++;
                if (SubmitPriceStep2Job.price != 0)
                    this.givePrice(pos, SubmitPriceStep2Job.operation.give, price: SubmitPriceStep2Job.price);//出价;
                else{

                    //int delta = SubmitPriceStep2Job.bidOperation.price > 300 ?
                    //    SubmitPriceStep2Job.bidOperation.price - (submitCount - 1) * 100 : SubmitPriceStep2Job.bidOperation.price;
                    if (delta == 0) {
                        this.giveDeltaPrice(pos, SubmitPriceStep2Job.operation.give, delta: submitCount == 1 ? SubmitPriceStep2Job.bidOperation.price : 300);//出价
                    } else {
                        this.giveDeltaPrice(pos, SubmitPriceStep2Job.operation.give, delta: delta);//出价
                    }
                }
                if (SubmitPriceStep2Job.priceOnly)
                    if(delta == 0)
                        success = true;
                    else
                        success = this.submit(pos, SubmitPriceStep2Job.operation.submit);//提交
                else
                    success = this.submit(pos, SubmitPriceStep2Job.operation.submit);//提交
                    
                logger.WarnFormat("ROUND[{0}] {1}", submitCount, success?"SUCCESS":"FAILED");
            }
        }

        public void Execute()
        {
            DateTime now = DateTime.Now;
            if (null == SubmitPriceStep2Job.bidOperation)
                logger.Error(String.Format("{{Count:{0}}}", SubmitPriceStep2Job.executeCount));
            else {
                //logger.Debug(String.Format("{{Start:{0}, Expire:{1}, Count:{2}}}",
                //    SubmitPriceStep2Job.bidOperation.startTime, SubmitPriceStep2Job.bidOperation.expireTime,
                //    SubmitPriceStep2Job.executeCount));
                //TimeSpan diff = SubmitPriceStep2Job.bidOperation.startTime - now;
                //if(SubmitPriceStep2Job.executeCount == 0)
                //    this.notify.acceptMessage(String.Format("SECS:{0}", (int)diff.TotalSeconds));
            }

            if (Monitor.TryEnter(SubmitPriceStep2Job.lockObj, 500))
            {
                if (null != SubmitPriceStep2Job.bidOperation)
                    //&& now >= SubmitPriceStep2Job.bidOperation.startTime && now <= SubmitPriceStep2Job.bidOperation.expireTime && SubmitPriceStep2Job.executeCount == 0)
                {
                    SubmitPriceStep2Job.executeCount++;
                    this.Fire(0);
                }

                Monitor.Exit(SubmitPriceStep2Job.lockObj);
            }
            else
                logger.Warn("obtain SubmitPriceJob.lockObj timeout on Execute(...)");

        }

        /// <summary>
        /// 绝对价格，出价
        /// </summary>
        /// <param name="givePrice">坐标</param>
        /// <param name="price">绝对价</param>
        private void givePrice(Position origin, GivePriceStep2 givePrice, int price)
        {
            int x = origin.x;
            int y = origin.y;

            logger.InfoFormat("BEGIN givePRICE(price : {0})", price);
            
            String txtPrice = String.Format("{0:D}", price);

            //INPUT BOX
            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            logger.DebugFormat("INPUT BOX({0}, {1})", x + givePrice.inputBox.x, y + givePrice.inputBox.y);
            ScreenUtil.SetCursorPos(x + givePrice.inputBox.x, y + givePrice.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(50);

            System.Windows.Forms.SendKeys.SendWait("{BACKSPACE 5}{DEL 5}");

            //for (int i = 0; i < txtPrice.Length; i++)
            //{
            //    System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
            //    KeyBoardUtil.sendKeyDown(txtPrice[i].ToString());
            //    System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0x2, 0);
            //    KeyBoardUtil.sendKeyUp(txtPrice[i].ToString());
            //}
            KeyBoardUtil.sendMessage(txtPrice, this.orcRepository.interval);
            logger.Info("\tEND   input PRICE");

            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            logger.DebugFormat("BOX[出价]({0}, {1})", x + givePrice.button.x, y + givePrice.button.y);
            System.Threading.Thread.Sleep(100);
            ScreenUtil.SetCursorPos(x + givePrice.button.x, y + givePrice.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");
        }

        /// <summary>
        /// 获取当前价格，+delta，出价
        /// </summary>
        /// <param name="givePrice">坐标</param>
        /// <param name="delta">差价</param>
        private void giveDeltaPrice(Position origin, GivePriceStep2 givePrice, int delta)
        {
            int x = origin.x;
            int y = origin.y;
            logger.InfoFormat("BEGIN givePRICE(delta : {0})", delta);
            //INPUT BOX

            if (this.orcRepository.deltaPriceOnUI && givePrice.delta != null)
            {
                logger.Info("\tBEGIN make delta PRICE blank...");
                ScreenUtil.SetCursorPos(x + givePrice.delta.inputBox.x, y + givePrice.delta.inputBox.y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                logger.Info("\tEND   make delta PRICE blank...");

                logger.Info("\tBEGIN input delta PRICE...");
                KeyBoardUtil.sendMessage(Convert.ToString(delta), this.orcRepository.interval, needClean:true);
                logger.Info("\tEND   input delta PRICE...");
                System.Threading.Thread.Sleep(100);

                logger.Info("\tBEGIN click delta PRICE button");
                ScreenUtil.SetCursorPos(x + givePrice.delta.button.x, y + givePrice.delta.button.y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                logger.Info("\tEND   click delta PRICE button");
            }
            else
            {
                logger.DebugFormat("CAPTURE PRICE({0}, {1})", x + givePrice.price.x, y + givePrice.price.y);
                byte[] content = new ScreenUtil().screenCaptureAsByte(x + givePrice.price.x, y + givePrice.price.y, 52, 18);

                logger.Info("\tBEGIN make PRICE blank...");
                logger.DebugFormat("\t\tINPUT BOX({0}, {1})", x + givePrice.inputBox.x, y + givePrice.inputBox.y);
                ScreenUtil.SetCursorPos(x + givePrice.inputBox.x, y + givePrice.inputBox.y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                System.Threading.Thread.Sleep(50);

                //System.Windows.Forms.SendKeys.SendWait("{BACKSPACE 5}{DEL 5}");
                //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
                //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
                //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
                //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
                //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);

                //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
                //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
                //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
                //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
                //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
                logger.Info("\tEND   make PRICE blank...");

                logger.Info("\tBEGIN identify PRICE...");
                String txtPrice = this.orcRepository.orcPrice.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
                int price = Int32.Parse(txtPrice) + delta;
                logger.InfoFormat("\tEND identified PRICE = {0}", txtPrice);
                txtPrice = String.Format("{0:D}", price);

                logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
                //for (int i = 0; i < txtPrice.Length; i++)
                //{
                //    System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
                //    KeyBoardUtil.sendKeyDown(txtPrice[i].ToString());
                //    System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0x2, 0);
                //    KeyBoardUtil.sendKeyUp(txtPrice[i].ToString());
                //}
                KeyBoardUtil.sendMessage(txtPrice, this.orcRepository.interval, needClean:true);
                logger.Info("\tEND   input PRICE");
            }

            System.Threading.Thread.Sleep(50);
            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            logger.DebugFormat("\t\tBUTTON[出价]({0}, {1})", x + givePrice.button.x, y + givePrice.button.y);
            System.Threading.Thread.Sleep(100);
            ScreenUtil.SetCursorPos(x + givePrice.button.x, y + givePrice.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");
        }

        public Boolean submit(Position origin, SubmitPrice submitPoints)
        {
            int x = origin.x;
            int y = origin.y;

            logger.Info("BEGIN giveCAPTCHA");
            logger.Info("\tBEGIN make INPUT blank");
            logger.DebugFormat("\t\tINPUT BOX({0}, {1})", x + submitPoints.inputBox.x, y + submitPoints.inputBox.y);
            ScreenUtil.SetCursorPos(x + submitPoints.inputBox.x, y + submitPoints.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(50);

            //System.Windows.Forms.SendKeys.SendWait("{BACKSPACE 4}{DEL 4}");
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);

            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            logger.Info("\tEND   make INPUT blank");

            logger.Info("\tBEGIN identify CAPTCHA...");
            byte[] binaryCaptcha = null;
            Boolean isLoading = true;
            int retry = 0;
            Thread.Sleep(500);//等待0.5秒钟（等待验证码或者“正在获取验证码”字样出来
            logger.DebugFormat("\t\tCAPTURE CAPTCHA({0}, {1})", x+submitPoints.captcha[0].x, y+submitPoints.captcha[0].y);
            while (isLoading)//重试3.5秒钟
            {   
                binaryCaptcha = new ScreenUtil().screenCaptureAsByte(x + submitPoints.captcha[0].x, y + submitPoints.captcha[0].y, 128, 28);
                File.WriteAllBytes(String.Format("AUTO-LOADING-{0}.BMP", retry), binaryCaptcha);
                Bitmap bitMap = new Bitmap(new MemoryStream(binaryCaptcha));
                //if (this.orcRepository.orcCaptchaLoading.IsBlank(bitMap))
                //{
                //    logger.Debug("BLANK");
                //    continue;
                //}
                String strLoading = this.orcRepository.orcCaptchaLoading.IdentifyStringFromPic(bitMap);
                logger.InfoFormat("\t\t LOADING({0}) = {1}", retry++, strLoading);
                if ("正在获取校验码".Equals(strLoading))
                {
                    if (retry > 14)
                    {   
                        //重试1,2,3,4     ->1秒
                        //重试5,6,7,8     ->2秒
                        //重试9,10,11,12  ->3秒
                        //重试13,14       ->0.5秒
                        //都在获取校验码
                        logger.InfoFormat("\t\tLoading captcha timeout. 放弃 close & re-open");
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
            logger.DebugFormat("\t\tCAPTURE TIPS({0}, {1})", x + submitPoints.captcha[1].x, y + submitPoints.captcha[1].y);
            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(x + submitPoints.captcha[1].x, y + submitPoints.captcha[1].y, 112, 16);
            File.WriteAllBytes("AUTO-TIPS.BMP", binaryCaptcha);
            String strActive = this.orcRepository.orcCaptchaTipsUtil.getActive(txtCaptcha, new Bitmap(new System.IO.MemoryStream(binaryTips)));
            logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}", txtCaptcha, strActive);

            logger.Info("\tBEGIN input CAPTCHA");
            {
                //for (int i = 0; i < strActive.Length; i++)
                //{
                //    System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0, 0);
                //    KeyBoardUtil.sendKeyDown(strActive[i].ToString());
                //    System.Threading.Thread.Sleep(this.orcRepository.interval); //ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0x2, 0);
                //    KeyBoardUtil.sendKeyUp(strActive[i].ToString());
                //}
                KeyBoardUtil.sendMessage(strActive, this.orcRepository.interval, needClean:true);
            } System.Threading.Thread.Sleep(100);
            logger.Info("\tEND   input CAPTCHA");

            logger.Info("\tBEGIN click BUTTON[确定]");
            logger.DebugFormat("BUTTON[确定]({0}, {1})", x + submitPoints.buttons[0].x, y + submitPoints.buttons[0].y);
            ScreenUtil.SetCursorPos(x + submitPoints.buttons[0].x, y + submitPoints.buttons[0].y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[确定]");

            logger.Info("END   giveCAPTCHA");
            return true;
        }
    }
}
