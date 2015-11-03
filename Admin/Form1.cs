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
using tobid.rest.position;
using tobid.util;
using tobid.util.orc;
using tobid.scheduler;
using tobid.scheduler.jobs;

namespace Admin {

    enum CaptchaInput {
        LEFT, MIDDLE, RIGHT,
        AUTO
    }

    public partial class Form1 : Form, IRepository, INotify {

        public Form1(String endPoint) {
            InitializeComponent();
            this.pictureSubs = new PictureBox[]{
                this.pictureSub1,
                this.pictureSub2,
                this.pictureSub3,
                this.pictureSub4,
                this.pictureSub5,
                this.pictureSub6
            };

            this.m_endPoint = endPoint;
        }

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Form1));
        private PictureBox[] pictureSubs;

        private String m_endPoint;

        private IOrc m_orcTitle;
        private IOrc m_orcLogin;
        private IOrc m_orcCaptcha;
        private IOrc m_orcCaptchaLoading;
        private IOrc[] m_orcCaptchaTip;
        private IOrc m_orcPrice;
        private IOrc m_orcPriceSM;
        private CaptchaUtil m_orcCaptchaTipsUtil;
        private Step2Form m_step2Form;
        private Step1Form m_step1Form;

        #region IRepository
        public String endPoint { get { return this.m_endPoint; } }
        public IOrc orcTitle { get { return this.m_orcTitle; } }
        public IOrc orcCaptcha { get { return this.m_orcCaptcha; } }
        public IOrc orcPrice { get { return this.m_orcPrice; } }
        public IOrc orcPriceSM { get { return this.m_orcPriceSM; } }
        public IOrc orcCaptchaLoading { get { return this.m_orcCaptchaLoading; } }
        public IOrc[] orcCaptchaTip { get { return this.m_orcCaptchaTip; } }
        public CaptchaUtil orcCaptchaTipsUtil { get { return this.m_orcCaptchaTipsUtil; } }
        public int interval { get { return 25; } }
        public Entry[] entries { get { return null; } }
        public String category
        {
            get
            {
                if (this.RealToolStripMenuItem.Checked)
                    return "real";
                else
                    return "simulate";
            }
        }
        public GivePriceStep2 givePriceStep2 {
            get {
                return SubmitPriceStep2Job.getPosition().give;
            }
        }
        public SubmitPrice submitPrice {
            get {
                return SubmitPriceStep2Job.getPosition().submit;
            }
        }
        #endregion

        private System.Threading.Thread keepAliveThread;
        private System.Threading.Thread submitPriceThread;
        private System.Timers.Timer timer = new System.Timers.Timer();

        private Scheduler m_schedulerKeepAlive;
        private Scheduler m_schedulerSubmit;

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {

            Hotkey.UnregisterHotKey(this.Handle, 103);
            Hotkey.UnregisterHotKey(this.Handle, 104);
            Hotkey.UnregisterHotKey(this.Handle, 105);
            Hotkey.UnregisterHotKey(this.Handle, 106);
            Hotkey.UnregisterHotKey(this.Handle, 107);
            Hotkey.UnregisterHotKey(this.Handle, 108);
            Hotkey.UnregisterHotKey(this.Handle, 109);

            Hotkey.UnregisterHotKey(this.Handle, 121);
            Hotkey.UnregisterHotKey(this.Handle, 120);
            Hotkey.UnregisterHotKey(this.Handle, 122);
            Hotkey.UnregisterHotKey(this.Handle, 123);
            Hotkey.UnregisterHotKey(this.Handle, 124);
            Hotkey.UnregisterHotKey(this.Handle, 125);

            Hotkey.UnregisterHotKey(this.Handle, 155);

            if (null != this.keepAliveThread)
                this.keepAliveThread.Abort();
            if (null != this.submitPriceThread)
                this.submitPriceThread.Abort();
        }

        private void loadResource(String category) {

            IGlobalConfig configResource = Resource.getInstance(this.m_endPoint, category);//加载配置

            this.Text = configResource.tag;
            this.m_orcTitle = configResource.Title;
            this.m_orcLogin = configResource.Login;
            this.m_orcCaptcha = configResource.Captcha;//验证码
            this.m_orcPrice = configResource.Price;//价格识别
            this.m_orcCaptchaLoading = configResource.Loading;//LOADING识别
            this.m_orcCaptchaTip = configResource.Tips;//验证码提示（文字）
            this.m_orcCaptchaTipsUtil = new CaptchaUtil(m_orcCaptchaTip);
        }

        private void Form1_Load(object sender, EventArgs e) {

            Form.CheckForIllegalCrossThreadCalls = false;
            this.textURL.Text = this.m_endPoint;
            this.m_step2Form = new Step2Form();
            this.m_step1Form = new Step1Form();

            this.loadResource("real");

            //加载配置项2
            KeepAliveJob keepAliveJob = new KeepAliveJob(this.m_endPoint, 
                new ReceiveLogin(this.receiveLogin),
                new ReceiveOperation[]{
                    new ReceiveOperation(this.receiveStep1),
                    new ReceiveOperation(this.receiveStep2)},
                this);
            keepAliveJob.Execute();

            //keepAlive任务配置
            SchedulerConfiguration config5M = new SchedulerConfiguration(1000 * 60 * 1);
            config5M.Job = new KeepAliveJob(this.m_endPoint,
                new ReceiveLogin(this.receiveLogin), 
                new ReceiveOperation[]{
                    new ReceiveOperation(this.receiveStep1),
                    new ReceiveOperation(this.receiveStep2)},
                this);
            m_schedulerKeepAlive = new Scheduler(config5M);

            //Action任务配置
            SchedulerConfiguration config1S = new SchedulerConfiguration(1000);
            config1S.Job = new SubmitPriceStep2Job(repository: this, notify: this);
            m_schedulerSubmit = new Scheduler(config1S);

            config1S = new SchedulerConfiguration(1000);
            config1S.Job = new CustomJob(null);

            Hotkey.RegisterHotKey(this.Handle, 103, Hotkey.KeyModifiers.Ctrl, Keys.D3);
            Hotkey.RegisterHotKey(this.Handle, 104, Hotkey.KeyModifiers.Ctrl, Keys.D4);
            Hotkey.RegisterHotKey(this.Handle, 105, Hotkey.KeyModifiers.Ctrl, Keys.D5);
            Hotkey.RegisterHotKey(this.Handle, 106, Hotkey.KeyModifiers.Ctrl, Keys.D6);
            Hotkey.RegisterHotKey(this.Handle, 107, Hotkey.KeyModifiers.Ctrl, Keys.D7);
            Hotkey.RegisterHotKey(this.Handle, 108, Hotkey.KeyModifiers.Ctrl, Keys.D8);
            Hotkey.RegisterHotKey(this.Handle, 109, Hotkey.KeyModifiers.Ctrl, Keys.D9);

            Hotkey.RegisterHotKey(this.Handle, 121, Hotkey.KeyModifiers.Ctrl, Keys.Left);
            Hotkey.RegisterHotKey(this.Handle, 120, Hotkey.KeyModifiers.Ctrl, Keys.Up);
            Hotkey.RegisterHotKey(this.Handle, 122, Hotkey.KeyModifiers.Ctrl, Keys.Right);
            Hotkey.RegisterHotKey(this.Handle, 123, Hotkey.KeyModifiers.Ctrl, Keys.Enter);
            Hotkey.RegisterHotKey(this.Handle, 124, Hotkey.KeyModifiers.None, Keys.Escape);
            Hotkey.RegisterHotKey(this.Handle, 125, Hotkey.KeyModifiers.None, Keys.Enter);

            Hotkey.RegisterHotKey(this.Handle, 155, Hotkey.KeyModifiers.CtrlShift, Keys.Enter);
        }

        protected override void WndProc(ref Message m) {
            const int WM_HOTKEY = 0x0312;
            switch (m.Msg) {
                case WM_HOTKEY:
                    switch (m.WParam.ToInt32()) {
                        case 103://CTRL+3
                            logger.Info("HOT KEY [CTRL+3]");
                            this.givePrice(this.m_endPoint, this.m_step2Form.bid.give, 300);
                            break;
                        case 104://CTRL+4
                            logger.Info("HOT KEY [CTRL+4]");
                            this.givePrice(this.m_endPoint, this.m_step2Form.bid.give, 400);
                            break;
                        case 105://CTRL+5
                            logger.Info("HOT KEY [CTRL+5]");
                            this.givePrice(this.m_endPoint, this.m_step2Form.bid.give, 500);
                            break;
                        case 106://CTRL+6
                            logger.Info("HOT KEY [CTRL+6]");
                            this.givePrice(this.m_endPoint, this.m_step2Form.bid.give, 600);
                            break;
                        case 107://CTRL+7
                            logger.Info("HOT KEY [CTRL+7]");
                            this.givePrice(this.m_endPoint, this.m_step2Form.bid.give, 700);
                            break;
                        case 108://CTRL+8
                            logger.Info("HOT KEY [CTRL+8]");
                            this.givePrice(this.m_endPoint, this.m_step2Form.bid.give, 800);
                            break;
                        case 109://CTRL+9
                            logger.Info("HOT KEY [CTRL+9]");
                            this.givePrice(this.m_endPoint, this.m_step2Form.bid.give, 900);
                            break;
                        case 120://CTRL+UP
                            logger.Info("HOT KEY [CTRL+UP]");
                            this.subimt(this.m_endPoint, this.m_step2Form.bid.submit, CaptchaInput.MIDDLE);
                            break;
                        case 121://CTRL+LEFT
                            logger.Info("HOT KEY [CTRL+LEFT]");
                            this.subimt(this.m_endPoint, this.m_step2Form.bid.submit, CaptchaInput.LEFT);
                            break;
                        case 122://CTRL+RIGHT
                            logger.Info("HOT KEY [CTRL+RIGHT]");
                            this.subimt(this.m_endPoint, this.m_step2Form.bid.submit, CaptchaInput.RIGHT);
                            break;
                        case 123://CTRL+ENTER
                            logger.Info("HOT KEY [CTRL+ENTER]");
                            this.subimt(this.m_endPoint, this.m_step2Form.bid.submit, CaptchaInput.AUTO);
                            break;
                        case 124://ESC
                            logger.Info("HOT KEY [ESC]");
                            this.closeDialog(this.m_step2Form.bid);
                            break;
                        case 125://ENTER
                            logger.Info("HOT KEY [ENTER]");
                            this.processEnter();
                            break;
                        case 155://CTRL+SHIFT+ENTER
                            logger.Info("HOT KEY [CTRL+SHIFT+ENTER]");
                            this.textPosition.Text = this.textMousePos.Text;
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        private void timer1_Tick(object sender, EventArgs e) {

            Point screenPoint = Control.MousePosition;
            this.textMousePos.Text = screenPoint.X + "," + screenPoint.Y;
        }

        static private Point findOrigin() {

            Rectangle rectX = new Rectangle();
            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindowsClass();
            foreach (SHDocVw.InternetExplorer Browser in shellWindows) {
                if (Browser.LocationURL.StartsWith("http://") || Browser.LocationURL.StartsWith("https://")) {

                    IntPtr frameTab = FindWindowEx((IntPtr)Browser.HWND, IntPtr.Zero, "Frame Tab", String.Empty);
                    IntPtr tabWindow = FindWindowEx(frameTab, IntPtr.Zero, "TabWindowClass", null);
                    int rtnX = GetWindowRect(tabWindow, out rectX);
                }
            }
            return new Point(rectX.X, rectX.Y);
        }
        /// <summary>
        /// 检查价格
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_checkPrice_Click(object sender, EventArgs e) {

            Point origin = findOrigin();
            foreach (PictureBox picBox in this.pictureSubs)
                picBox.Image = null;

            //String[] pos = this.textPosition.Text.Split(new char[] { ',' });
            //byte[] content = new ScreenUtil().screenCaptureAsByte(Int32.Parse(pos[0]) + this.m_step2Form.bid.give.price.x, Int32.Parse(pos[1]) + this.m_step2Form.bid.give.price.y, 100, 24);
            byte[] content = new ScreenUtil().screenCaptureAsByte(origin.X + this.m_step2Form.bid.give.price.x, origin.Y + this.m_step2Form.bid.give.price.y, 100, 24);
            this.pictureBox3.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));
            String txtPrice = this.m_orcPrice.IdentifyStringFromPic(new Bitmap(this.pictureBox3.Image));
            for (int i = 0; i < this.m_orcPrice.SubImgs.Count; i++)
                this.pictureSubs[i].Image = this.m_orcPrice.SubImgs[i];
            this.label1.Text = txtPrice;
        }

        /// <summary>
        /// 校验码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_checkCaptcha_Click(object sender, EventArgs e) {

            Point origin = findOrigin();
            foreach (PictureBox picBox in this.pictureSubs)
                picBox.Image = null;

            String[] pos = this.textPosition.Text.Split(new char[] { ',' });
            //byte[] content = new ScreenUtil().screenCaptureAsByte(Int32.Parse(pos[0]) + this.m_step2Form.bid.submit.captcha[0].x, Int32.Parse(pos[1]) + this.m_step2Form.bid.submit.captcha[0].y, 120, 38);
            byte[] content = new ScreenUtil().screenCaptureAsByte(origin.X + this.m_step2Form.bid.submit.captcha[0].x, origin.Y + this.m_step2Form.bid.submit.captcha[0].y, 120, 38);
            this.pictureBox3.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));

            if (this.checkBoxCaptcha.Checked) {//如果选中“校验码”
            
                //String strCaptcha = new HttpUtil().postByteAsFile(this.textURL.Text + "/receive/captcha/detail.do", content);
                String strCaptcha = this.m_orcCaptcha.IdentifyStringFromPic(new Bitmap(new MemoryStream(content)));
                //String[] array = Newtonsoft.Json.JsonConvert.DeserializeObject<String[]>(strCaptcha);

                for (int i = 0; i < 6; i++)
                    this.pictureSubs[i].Image = this.m_orcCaptcha.SubImgs[i];
                this.label1.Text = strCaptcha;

            } else {//测试“正在加载校验码”

                String strLoading = this.m_orcCaptchaLoading.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));

                for (int i = 0; i < 6; i++)
                    this.pictureSubs[i].Image = this.m_orcCaptchaLoading.SubImgs[i];
                this.label2.Text = strLoading;
            }
        }

        /// <summary>
        /// 校验码提示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_checkTips_Click(object sender, EventArgs e) {

            Point origin = findOrigin();
            foreach (PictureBox picBox in this.pictureSubs)
                picBox.Image = null;

            String[] pos = this.textPosition.Text.Split(new char[] { ',' });
            byte[] content = new ScreenUtil().screenCaptureAsByte(origin.X + this.m_step2Form.bid.submit.captcha[1].x, origin.Y + this.m_step2Form.bid.submit.captcha[1].y, 140, 24);
            //byte[] content = new ScreenUtil().screenCaptureAsByte(Int32.Parse(pos[0]) + this.m_step2Form.bid.submit.captcha[1].x, Int32.Parse(pos[1]) + this.m_step2Form.bid.submit.captcha[1].y, 140, 24);
            //byte[] content = new ScreenUtil().screenCaptureAsByte(Int32.Parse(pos[0]), Int32.Parse(pos[1]), 140, 24);
            this.pictureBox3.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));

            this.label2.Text = this.m_orcCaptchaTipsUtil.getActive("一二三四五六", new Bitmap(new MemoryStream(content)));
            for (int i = 0; i < this.m_orcCaptchaTipsUtil.SubImgs.Count; i++)
                this.pictureSubs[i].Image = this.m_orcCaptchaTipsUtil.SubImgs[i];
            
        }

        private void receiveLogin(Client client) {

            Config config = client.config;
            if (null != config) {
                this.labelName.Text = config.pname;
                this.labelNo.Text = config.no;

                this.buttonLogin.Enabled = true;
            }
        }

        private void receiveStep1(Operation operation) {

            if (null != operation) {
                Step1Operation bidOps = (Step1Operation)operation;
                BidStep1 bid = Newtonsoft.Json.JsonConvert.DeserializeObject<BidStep1>(operation.content);
                this.m_step1Form.bid = bid;
                this.toolStripStatusLabel2.Text = String.Format("步骤1：{1} @{0}", operation.startTime, bidOps.price);
                this.buttonStep1.Enabled = true;
            }
        }

        private void receiveStep2(Operation operation) {

            if (null != operation) {
                Step2Operation bidOps = (Step2Operation)operation;
                BidStep2 bid = Newtonsoft.Json.JsonConvert.DeserializeObject<BidStep2>(operation.content);
                this.m_step2Form.bid = bid;
                this.toolStripStatusLabel1.Text = String.Format("步骤2：+{1} @{0}", operation.startTime, bidOps.price);
            }
        }

        private void radioButton_Manual_CheckedChanged(object sender, EventArgs e) {
            
            if (this.radioButton1.Checked) {
                if (null != this.keepAliveThread)
                    this.keepAliveThread.Abort();
                if (null != this.submitPriceThread)
                    this.submitPriceThread.Abort();
            }
        }

        private void radioButton_Auto_CheckedChanged(object sender, EventArgs e) {

            if (this.radioButton2.Checked) {
                System.Threading.ThreadStart keepAliveThread = new System.Threading.ThreadStart(this.m_schedulerKeepAlive.Start);
                this.keepAliveThread = new System.Threading.Thread(keepAliveThread);
                this.keepAliveThread.Name = "keepAliveThread";
                this.keepAliveThread.Start();

                System.Threading.ThreadStart submitPriceThreadStart = new System.Threading.ThreadStart(this.m_schedulerSubmit.Start);
                this.submitPriceThread = new System.Threading.Thread(submitPriceThreadStart);
                this.submitPriceThread.Name = "submitPriceThread";
                this.submitPriceThread.Start();
            }
        }


        #region 拍ACTION
        private void closeDialog(tobid.rest.position.BidStep2 bid) {

            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;
            //int x = bid.Origin.x;
            //int y = bid.Origin.y;

            logger.Info("关闭校验码窗口");
            ScreenUtil.SetCursorPos(bid.submit.buttons[1].x + x, bid.submit.buttons[1].y + y);//取消按钮
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
        }

        private void submitCaptcha(tobid.rest.position.BidStep2 bid)
        {
            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;
            //int x = this.m_step2Form.bid.Origin.x;
            //int y = this.m_step2Form.bid.Origin.y;
            logger.Info("提交验证码");
            ScreenUtil.SetCursorPos(x+bid.submit.buttons[0].x, y+bid.submit.buttons[0].y);//取消按钮
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
        }

        private void processEnter()
        {
            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;
            //int x = this.m_step2Form.bid.Origin.x;
            //int y = this.m_step2Form.bid.Origin.y;
            //byte[] title = new ScreenUtil().screenCaptureAsByte(x + 30, y + 85, 170, 50);
            byte[] title = new ScreenUtil().screenCaptureAsByte(x + 555, y + 241, 170, 50);
            File.WriteAllBytes("TITLE.bmp", title);
            Bitmap bitTitle = new Bitmap(new MemoryStream(title));
            String strTitle = this.m_orcTitle.IdentifyStringFromPic(bitTitle);
            logger.Debug(strTitle);
            if ("系统提示".Equals(strTitle))
            {

                logger.Info("proces Enter in [系统提示]");
                //ScreenUtil.SetCursorPos(x + 235, y + 312);
                ScreenUtil.SetCursorPos(x + 733, y + 465);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            }
            else if ("投标拍卖".Equals(strTitle))
            {

                logger.Info("proces Enter in [投标拍卖]");
                this.submitCaptcha(this.m_step2Form.bid);
            }
        }

        private void givePrice(String URL, GivePriceStep2 points, int deltaPrice) {

            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;
            //int x = this.m_step2Form.bid.Origin.x;
            //int y = this.m_step2Form.bid.Origin.y;

            logger.WarnFormat("BEGIN 出价格(delta : {0})", deltaPrice);
            byte[] content = new ScreenUtil().screenCaptureAsByte(x+points.price.x, y+points.price.y, 52, 18);
            this.pictureBox2.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));
            logger.Info("\tBEGIN identify Price");
            String txtPrice = this.m_orcPrice.IdentifyStringFromPic(new Bitmap(this.pictureBox2.Image));
            logger.InfoFormat("\tEND  identify Price : {0}", txtPrice);
            int price = Int32.Parse(txtPrice);
            price += deltaPrice;
            txtPrice = String.Format("{0:D5}", price);

            logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
            ScreenUtil.SetCursorPos(x + points.inputBox.x, y + points.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            System.Threading.Thread.Sleep(50); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["BACKSPACE"], 0, 0x2, 0);

            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);
            System.Threading.Thread.Sleep(25); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0, 0); ScreenUtil.keybd_event(ScreenUtil.keycode["DELETE"], 0, 0x2, 0);

            for (int i = 0; i < txtPrice.Length; i++) {
                System.Threading.Thread.Sleep(25);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], ScreenUtil.keycode[txtPrice[i].ToString()], 0x2, 0);
            }
            logger.Info("\tEND   input PRICE");

            //点击出价
            System.Threading.Thread.Sleep(100);
            logger.Info("\tBEGIN click button[出价]");
            ScreenUtil.SetCursorPos(x + points.button.x, y + points.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click button[出价]");
            logger.Info("END   出价格");
        }

        private void subimt(String URL, SubmitPrice points, CaptchaInput inputType) {

            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;
            //int x = this.m_step2Form.bid.Origin.x;
            //int y = this.m_step2Form.bid.Origin.y;
            logger.WarnFormat("BEGIN 验证码({0})", inputType);

            ScreenUtil.SetCursorPos(x + points.inputBox.x, y + points.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

            logger.Info("\tBEGIN make INPUTBOX blank");
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

            logger.Info("\tEND   make INPUTBOX blank");

            byte[] content = new ScreenUtil().screenCaptureAsByte(x + points.captcha[0].x, y + points.captcha[0].y, 128, 28);
            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(x + points.captcha[1].x, y + points.captcha[1].y, 112, 16);
            Bitmap bitmap = new Bitmap(new MemoryStream(content));
            this.pictureBox1.Image = bitmap;
            String strLoading = this.m_orcCaptchaLoading.IdentifyStringFromPic(bitmap);
            logger.InfoFormat("LOADING : {0}", strLoading);

            
            //if ("正在获取校验码".Equals(strLoading)) {
            //    logger.InfoFormat("正在获取校验码，关闭&打开窗口重新获取");
            //    ScreenUtil.SetCursorPos(points.buttons[0].x + 188, points.buttons[0].y);//取消按钮
            //    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            //    return;
            //}
            
            logger.Info("\tBEGIN identify Captcha");
            String txtCaptcha = this.m_orcCaptcha.IdentifyStringFromPic(bitmap);
            logger.InfoFormat("\tEND   identify Captcha : [{0}]", txtCaptcha);

            logger.Info("\tBEGIN input ACTIVE CAPTCHA");
            String strActive = "";
            if (CaptchaInput.LEFT == inputType)
                strActive = txtCaptcha.Substring(0, 4);
            else if (CaptchaInput.MIDDLE == inputType)
                strActive = txtCaptcha.Substring(1, 4);
            else if (CaptchaInput.RIGHT == inputType)
                strActive = txtCaptcha.Substring(2, 4);
            else if (CaptchaInput.AUTO == inputType)
                strActive = this.m_orcCaptchaTipsUtil.getActive(txtCaptcha, new Bitmap(new MemoryStream(binaryTips)));

            for (int i = 0; i < strActive.Length; i++) {
                System.Threading.Thread.Sleep(25);
                ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0, 0);
                ScreenUtil.keybd_event(ScreenUtil.keycode[strActive[i].ToString()], 0, 0x2, 0);
            } System.Threading.Thread.Sleep(25);
            logger.InfoFormat("\tEND   input ACTIVE CAPTCHA [{0}]", strActive);

            {
                MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                DialogResult dr = MessageBox.Show("确定要提交出价吗?", "提交出价", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                if (dr == DialogResult.OK) {
                    logger.Info("用户选择确定出价");
                    ScreenUtil.SetCursorPos(x + points.buttons[0].x, y + points.buttons[0].y);//确定按钮
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

                    //System.Threading.Thread.Sleep(1000);
                    //ScreenUtil.SetCursorPos(points.buttons[0].x + 188 / 2, points.buttons[0].y - 10);//确定按钮
                    //ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                } else {
                    logger.Info("用户选择取消出价");
                    ScreenUtil.SetCursorPos(x + points.buttons[1].x, y + points.buttons[1].y);//取消按钮
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                }
            }
            logger.Info("END   验证码");
        }
        #endregion

        static void ie_DocumentComplete(object pDisp, ref object URL) {

            DocComplete.Set();
        }

        private void button_ConfigBid_Click(object sender, EventArgs e) {

            this.m_step2Form.endPoint = this.textURL.Text;
            this.m_step2Form.ShowDialog(this);
            this.m_step2Form.BringToFront();
        }

        private static System.Threading.AutoResetEvent DocComplete = new System.Threading.AutoResetEvent(false);
        private void button7_Click(object sender, EventArgs e) {

            this.m_step1Form.endPoint = this.textURL.Text;
            this.m_step1Form.ShowDialog(this);
            this.m_step1Form.BringToFront();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int Width, int Height, int flags);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpszClass, string lpszWindow);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int GetWindowRect(IntPtr hwnd, out Rectangle lpRect);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int ClientToScreen(IntPtr hwnd, ref Point lpPoint);

        private void buttonLogin_Click(object sender, EventArgs e) {
            
            const int GWL_STYLE = (-16);
            const long WS_THICKFRAME = 0x40000L;
            const long WS_MINIMIZEBOX = 0x00020000L;
            const long WS_MAXIMIZEBOX = 0x00010000L;

            System.Diagnostics.Process[] myProcesses;
            myProcesses = System.Diagnostics.Process.GetProcessesByName("IEXPLORE");
            foreach (System.Diagnostics.Process instance in myProcesses) {
                instance.Kill();
            }

            System.Diagnostics.Process.Start("iexplore.exe", "about:blank");
            System.Threading.Thread.Sleep(1500);
            //IntPtr ie = FindWindow("IEFrame", null);
            //IntPtr frame = FindWindowEx(ie, IntPtr.Zero, "Frame Tab", null);
            //IntPtr tab = FindWindowEx(frame, IntPtr.Zero, "TabWindowClass", null);
            //Rectangle rect = new Rectangle();
            //int rtn = GetWindowRect(tab, out rect);

            //System.Diagnostics.Process process = System.Diagnostics.Process.Start("iexplore.exe", "about:blank");
            //System.Threading.Thread.Sleep(1500);
            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindowsClass();
            foreach (SHDocVw.InternetExplorer Browser in shellWindows) {
                if (Browser.LocationURL.Contains("about:blank")) {

                    IntPtr frameTab = FindWindowEx((IntPtr)Browser.HWND, IntPtr.Zero, "Frame Tab", String.Empty);
                    IntPtr tabWindow = FindWindowEx(frameTab, IntPtr.Zero, "TabWindowClass", null);
                    Rectangle rectX = new Rectangle();
                    int rtnX = GetWindowRect(tabWindow, out rectX);
                    //Point point = new Point(rect.X, rect.Y);
                    //rtn = ClientToScreen(IntPtr.Zero, ref point);

                    //int x=0, y=0;
                    //Browser.ClientToWindow(ref x, ref y);
                    SetWindowPos((IntPtr)Browser.HWND, 0, 0, 0, 1000, 1100, 0x40);
                    long value = (long)GetWindowLong((IntPtr)Browser.HWND, GWL_STYLE);
                    SetWindowLong((IntPtr)Browser.HWND, GWL_STYLE, (int)(value & ~WS_MINIMIZEBOX & ~WS_MAXIMIZEBOX & ~WS_THICKFRAME));
                    Browser.MenuBar = false;
                    Browser.Top = 0;
                    Browser.Left = 0;
                    Browser.Height = 1000;
                    Browser.Width = 1100;
                    Browser.Navigate("http://moni.51hupai.org:8081");
                }
            }
            
            //LoginJob job = new LoginJob(this.m_orcLogin);
            //job.Execute();
        }

        private void buttonStep1_Click(object sender, EventArgs e) {

            SubmitPriceStep1Job job = new SubmitPriceStep1Job(this.m_orcCaptchaLoading, this.m_orcCaptchaTipsUtil, this.m_orcCaptcha);
            job.Execute();SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindowsClass();
        }

        private void RealToolStripMenuItem_Click(object sender, EventArgs e) {

            this.RealToolStripMenuItem.Checked = true;
            this.SimulateToolStripMenuItem.Checked = false;
            this.loadResource("real");
        }

        private void SimulateToolStripMenuItem_Click(object sender, EventArgs e) {

            this.RealToolStripMenuItem.Checked = false;
            this.SimulateToolStripMenuItem.Checked = true;
            this.loadResource("simulate");
        }

        public void acceptMessage(string msg) {
            throw new NotImplementedException();
        }
    }
}
