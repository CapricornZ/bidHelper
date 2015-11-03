using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tobid.util;
using System.Drawing;
using tobid.rest.position;

namespace tobid.scheduler.jobs.action {
    
    public class InputPriceAction : IBidAction {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(InputPriceAction));

        private int delta;
        private IRepository repository;
        public InputPriceAction(int delta, IRepository repo) {
            this.delta = delta;
            this.repository = repo;
        }

        public void notify(String message) {
            logger.Debug(message);
        }

        public bool execute() {

            System.Drawing.Point origin = IEUtil.findOrigin();
            int x = origin.X;
            int y = origin.Y;
            GivePriceStep2 givePrice = this.repository.givePriceStep2;

            logger.InfoFormat("BEGIN givePRICE(delta : {0})", delta);

            logger.DebugFormat("CAPTURE PRICE({0}, {1})", x + givePrice.price.x, y + givePrice.price.y);
            byte[] content = new ScreenUtil().screenCaptureAsByte(x + givePrice.price.x, y + givePrice.price.y, 52, 18);

            logger.Info("\tBEGIN make PRICE blank...");
            logger.DebugFormat("\t\tINPUT BOX({0}, {1})", x + givePrice.inputBox.x, y + givePrice.inputBox.y);
            ScreenUtil.SetCursorPos(x + givePrice.inputBox.x, y + givePrice.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(50);

            System.Windows.Forms.SendKeys.SendWait("{BACKSPACE 5}");
            System.Windows.Forms.SendKeys.SendWait("{DEL 5}");
            logger.Info("\tEND   make PRICE blank...");

            logger.Info("\tBEGIN identify PRICE...");
            String txtPrice = this.repository.orcPrice.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
            int price = Int32.Parse(txtPrice) + delta;
            logger.InfoFormat("\tEND identified PRICE = {0}", txtPrice);
            txtPrice = String.Format("{0:D}", price);

            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            KeyBoardUtil.sendMessage(txtPrice, this.repository.interval);
            logger.Info("\tEND   input PRICE");

            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            logger.DebugFormat("\t\tBUTTON[出价]({0}, {1})", x + givePrice.button.x, y + givePrice.button.y);
            System.Threading.Thread.Sleep(100);
            ScreenUtil.SetCursorPos(x + givePrice.button.x, y + givePrice.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");

            return true;
        }
    }
}
