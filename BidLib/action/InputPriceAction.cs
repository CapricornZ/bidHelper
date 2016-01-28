using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tobid.util;
using System.Drawing;
using tobid.rest.position;

namespace tobid.scheduler.jobs.action {
    
    /// <summary>
    /// 输入Delta价格
    /// </summary>
    public class InputPriceAction : IBidAction {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(InputPriceAction));

        private int delta;
        private IRepository repository;
        public InputPriceAction(int delta, IRepository repo) {
            this.delta = delta;
            this.repository = repo;
        }

        public int BasePrice { get; set; }

        public void notify(String message) { logger.Debug(message); }

        public bool execute() {

            System.Drawing.Point origin = IEUtil.findOrigin();
            int x = origin.X;
            int y = origin.Y;
            GivePriceStep2 givePrice = this.repository.givePriceStep2;

            logger.InfoFormat("BEGIN givePRICE(delta : {0})", delta);

            int price = 0;
            if (this.repository.deltaPriceOnUI && givePrice.delta != null){

                logger.DebugFormat("CAPTURE PRICE({0}, {1})", x + givePrice.price.x, y + givePrice.price.y);
                byte[] content = new ScreenUtil().screenCaptureAsByte(x + givePrice.price.x, y + givePrice.price.y, givePrice.price.width, givePrice.price.height);

                logger.Info("\tBEGIN make delta PRICE blank...");
                ScreenUtil.SetCursorPos(x + givePrice.delta.inputBox.x, y + givePrice.delta.inputBox.y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                logger.Info("\tEND   make delta PRICE blank...");

                String txtPrice = this.repository.orcPrice.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
                price = Int32.Parse(txtPrice) + delta;

                logger.Info("\tBEGIN input delta PRICE...");
                KeyBoardUtil.sendMessage(Convert.ToString(delta), interval:this.repository.interval, needClean:true);
                logger.Info("\tEND   input delta PRICE...");
                System.Threading.Thread.Sleep(100);

                logger.Info("\tBEGIN click delta PRICE button");
                ScreenUtil.SetCursorPos(x + givePrice.delta.button.x, y + givePrice.delta.button.y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                logger.Info("\tEND   click delta PRICE button");

            } else {

                logger.DebugFormat("CAPTURE PRICE({0}, {1})", x + givePrice.price.x, y + givePrice.price.y);
                byte[] content = new ScreenUtil().screenCaptureAsByte(x + givePrice.price.x, y + givePrice.price.y, givePrice.price.width, givePrice.price.height);

                logger.Info("\tBEGIN make PRICE blank...");
                logger.DebugFormat("\t\tINPUT BOX({0}, {1})", x + givePrice.inputBox.x, y + givePrice.inputBox.y);
                ScreenUtil.SetCursorPos(x + givePrice.inputBox.x, y + givePrice.inputBox.y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                logger.Info("\tEND   make PRICE blank...");

                logger.Info("\tBEGIN identify PRICE...");
                String txtPrice = this.repository.orcPrice.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
                price = Int32.Parse(txtPrice) + delta;
                logger.InfoFormat("\tEND identified PRICE = {0}", txtPrice);
                txtPrice = String.Format("{0:D}", price);

                logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
                KeyBoardUtil.sendMessage(txtPrice, interval:this.repository.interval, needClean:true);
                logger.Info("\tEND   input PRICE");
            }
            this.BasePrice = price;

            this.repository.isReady = false;//等待用户输入验证码
            System.Threading.Thread.Sleep(50);

            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            logger.DebugFormat("\t\tBUTTON[出价]({0}, {1})", x + givePrice.button.x, y + givePrice.button.y);
            ScreenUtil.SetCursorPos(x + givePrice.button.x, y + givePrice.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");

            System.Threading.Thread capture = new System.Threading.Thread(delegate() {

                if (!System.IO.Directory.Exists("Captchas"))
                    System.IO.Directory.CreateDirectory("Captchas");

                SubmitPrice submitPrice = this.repository.submitPrice;
                ScreenUtil screen = new ScreenUtil();

                try {
                    for (int i = 0; i < 8; i++) {
                        String fileName = DateTime.Now.ToString("MMdd-HHmmss-fff");
                        byte[] captcha = screen.screenCaptureAsByte(x + submitPrice.captcha[0].x, y + submitPrice.captcha[0].y, submitPrice.captcha[0].width, submitPrice.captcha[0].height);
                        System.IO.File.WriteAllBytes(String.Format("Captchas/Auto-Captcha-{0}.bmp", fileName), captcha);
                        byte[] captchaTip = screen.screenCaptureAsByte(x + submitPrice.captcha[1].x, y + submitPrice.captcha[1].y, submitPrice.captcha[1].width, submitPrice.captcha[1].height);
                        System.IO.File.WriteAllBytes(String.Format("Captchas/Auto-Captcha-TIP-{0}.bmp", fileName), captchaTip);
                        System.Threading.Thread.Sleep(500);
                    }
                }
                catch (Exception ex){
                    System.Console.WriteLine(ex.ToString());
                }
            });
            capture.Start();
            return true;
        }
    }
}
