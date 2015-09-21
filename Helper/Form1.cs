using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;

using tobid.rest;
using tobid.util;
using tobid.util.http;
using tobid.util.orc;
using tobid.scheduler;
using tobid.scheduler.jobs;

namespace Helper
{
    enum CaptchaInput {
        LEFT, MIDDLE, RIGHT,
        AUTO
    }

    public partial class Form1 : Form, IRepository
    {
        public Form1(){

            InitializeComponent();
        }

        public Form1(String endPoint){

            this.EndPoint = endPoint;
            InitializeComponent();
        }

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Form1));
        private String EndPoint { get; set; }

        #region IRepository
        public String endPoint { get { return this.EndPoint; } }
        public IOrc orcTitle { get { return this.m_orcTitle; } }
        public IOrc orcCaptcha { get { return this.m_orcCaptcha; } }
        public IOrc orcPrice { get { return this.m_orcPrice; } }
        public IOrc orcCaptchaLoading { get { return this.m_orcCaptchaLoading; } }
        public IOrc[] orcCaptchaTip { get { return this.m_orcCaptchaTip; } }
        public CaptchaUtil orcCaptchaTipsUtil { get { return this.m_orcCaptchaTipsUtil;} }
        #endregion

        private IOrc m_orcTitle;
        private IOrc m_orcCaptcha;
        private IOrc m_orcPrice;
        private IOrc m_orcCaptchaLoading;
        private IOrc[] m_orcCaptchaTip;
        private CaptchaUtil m_orcCaptchaTipsUtil;

        private Scheduler m_schedulerKeepAlive;
        private Scheduler m_schedulerSubmitStep2;

        private System.Threading.Thread keepAliveThread;
        private System.Threading.Thread submitPriceStep2Thread;

        private Step2ConfigDialog step2Dialog = new Step2ConfigDialog();

        private void Form1_Activated(object sender, EventArgs e) {

            this.textInputPrice.Focus();
            logger.Info("Application Form Activated");
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {

            if (null != this.keepAliveThread && (this.keepAliveThread.ThreadState == System.Threading.ThreadState.Running || this.keepAliveThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin))
                this.keepAliveThread.Abort();

            if (null != this.submitPriceStep2Thread && (this.submitPriceStep2Thread.ThreadState == System.Threading.ThreadState.Running || this.submitPriceStep2Thread.ThreadState == System.Threading.ThreadState.WaitSleepJoin))
                this.submitPriceStep2Thread.Abort();

            Hotkey.UnregisterHotKey(this.Handle, 103);
            Hotkey.UnregisterHotKey(this.Handle, 104);
            Hotkey.UnregisterHotKey(this.Handle, 105);
            Hotkey.UnregisterHotKey(this.Handle, 106);
            Hotkey.UnregisterHotKey(this.Handle, 107);
            Hotkey.UnregisterHotKey(this.Handle, 108);
            Hotkey.UnregisterHotKey(this.Handle, 109);

            Hotkey.UnregisterHotKey(this.Handle, 201);
            Hotkey.UnregisterHotKey(this.Handle, 202);
            Hotkey.UnregisterHotKey(this.Handle, 203);
            Hotkey.UnregisterHotKey(this.Handle, 204);

            Hotkey.UnregisterHotKey(this.Handle, 221);
            Hotkey.UnregisterHotKey(this.Handle, 222);

            logger.Info("Application Form Closed");
        }

        private void loadResource(String category)
        {

            //加载配置项1
            IGlobalConfig configResource = Resource.getInstance(this.EndPoint, category);//加载配置

            this.Text = configResource.tag;
            this.m_orcTitle = configResource.Title;
            this.m_orcCaptcha = configResource.Captcha;//
            this.m_orcPrice = configResource.Price;//价格识别
            this.m_orcCaptchaLoading = configResource.Loading;//LOADING识别
            this.m_orcCaptchaTip = configResource.Tips;//验证码提示（文字）
            this.m_orcCaptchaTipsUtil = new CaptchaUtil(m_orcCaptchaTip);
        }

        private void Form1_Load(object sender, EventArgs e){

            logger.Info("Application Form Load");

            Form.CheckForIllegalCrossThreadCalls = false;
            this.dateTimePicker1.Value = DateTime.Now;

            this.loadResource("real");

            //加载配置项2
            KeepAliveJob keepAliveJob = new KeepAliveJob(this.EndPoint,
                new ReceiveLogin(this.receiveLogin),
                new ReceiveOperation[]{
                    new ReceiveOperation(this.receiveOperation),
                    new ReceiveOperation(this.receiveOperation)});
            keepAliveJob.Execute();

            //keepAlive任务配置
            SchedulerConfiguration config1M = new SchedulerConfiguration(1000 * 60 * 1);
            config1M.Job = new KeepAliveJob(this.EndPoint,
                new ReceiveLogin(this.receiveLogin), 
                new ReceiveOperation[]{
                    new ReceiveOperation(this.receiveOperation),
                    new ReceiveOperation(this.receiveOperation)});
            this.m_schedulerKeepAlive = new Scheduler(config1M);

            //Action任务配置
            SchedulerConfiguration configStep2 = new SchedulerConfiguration(1000);
            //configStep2.Job = new SubmitPriceStep2Job(this.EndPoint, this.m_orcPrice, this.m_orcCaptchaLoading, this.m_orcCaptchaTipsUtil, this.m_orcCaptcha);
            configStep2.Job = new SubmitPriceStep2Job(this);
            m_schedulerSubmitStep2 = new Scheduler(configStep2);
            

            Hotkey.RegisterHotKey(this.Handle, 103, Hotkey.KeyModifiers.Ctrl, Keys.D3);
            Hotkey.RegisterHotKey(this.Handle, 104, Hotkey.KeyModifiers.Ctrl, Keys.D4);
            Hotkey.RegisterHotKey(this.Handle, 105, Hotkey.KeyModifiers.Ctrl, Keys.D5);
            Hotkey.RegisterHotKey(this.Handle, 106, Hotkey.KeyModifiers.Ctrl, Keys.D6);
            Hotkey.RegisterHotKey(this.Handle, 107, Hotkey.KeyModifiers.Ctrl, Keys.D7);
            Hotkey.RegisterHotKey(this.Handle, 108, Hotkey.KeyModifiers.Ctrl, Keys.D8);
            Hotkey.RegisterHotKey(this.Handle, 109, Hotkey.KeyModifiers.Ctrl, Keys.D9);

            Hotkey.RegisterHotKey(this.Handle, 202, Hotkey.KeyModifiers.Ctrl, Keys.Up);
            Hotkey.RegisterHotKey(this.Handle, 201, Hotkey.KeyModifiers.Ctrl, Keys.Left);
            Hotkey.RegisterHotKey(this.Handle, 203, Hotkey.KeyModifiers.Ctrl, Keys.Right);
            Hotkey.RegisterHotKey(this.Handle, 204, Hotkey.KeyModifiers.Ctrl, Keys.Enter);

            Hotkey.RegisterHotKey(this.Handle, 221, Hotkey.KeyModifiers.None, Keys.Escape);
            Hotkey.RegisterHotKey(this.Handle, 222, Hotkey.KeyModifiers.None, Keys.Enter);
        }

        /// <summary>
        /// 处理快捷键
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m) {
            const int WM_HOTKEY = 0x0312;
            switch (m.Msg) {
                case WM_HOTKEY:
                    switch (m.WParam.ToInt32()) {
                        case 103://CTRL+3
                            logger.Info("HOT KEY CTRL + 3 (103)");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 300);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 104://CTRL+4
                            logger.Info("HOT KEY CTRL + 4 (104)");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 400);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 105://CTRL+5
                            logger.Info("HOT KEY CTRL + 5 (105)");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 500);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 106://CTRL+6
                            logger.Info("HOT KEY CTRL + 6 (106)");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 600);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 107://CTRL+7
                            logger.Info("HOT KEY CTRL + 7 (107)");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 700);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 108://CTRL+8
                            logger.Info("HOT KEY CTRL + 8 (108)");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 800);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 109://CTRL+9
                            logger.Info("HOT KEY CTRL + 9 (109)");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 900);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 201://LEFT
                            logger.Info("HOT KEY CTRL + LEFT (201)");
                            this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.LEFT);
                            break;
                        case 202://UP
                            logger.Info("HOT KEY CTRL + UP (202)");
                            this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.MIDDLE);
                            break;
                        case 203://RIGHT
                            logger.Info("HOT KEY CTRL + RIGIHT (203)");
                            this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.RIGHT);
                            break;
                        case 204://CTRL + ENTER
                            logger.Info("HOT KEY CTRL + ENTER (204)");
                            this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.AUTO);
                            break;
                        case 221://ESC
                            logger.Info("HOT KEY ESCAPE");
                            this.closeDialog(SubmitPriceStep2Job.getPosition());
                            break;
                        case 222://ENTER
                            logger.Info("HOT KEY ENTER");
                            //this.submitCaptcha(SubmitPriceStep2Job.getPosition());
                            this.processEnter();
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        private void receiveLogin(Operation operation, Config config) {
        }

        private void receiveOperation(Operation operation) {
            Step2Operation bid = operation as Step2Operation;
            if (null != bid) {
                this.textBox1.Text = bid.startTime.ToString("MM/dd HH:mm:ss");
                this.textBox2.Text = String.Format("+{0}", bid.price);
            }
        }

        private void timer1_Tick(object sender, EventArgs e) {

            this.toolStripStatusLabel2.Text = String.Format("当前时间 {0}", DateTime.Now.ToString("HH:mm:ss"));
        }

        #region 拍牌ACTION
        /// <summary>
        /// 绝对价格，出价
        /// </summary>
        /// <param name="givePrice">坐标</param>
        /// <param name="price">绝对价</param>
        private void givePrice(tobid.rest.position.BidStep2 bid, String txtPrice) {

            logger.WarnFormat("BEGIN givePRICE({0})", txtPrice);

            //INPUT BOX
            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            ScreenUtil.SetCursorPos(bid.give.inputBox.x, bid.give.inputBox.y);
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

            for (int i = 0; i < txtPrice.Length; i++) {
                System.Threading.Thread.Sleep(25);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0x2, 0);
            } System.Threading.Thread.Sleep(50);
            logger.Info("\tEND   input PRICE");

            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            ScreenUtil.SetCursorPos(bid.give.button.x, bid.give.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");

            ScreenUtil.SetCursorPos(bid.submit.inputBox.x, bid.submit.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            //启动线程截校验码
            System.Threading.Thread loading = new System.Threading.Thread(delegate() {
                ScreenUtil screen = new ScreenUtil();
                for (int i = 0; i < 50; i++)//5秒钟
                {
                    logger.Debug("LOADING captcha...");
                    byte[] binaryCaptcha = new ScreenUtil().screenCaptureAsByte(bid.submit.captcha[0].x, bid.submit.captcha[0].y, 128, 28);
                    this.pictureCaptcha.Image = Bitmap.FromStream(new MemoryStream(binaryCaptcha));
                    System.Threading.Thread.Sleep(100);
                }
            });
            loading.Start();
            this.textInputCaptcha.Focus();
        }

        /// <summary>
        /// 获取当前价格，+delta，出价.【for CTRL+3、4、5、6、7、8、9】
        /// </summary>
        /// <param name="givePrice">坐标</param>
        /// <param name="delta">差价</param>
        private void giveDeltaPrice(tobid.rest.position.BidStep2 bid, int delta) {

            int x = bid.Origin.x;
            int y = bid.Origin.y;
            logger.WarnFormat("BEGIN givePRICE(delta : {0})", delta);
            logger.Info("\tBEGIN identify PRICE...");
            byte[] content = new ScreenUtil().screenCaptureAsByte(x+bid.give.price.x, y+bid.give.price.y, 52, 18);
            String txtPrice = this.m_orcPrice.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
            int price = Int32.Parse(txtPrice);
            price += delta;
            logger.InfoFormat("\tEND   identified PRICE = {0}", txtPrice);
            txtPrice = String.Format("{0:D}", price);

            //INPUT BOX
            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            ScreenUtil.SetCursorPos(x+bid.give.inputBox.x, y+bid.give.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);

            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);

            for (int i = 0; i < txtPrice.Length; i++) {
                System.Threading.Thread.Sleep(25);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0x2, 0);
            } System.Threading.Thread.Sleep(50);
            logger.Info("\tEND   input PRICE");

            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            //ScreenUtil.SetCursorPos(x+376, y+250);
            ScreenUtil.SetCursorPos(x + bid.give.button.x, y + bid.give.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");

            //ScreenUtil.SetCursorPos(bid.submit.inputBox.x, bid.submit.inputBox.y);
            //ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            //启动线程截校验码
            System.Threading.Thread loading = new System.Threading.Thread(delegate() {
                ScreenUtil screen = new ScreenUtil();
                for (int i = 0; i < 50; i++)//5秒钟
                {
                    logger.Debug("LOADING captcha...");
                    byte[] binaryCaptcha = new ScreenUtil().screenCaptureAsByte(x + bid.submit.captcha[0].x, y + bid.submit.captcha[0].y, 128, 28);
                    this.pictureCaptcha.Image = Bitmap.FromStream(new MemoryStream(binaryCaptcha));
                    System.Threading.Thread.Sleep(100);

                    //if (i == 15) {
                    //    ScreenUtil.SetCursorPos(bid.submit.inputBox.x, bid.submit.inputBox.y);
                    //    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                    //}
                }
            });
            loading.Start();
            this.textInputCaptcha.Focus();
        }

        private void closeDialog(tobid.rest.position.BidStep2 bid) {

            int x = bid.Origin.x;
            int y = bid.Origin.y;

            logger.Info("关闭校验码窗口");
            ScreenUtil.SetCursorPos(x + bid.submit.buttons[0].x + 188, y + bid.submit.buttons[0].y);//取消按钮
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
        }

        private void submitCaptcha(tobid.rest.position.BidStep2 bid) {

            int x = bid.Origin.x;
            int y = bid.Origin.y;
            logger.Info("提交验证码");
            ScreenUtil.SetCursorPos(x+bid.submit.buttons[0].x, y+bid.submit.buttons[0].y);//取消按钮
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
        }

        private void processEnter(){

            int x = SubmitPriceStep2Job.getPosition().Origin.x;
            int y = SubmitPriceStep2Job.getPosition().Origin.y;
            byte[] title = new ScreenUtil().screenCaptureAsByte(x + 30, y + 85, 170, 50);
            File.WriteAllBytes("TITLE.bmp", title);
            Bitmap bitTitle = new Bitmap(new MemoryStream(title));
            String strTitle = this.m_orcTitle.IdentifyStringFromPic(bitTitle);
            logger.Debug(strTitle);
            if ("系统提示".Equals(strTitle))
            {

                logger.Info("proces Enter in [系统提示]");
                ScreenUtil.SetCursorPos(x + 235, y + 312);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            }
            else if ("投标拍卖".Equals(strTitle))
            {

                logger.Info("proces Enter in [投标拍卖]");
                this.submitCaptcha(SubmitPriceStep2Job.getPosition());
            }
        }

        private void submit(tobid.rest.position.BidStep2 bid, String activeCaptcha) {
            
            int x = bid.Origin.x;
            int y = bid.Origin.y;

            logger.WarnFormat("BEGIN submitCAPTCHA({0})", activeCaptcha);

            logger.Info("\tBEGIN make INPUT blank");
            logger.DebugFormat("\tINPUT BOX({0}, {1})", x+bid.submit.inputBox.x, y+bid.submit.inputBox.y);
            ScreenUtil.SetCursorPos(x+bid.submit.inputBox.x, y+bid.submit.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);

            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            logger.Info("\tEND   make INPUT blank");

            logger.Info("\tBEGIN input CAPTCHA");
            {
                for (int i = 0; i < activeCaptcha.Length; i++) {

                    System.Threading.Thread.Sleep(25);
                    ScreenUtil.keybd_event(ScreenUtil.keycode[activeCaptcha[i].ToString()], 0, 0, 0);
                    ScreenUtil.keybd_event(ScreenUtil.keycode[activeCaptcha[i].ToString()], 0, 0x2, 0);
                }
            } System.Threading.Thread.Sleep(50);
            logger.Info("\tEND   input CAPTCHA");

            logger.Info("\tBEGIN click BUTTON[确定]");
            logger.DebugFormat("\tBUTTON[确定]({0}, {1})", x+bid.submit.buttons[0].x, y+bid.submit.buttons[0].y);
            ScreenUtil.SetCursorPos(x+bid.submit.buttons[0].x, y+bid.submit.buttons[0].y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[确定]");

            logger.InfoFormat("END submit({0})", activeCaptcha);
        }

        /// <summary>
        /// 出校验码，【for CTRL+左、上、右】
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="bid"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private Boolean submit(String URL, tobid.rest.position.BidStep2 bid, CaptchaInput input) {

            int x = bid.Origin.x;
            int y = bid.Origin.y;

            logger.WarnFormat("BEGIN submitCAPTCHA({0})", input);
            logger.Info("\tBEGIN make INPUT blank");
            logger.DebugFormat("\tINPUT BOX({0}, {1})", x + bid.submit.inputBox.x, y + bid.submit.inputBox.y);
            ScreenUtil.SetCursorPos(x+bid.submit.inputBox.x, y+bid.submit.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);

            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(15); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            //System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            logger.Info("\tEND   make INPUT blank");

            logger.Info("\tBEGIN identify CAPTCHA...");
            logger.DebugFormat("\tCAPTURE CAPTCHA({0}, {1})", x+bid.submit.captcha[0].x, y+bid.submit.captcha[0].y);
            byte[] binaryCaptcha = new ScreenUtil().screenCaptureAsByte(x+bid.submit.captcha[0].x, y+bid.submit.captcha[0].y, 128, 28);
            File.WriteAllBytes("CAPTCHA.BMP", binaryCaptcha);
            String txtCaptcha = this.m_orcCaptcha.IdentifyStringFromPic(new Bitmap(new MemoryStream(binaryCaptcha)));

            logger.DebugFormat("\tCAPTURE TIPS({0}, {1})", x+bid.submit.captcha[1].x, y+bid.submit.captcha[1].y);
            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(x+bid.submit.captcha[1].x, y+bid.submit.captcha[1].y, 112, 16);
            File.WriteAllBytes("TIPS.BMP", binaryTips);
            String txtActive = this.m_orcCaptchaTipsUtil.getActive(txtCaptcha, new Bitmap(new MemoryStream(binaryTips)));
            logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}", txtCaptcha, txtActive);

            logger.Info("\tBEGIN input CAPTCHA");
            {
                if (CaptchaInput.LEFT == input) {

                    for (int i = 0; i <= 3; i++) {
                        System.Threading.Thread.Sleep(25);
                        ScreenUtil.keybd_event(ScreenUtil.keycode[txtCaptcha[i].ToString()], 0, 0, 0);
                        ScreenUtil.keybd_event(ScreenUtil.keycode[txtCaptcha[i].ToString()], 0, 0x2, 0);
                    }
                }
                if (CaptchaInput.MIDDLE == input) {

                    for (int i = 1; i <= 4; i++) {

                        System.Threading.Thread.Sleep(25);
                        ScreenUtil.keybd_event(ScreenUtil.keycode[txtCaptcha[i].ToString()], 0, 0, 0);
                        ScreenUtil.keybd_event(ScreenUtil.keycode[txtCaptcha[i].ToString()], 0, 0x2, 0);
                    }
                }
                if (CaptchaInput.RIGHT == input) {

                    for (int i = 2; i <= 5; i++) {

                        System.Threading.Thread.Sleep(25);
                        ScreenUtil.keybd_event(ScreenUtil.keycode[txtCaptcha[i].ToString()], 0, 0, 0);
                        ScreenUtil.keybd_event(ScreenUtil.keycode[txtCaptcha[i].ToString()], 0, 0x2, 0);
                    }
                }
                if (CaptchaInput.AUTO == input) {

                    for (int i = 0; i < txtActive.Length; i++) {

                        System.Threading.Thread.Sleep(25);
                        ScreenUtil.keybd_event(ScreenUtil.keycode[txtActive[i].ToString()], 0, 0, 0);
                        ScreenUtil.keybd_event(ScreenUtil.keycode[txtActive[i].ToString()], 0, 0x2, 0);
                    }
                }
            }
            System.Threading.Thread.Sleep(50);
            logger.Info("\tEND   input CAPTCHA");

            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("确定要提交出价吗?", "提交出价", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            if (dr == DialogResult.OK) {
                logger.InfoFormat("用户选择确定出价");
                logger.DebugFormat("\tBUTTON[确定]({0}, {1})", x+bid.submit.buttons[0].x, y+bid.submit.buttons[0].y);
                ScreenUtil.SetCursorPos(x+bid.submit.buttons[0].x, y+bid.submit.buttons[0].y);//确定按钮
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

                //System.Threading.Thread.Sleep(1000);
                //ScreenUtil.SetCursorPos(bid.submit.buttons[0].x + 188 / 2, bid.submit.buttons[0].y - 10);//确定按钮
                //ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                //ScreenUtil.SetCursorPos(bid.submit.buttons[0].x + 188 / 2, bid.submit.buttons[0].y - 10);//确定按钮
                logger.Info("END   submitCAPTCHA");
                return true;
            } else {
                logger.InfoFormat("用户选择取消出价");
                logger.DebugFormat("\tBUTTON[取消]({0}, {1})", x+bid.submit.buttons[0].x+188, y+bid.submit.buttons[0].y);
                ScreenUtil.SetCursorPos(x+bid.submit.buttons[0].x + 188, y+bid.submit.buttons[0].y);//取消按钮
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                logger.Info("END   submitCAPTCHA");
                return false;
            }

            //logger.Info("\tBEGIN click BUTTON[确定]");
            //ScreenUtil.SetCursorPos(bid.submit.buttons[0].x, bid.submit.buttons[0].y);
            //ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            //logger.Info("\tEND   click BUTTON[确定]");
        }
        #endregion

        #region 控件事件处理
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            this.radioDeltaPrice.Checked = true;
        }
        
        private void textPrice2_TextChanged(object sender, EventArgs e) {
            this.radioPrice.Checked = true;
        }

        private void textPrice2_KeyPress(object sender, KeyPressEventArgs e) {

            int keyCode = (int)e.KeyChar;
            if ((keyCode < 48 || keyCode > 57) && keyCode != 8)
                e.Handled = true;
        }

        private void button_updatePolicy_Click(object sender, EventArgs e) {

            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            //DialogResult dr = MessageBox.Show("确定要更新出价策略吗?", "更新策略", 
            //    messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            DialogResult dr = MessageBox.Show("确定要更新出价策略吗?", "更新策略", 
                messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            if (dr == DialogResult.OK) {
                Step2Operation bidOps = SubmitPriceStep2Job.getConfig();
                bidOps.updateTime = DateTime.Now;
                bidOps.startTime = this.dateTimePicker1.Value;
                bidOps.expireTime = bidOps.startTime.AddHours(1);

                this.textBox1.Text = this.dateTimePicker1.Value.ToString("MM/dd HH:mm:ss");

                if (radioDeltaPrice.Checked) {
                    object obj = this.comboBox1.SelectedItem;
                    if (obj == null) {
                        MessageBox.Show("请选择价格");
                        this.comboBox1.Focus();
                        return;
                    }
                    this.textBox2.Text = this.comboBox1.Text;
                    bidOps.price = Int32.Parse(this.comboBox1.Text);
                    SubmitPriceStep2Job.setConfig(bidOps, this.checkPriceOnly.Checked);
                }

                if (radioPrice.Checked) {
                    if ("".Equals(this.textPrice2.Text)) {
                        MessageBox.Show("请正确的价格");
                        this.textPrice2.Focus();
                        return;
                    }
                    this.textBox2.Text = this.textPrice2.Text;
                    SubmitPriceStep2Job.setConfig(Int32.Parse(this.textPrice2.Text), bidOps, this.checkPriceOnly.Checked);
                }

                if (null != this.submitPriceStep2Thread)
                    this.submitPriceStep2Thread.Abort();
                System.Threading.ThreadStart submitPriceThreadStart = new System.Threading.ThreadStart(this.m_schedulerSubmitStep2.Start);
                this.submitPriceStep2Thread = new System.Threading.Thread(submitPriceThreadStart);
                this.submitPriceStep2Thread.Name = "submitPriceThread";
                this.submitPriceStep2Thread.Start();
            }
        }

        private void textInputPrice_KeyPress(object sender, KeyPressEventArgs e) {

            int keyCode = (int)e.KeyChar;
            if ((keyCode < 48 || keyCode > 57) && keyCode != 8)
                e.Handled = true;
        }

        /// <summary>
        /// WINFORM的按键处理
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {

            Point p1 = MousePosition;
            if (keyData == Keys.Escape) {
                this.closeDialog(SubmitPriceStep2Job.getPosition());
                this.pictureCaptcha.Image = null;
                ScreenUtil.SetForegroundWindow(this.Handle);
                this.textInputPrice.Focus();
                ScreenUtil.SetCursorPos(p1.X, p1.Y);
            }
            if (keyData == Keys.Enter) {
                if (this.textInputPrice.Focused) {
                    this.givePrice(SubmitPriceStep2Job.getPosition(), this.textInputPrice.Text);
                    this.textInputCaptcha.Focus();
                } else if (this.textInputCaptcha.Focused) {
                    this.submit(SubmitPriceStep2Job.getPosition(), this.textInputCaptcha.Text);
                    this.textInputCaptcha.Text = "";
                    this.textInputPrice.Focus();
                }
                ScreenUtil.SetForegroundWindow(this.Handle);
                ScreenUtil.SetCursorPos(p1.X, p1.Y);
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion

        #region 服务器策略|自助策略|手动
        private void radioServPolicy_Click(object sender, EventArgs e) {

            if (!this.radioServPolicy.Checked) {
                MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                DialogResult dr = MessageBox.Show(String.Format("确定使用{0}吗?", this.radioServPolicy.Text), "策略选择",
                    messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                if (dr == DialogResult.OK) {
                    this.radioServPolicy.Checked = true;
                    this.radioLocalPolicy.Checked = false;
                    this.groupBoxLocal.Enabled = false;
                    this.radioManualPolicy.Checked = false;
                    this.groupBoxManual.Enabled = false;

                    this.groupBoxPolicy.Text = this.radioServPolicy.Text;

                    if (null != this.submitPriceStep2Thread)
                        this.submitPriceStep2Thread.Abort();

                    //加载配置项2
                    KeepAliveJob keepAliveJob = new KeepAliveJob(this.EndPoint, 
                        new ReceiveLogin(this.receiveLogin),
                        new ReceiveOperation[]{
                            new ReceiveOperation(this.receiveOperation),
                            new ReceiveOperation(this.receiveOperation)});
                    keepAliveJob.Execute();

                    System.Threading.ThreadStart keepAliveThread = new System.Threading.ThreadStart(this.m_schedulerKeepAlive.Start);
                    this.keepAliveThread = new System.Threading.Thread(keepAliveThread);
                    this.keepAliveThread.Name = "keepAliveThread";
                    this.keepAliveThread.Start();

                    System.Threading.ThreadStart submitPriceThreadStart = new System.Threading.ThreadStart(this.m_schedulerSubmitStep2.Start);
                    this.submitPriceStep2Thread = new System.Threading.Thread(submitPriceThreadStart);
                    this.submitPriceStep2Thread.Name = "submitPriceThread";
                    this.submitPriceStep2Thread.Start();
                }
            }
        }

        private void radioLocalPolicy_Click(object sender, EventArgs e) {

            if (!this.radioLocalPolicy.Checked) {
                MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                DialogResult dr = MessageBox.Show(String.Format("确定使用{0}吗?", this.radioLocalPolicy.Text), "策略选择",
                    messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                if (dr == DialogResult.OK) {
                    this.radioServPolicy.Checked = false;
                    this.radioLocalPolicy.Checked = true;
                    this.radioManualPolicy.Checked = false;
                    this.groupBoxManual.Enabled = false;

                    this.groupBoxLocal.Enabled = true;
                    this.groupBoxPolicy.Text = this.radioLocalPolicy.Text;
                    if (null != this.keepAliveThread)
                        this.keepAliveThread.Abort();

                    if (null != this.submitPriceStep2Thread)
                        this.submitPriceStep2Thread.Abort();
                }
            }
        }

        private void radioManualPolicy_Click(object sender, EventArgs e) {

            if (!this.radioManualPolicy.Checked) {
                MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                DialogResult dr = MessageBox.Show(String.Format("确定使用{0}吗?", this.radioManualPolicy.Text), "策略选择",
                    messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                if (dr == DialogResult.OK) {
                    this.radioServPolicy.Checked = false;
                    this.radioLocalPolicy.Checked = false;
                    this.groupBoxLocal.Enabled = false;
                    this.radioManualPolicy.Checked = true;

                    this.groupBoxManual.Enabled = true;
                    this.textInputPrice.Focus();
                    this.groupBoxPolicy.Text = this.radioLocalPolicy.Text;
                    if (null != this.keepAliveThread)
                        this.keepAliveThread.Abort();

                    if (null != this.submitPriceStep2Thread)
                        this.submitPriceStep2Thread.Abort();
                }
            }
        }
        #endregion        

        private void buttonLogin_Click(object sender, EventArgs e) {

            LoginJob loginJob = new LoginJob(this.m_orcCaptchaLoading);
            loginJob.Execute();

            //SubmitPriceStep1Job submitPriceJob = new SubmitPriceStep1Job(
            //    this.m_orcCaptchaLoading,
            //    this.m_orcCaptchaTipsUtil,
            //    this.m_orcCaptcha);
            //submitPriceJob.Execute();
        }

        private void 国拍ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.国拍ToolStripMenuItem.Checked = true;
            this.模拟ToolStripMenuItem.Checked = false;
            this.loadResource("real");
        }

        private void 模拟ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.国拍ToolStripMenuItem.Checked = false;
            this.模拟ToolStripMenuItem.Checked = true;
            this.loadResource("simulate");
        }

        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.step2Dialog.ShowDialog(this);
            this.step2Dialog.BringToFront();
        }
    }
}
