﻿using System;
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
using System.Configuration;
using tobid.rest.position;
using tobid.scheduler.jobs.action;

namespace Helper
{
    enum CaptchaInput {
        LEFT, MIDDLE, RIGHT,
        AUTO
    }

    public partial class Form1 : Form, IRepository, INotify
    {
        public Form1(){

            InitializeComponent();
        }

        public Form1(String endPoint, String timePos){

            this.EndPoint = endPoint;
            string[] pos = timePos.Split(new char[] { ',' });
            this.TimePos = new Point(Convert.ToInt16(pos[0]), Convert.ToInt16(pos[1]));
            InitializeComponent();
        }

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Form1));
        private String EndPoint { get; set; }
        private Point TimePos { get; set; }

        #region IRepository
        public String endPoint { get { return this.EndPoint; } }
        public IOrc orcTitle { get { return this.m_orcTitle; } }
        public IOrc orcCaptcha { get { return this.m_orcCaptcha; } }
        public IOrc orcPrice { get { return this.m_orcPrice; } }
        public IOrc orcPriceSM { get { return this.m_orcPriceSM; } }
        public IOrc orcCaptchaLoading { get { return this.m_orcCaptchaLoading; } }
        public IOrc[] orcCaptchaTip { get { return this.m_orcCaptchaTip; } }
        public Entry[] entries { get { return this.m_entries; } }
        public CaptchaUtil orcCaptchaTipsUtil { get { return this.m_orcCaptchaTipsUtil;} }
        public int interval { get { 
            return Int16.Parse(this.toolStripTextBoxInterval.Text); 
        } }
        public String category
        {
            get
            {
                if (this.国拍ToolStripMenuItem.Checked)
                    return "real";
                else
                    return "simulate";
            }
        }

        public GivePriceStep2 givePriceStep2 { get {
            return SubmitPriceStep2Job.getPosition().give;
        } }
        public SubmitPrice submitPrice { get {
            return SubmitPriceStep2Job.getPosition().submit;
        } }
        #endregion

        private IOrc m_orcLogin;
        private IOrc m_orcTitle;
        private IOrc m_orcCaptcha;
        private IOrc m_orcPrice;
        private IOrc m_orcPriceSM;
        private IOrc m_orcCaptchaLoading;
        private IOrc[] m_orcCaptchaTip;
        private Entry[] m_entries;
        private CaptchaUtil m_orcCaptchaTipsUtil;

        private Scheduler m_schedulerKeepAlive;
        private Scheduler m_schedulerSubmitStep2;
        private Scheduler m_schedulerSubmitStepV2;
        private Scheduler m_schedulerCustom;

        private System.Threading.Thread keepAliveThread;
        private System.Threading.Thread submitPriceStep2Thread;
        private System.Threading.Thread submitPriceV2Thread;
        private System.Threading.Thread customThread;

        private Step2ConfigDialog step2Dialog;
        private EntrySelForm entryForm;
        private Object lockSubmit = new Object();
        private Object lockPrice = new Object();

        private void Form1_Activated(object sender, EventArgs e) {

            //logger.Info("Application Form Activated");
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {
            
            if (null != this.keepAliveThread && (this.keepAliveThread.ThreadState == System.Threading.ThreadState.Running || this.keepAliveThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin))
                this.keepAliveThread.Abort();

            if (null != this.submitPriceStep2Thread && (this.submitPriceStep2Thread.ThreadState == System.Threading.ThreadState.Running || this.submitPriceStep2Thread.ThreadState == System.Threading.ThreadState.WaitSleepJoin))
                this.submitPriceStep2Thread.Abort();

            if (null != this.submitPriceV2Thread && (this.submitPriceV2Thread.ThreadState == System.Threading.ThreadState.Running || this.submitPriceV2Thread.ThreadState == System.Threading.ThreadState.WaitSleepJoin))
                this.submitPriceV2Thread.Abort();

            if (null != this.customThread && (this.customThread.ThreadState == System.Threading.ThreadState.Running || this.customThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin))
                this.customThread.Abort();

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
            Hotkey.UnregisterHotKey(this.Handle, 223);
            Hotkey.UnregisterHotKey(this.Handle, 224);

            logger.Info("Application Form Closed");
        }

        private void disableForm()
        {
            this.groupBoxPolicy.Enabled = false;
            this.panel3.Enabled = false;
        }
        private void enableForm()
        {
            this.groupBoxPolicy.Enabled = true;
            this.panel3.Enabled = true;
        }

        private void loadResource(String category)
        {
            //加载配置项1
            IGlobalConfig configResource = Resource.getInstance(this.EndPoint, category);//加载配置
            
            this.Text = configResource.tag;
            this.m_orcLogin = configResource.Login;
            this.m_orcTitle = configResource.Title;
            this.m_orcCaptcha = configResource.Captcha;//
            this.m_orcPrice = configResource.Price;//价格识别
            this.m_orcPriceSM = configResource.PriceSM;//价格（小）
            this.m_orcCaptchaLoading = configResource.Loading;//LOADING识别
            this.m_orcCaptchaTip = configResource.Tips;//验证码提示（文字）
            this.m_orcCaptchaTipsUtil = new CaptchaUtil(m_orcCaptchaTip);
            this.m_entries = configResource.Entries;

            //加载配置项2
            KeepAliveJob keepAliveJob = new KeepAliveJob(this.EndPoint,
                new ReceiveLogin(this.receiveLogin),
                new ReceiveOperation[]{
                        new ReceiveOperation(this.receiveOperation),
                        new ReceiveOperation(this.receiveOperation)},
                    this);
            keepAliveJob.Execute();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpszClass, string lpszWindow);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int GetWindowRect(IntPtr hwnd, out Rectangle lpRect);
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

        private void Form1_Load(object sender, EventArgs e){

            logger.Info("Application Form Load");

            Form.CheckForIllegalCrossThreadCalls = false;
            this.dateTimePicker1.Value = DateTime.Now;
            this.dateTimePicker2.Value = DateTime.Now;
            this.dateTimePickerCustomInputCaptcha.Value = DateTime.Now;
            this.dateTimePickerCustomPrice.Value = DateTime.Now;
            this.dateTimePickerCustomSubmitCaptcha.Value = DateTime.Now;

            this.step2Dialog = new Step2ConfigDialog(this);
            this.entryForm = new EntrySelForm();

            String keyInterval = System.Configuration.ConfigurationManager.AppSettings["KeyInterval"];
            this.toolStripTextBoxInterval.Text = keyInterval;

            try
            {
                this.loadResource("real");
                this.enableForm();
            }
            catch (System.Net.WebException webEx)
            {
                MessageBoxButtons messButton = MessageBoxButtons.OK;
                if(webEx.Status == System.Net.WebExceptionStatus.ConnectFailure)
                    MessageBox.Show(webEx.InnerException.ToString(), "网络连接异常", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                if(webEx.Status == System.Net.WebExceptionStatus.ProtocolError)
                    MessageBox.Show("请按[菜单]->[配置]->[授权码]步骤，输入有效的授权码", "需要授权码", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);

                this.disableForm();
                logger.Error(webEx);
            }
            catch (Exception ex)
            {
                this.disableForm();
                logger.Error(ex);
            }

            Boolean isOk = false;
            isOk = Hotkey.RegisterHotKey(this.Handle, 103, Hotkey.KeyModifiers.Ctrl, Keys.D3);
            isOk = Hotkey.RegisterHotKey(this.Handle, 104, Hotkey.KeyModifiers.Ctrl, Keys.D4);
            isOk = Hotkey.RegisterHotKey(this.Handle, 105, Hotkey.KeyModifiers.Ctrl, Keys.D5);
            isOk = Hotkey.RegisterHotKey(this.Handle, 106, Hotkey.KeyModifiers.Ctrl, Keys.D6);
            isOk = Hotkey.RegisterHotKey(this.Handle, 107, Hotkey.KeyModifiers.Ctrl, Keys.D7);
            isOk = Hotkey.RegisterHotKey(this.Handle, 108, Hotkey.KeyModifiers.Ctrl, Keys.D8);
            isOk = Hotkey.RegisterHotKey(this.Handle, 109, Hotkey.KeyModifiers.Ctrl, Keys.D9);

            isOk = Hotkey.RegisterHotKey(this.Handle, 202, Hotkey.KeyModifiers.Ctrl, Keys.Up);
            isOk = Hotkey.RegisterHotKey(this.Handle, 201, Hotkey.KeyModifiers.Ctrl, Keys.Left);
            isOk = Hotkey.RegisterHotKey(this.Handle, 203, Hotkey.KeyModifiers.Ctrl, Keys.Right);
            isOk = Hotkey.RegisterHotKey(this.Handle, 204, Hotkey.KeyModifiers.Ctrl, Keys.Enter);

            isOk = Hotkey.RegisterHotKey(this.Handle, 221, Hotkey.KeyModifiers.None, Keys.Escape);
            isOk = Hotkey.RegisterHotKey(this.Handle, 222, Hotkey.KeyModifiers.None, Keys.Enter);

            isOk = Hotkey.RegisterHotKey(this.Handle, 223, Hotkey.KeyModifiers.None, Keys.F9);
            isOk = Hotkey.RegisterHotKey(this.Handle, 224, Hotkey.KeyModifiers.None, Keys.F11);

            //keepAlive任务配置
            SchedulerConfiguration config1M = new SchedulerConfiguration(1000 * 60 * 1);
            config1M.Job = new KeepAliveJob(this.EndPoint,
                new ReceiveLogin(this.receiveLogin), 
                new ReceiveOperation[]{
                    new ReceiveOperation(this.receiveOperation),
                    new ReceiveOperation(this.receiveOperation)},
                this);
            this.m_schedulerKeepAlive = new Scheduler(config1M);

            //Action任务配置
            SchedulerConfiguration configStep2 = new SchedulerConfiguration(1000);
            configStep2.Job = new SubmitPriceStep2Job(repository: this, notify: this);
            m_schedulerSubmitStep2 = new Scheduler(configStep2);

            SchedulerConfiguration configStepV2 = new SchedulerConfiguration(1000);
            configStepV2.Job = new SubmitPriceV2Job(repository: this, notify: this);
            m_schedulerSubmitStepV2 = new Scheduler(configStepV2);

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
                            logger.Info("HOT KEY CTRL + 3 (103) trigger");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 300);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 104://CTRL+4
                            logger.Info("HOT KEY CTRL + 4 (104) trigger");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 400);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 105://CTRL+5
                            logger.Info("HOT KEY CTRL + 5 (105) trigger");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 500);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 106://CTRL+6
                            logger.Info("HOT KEY CTRL + 6 (106) trigger");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 600);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 107://CTRL+7
                            logger.Info("HOT KEY CTRL + 7 (107) trigger");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 700);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 108://CTRL+8
                            logger.Info("HOT KEY CTRL + 8 (108) trigger");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 800);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 109://CTRL+9
                            logger.Info("HOT KEY CTRL + 9 (109) trigger");
                            this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 900);
                            //ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 201://LEFT
                            logger.Info("HOT KEY CTRL + LEFT (201) trigger");
                            this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.LEFT);
                            break;
                        case 202://UP
                            logger.Info("HOT KEY CTRL + UP (202) trigger");
                            this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.MIDDLE);
                            break;
                        case 203://RIGHT
                            logger.Info("HOT KEY CTRL + RIGIHT (203) trigger");
                            this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.RIGHT);
                            break;
                        case 204://CTRL + ENTER
                            logger.Info("HOT KEY CTRL + ENTER (204) trigger");
                            this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.AUTO);
                            break;
                        case 221://ESC
                            logger.Info("HOT KEY ESCAPE (221) trigger");
                            this.closeDialog(SubmitPriceStep2Job.getPosition());
                            break;
                        case 222://ENTER
                            logger.Info("HOT KEY ENTER (222) trigger");
                            //this.submitCaptcha(SubmitPriceStep2Job.getPosition());
                            this.processEnter(SubmitPriceStep2Job.getPosition());
                            break;
                        case 223://F9
                            logger.Info("HOT KEY F9 (223) trigger, +300");
                            this.fire(SubmitPriceStep2Job.getPosition(), 300);
                            break;
                        case 224://F12
                            logger.Info("HOT KEY F12 (224) trigger, +1000");
                            exam4Fire(1000);
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        private void receiveLogin(Client client) {

            Config config = client.config;
            if ( config != null) {

                this.groupBox1.Text = String.Format("标书:{0}", config.pname);
                this.groupBox1.Enabled = true;
                this.textBoxBNO.Text = config.no;
                this.textBoxBPass.Text = config.passwd;
                this.textBoxPID.Text = config.pid;

            } else {

                this.groupBox1.Text = "标书:NULL";
                this.groupBox1.Enabled = false;
                this.textBoxBNO.Text = "";
                this.textBoxBPass.Text = "";
                this.textBoxPID.Text = "";
            }

            if (!String.IsNullOrEmpty(client.memo)) {

                this.toolStripStatusLabel3.Text = client.memo;
                MessageBoxButtons messButton = MessageBoxButtons.OK;
                DialogResult dr = MessageBox.Show(client.memo, "操作策略", messButton);;
            }else
                this.toolStripStatusLabel3.Text = "";
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
            new ScreenUtil().drawSomething(this.TimePos.X, this.TimePos.Y, DateTime.Now.ToString("HH:mm:ss"));
        }

        public void acceptMessage(String msg){

            this.toolStripStatusLabel1.Text = msg;
        }

        #region 拍牌ACTION

        /// <summary>
        /// 获取当前价格，+delta，出价.【for CTRL+3、4、5、6、7、8、9】
        /// </summary>
        /// <param name="givePrice">坐标</param>
        /// <param name="delta">差价</param>
        private void giveDeltaPrice(tobid.rest.position.BidStep2 bid, int delta) {

            KeyBoardUtil.simulateKeyUP("LCONTROL");
            KeyBoardUtil.simulateKeyUP("RCONTROL");

            System.Threading.Thread startPrice = new System.Threading.Thread(delegate() {

                if (System.Threading.Monitor.TryEnter(this.lockPrice)) {

                    int interval = this.interval;
                    Point origin = findOrigin();
                    int x = origin.X;
                    int y = origin.Y;

                    //INPUT BOX
                    ScreenUtil.SetCursorPos(x + bid.give.inputBox.x, y + bid.give.inputBox.y);
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                    System.Threading.Thread.Sleep(50);

                    SendKeys.SendWait("{BACKSPACE 5}");
                    SendKeys.SendWait("{DEL 5}");

                    System.Threading.Thread.Sleep(50);

                    logger.WarnFormat("BEGIN givePRICE(delta : {0})", delta);
                    logger.Info("\tBEGIN identify PRICE...");
                    byte[] content = new ScreenUtil().screenCaptureAsByte(x + bid.give.price.x, y + bid.give.price.y, 52, 18);
                    String txtPrice = this.m_orcPrice.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
                    int price = Int32.Parse(txtPrice);
                    price += delta;
                    logger.InfoFormat("\tEND   identified PRICE = {0}", txtPrice);
                    txtPrice = String.Format("{0:D}", price);

                    logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
                    KeyBoardUtil.sendMessage(txtPrice, interval);
                    System.Threading.Thread.Sleep(100);

                    logger.Info("\tEND   input PRICE");

                    //点击出价
                    logger.Info("\tBEGIN click BUTTON[出价]");
                    ScreenUtil.SetCursorPos(x + bid.give.button.x, y + bid.give.button.y);
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                    logger.Info("\tEND   click BUTTON[出价]");
                    logger.Info("END   givePRICE");

                    System.Threading.Monitor.Exit(this.lockPrice);
                } else
                    logger.Error("PRICE Lock is not released");

            });
            startPrice.SetApartmentState(System.Threading.ApartmentState.STA);
            startPrice.Start();
        }

        private void closeDialog(tobid.rest.position.BidStep2 bid) {

            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;

            logger.Info("关闭校验码窗口");
            ScreenUtil.SetCursorPos(x + bid.submit.buttons[1].x, y + bid.submit.buttons[1].y);//取消按钮
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
        }

        private void submitCaptcha(tobid.rest.position.BidStep2 bid) {

            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;

            logger.Info("提交验证码");
            ScreenUtil.SetCursorPos(x+bid.submit.buttons[0].x, y+bid.submit.buttons[0].y);//确定按钮
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
        }

        private void processEnter(tobid.rest.position.BidStep2 bid) {

            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;

            byte[] title = new ScreenUtil().screenCaptureAsByte(x + bid.title.x, y + bid.title.y, 170, 50);
            File.WriteAllBytes("TITLE.bmp", title);
            Bitmap bitTitle = new Bitmap(new MemoryStream(title));
            String strTitle = this.m_orcTitle.IdentifyStringFromPic(bitTitle);
            logger.Debug(strTitle);
            if ("系统提示".Equals(strTitle)){

                logger.Info("proces Enter in [系统提示]");
                ScreenUtil.SetCursorPos(x + bid.okButton.x, y + bid.okButton.y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            } else if ("投标拍卖".Equals(strTitle)) {

                logger.Info("proces Enter in [投标拍卖]");
                this.submitCaptcha(SubmitPriceStep2Job.getPosition());
            }
        }

        private void fire(tobid.rest.position.BidStep2 bid, int delta) {

            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;

            ScreenUtil.SetCursorPos(x + bid.okButton.x, y + bid.okButton.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(150);

            SubmitPriceStep2Job job = new SubmitPriceStep2Job(repository: this, notify: this);
            job.Fire(delta);
        }

        private void exam4Fire(int delta) {

            String fire1 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            String fire2 = DateTime.Now.AddSeconds(1).ToString("yyyy-MM-dd HH:mm:ss");

            List<Task> tasks = new List<Task>();
            Task taskInputPrice = new tobid.scheduler.jobs.action.Task(
                action: new tobid.scheduler.jobs.action.InputPriceAction(delta: delta, repo: this),
                notify: this,
                fireTime: fire1);
            tasks.Add(taskInputPrice);

            InputCaptchaAction actionInputCaptcha = new tobid.scheduler.jobs.action.InputCaptchaAction(repo: this);
            Task taskInputCaptcha = new Task(action: actionInputCaptcha, notify: this, fireTime: fire2); 
            tasks.Add(taskInputCaptcha);

            CustomJob job = new CustomJob(tasks: tasks);
            job.Execute();
            System.Threading.Thread.Sleep(2000);
            job.Execute();
        }

        private void submit(tobid.rest.position.BidStep2 bid, String activeCaptcha) {

            ScreenUtil.keybd_event(ScreenUtil.keycode["CTRL"], 0, 0x2, 0);//释放CTRL
            int interval = this.interval;
            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;

            logger.WarnFormat("BEGIN submitCAPTCHA({0})", activeCaptcha);

            logger.Info("\tBEGIN make INPUT blank");
            logger.DebugFormat("\tINPUT BOX({0}, {1})", x + bid.submit.inputBox.x, y + bid.submit.inputBox.y);
            ScreenUtil.SetCursorPos(x + bid.submit.inputBox.x, y + bid.submit.inputBox.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(50);

            SendKeys.SendWait("{BACKSPACE 4}");
            SendKeys.SendWait("{DEL 4}");
            logger.Info("\tEND   make INPUT blank");

            logger.Info("\tBEGIN input CAPTCHA");
            {
                for (int i = 0; i < activeCaptcha.Length; i++) {

                    System.Threading.Thread.Sleep(interval);
                    ScreenUtil.keybd_event(ScreenUtil.keycode[activeCaptcha[i].ToString()], 0, 0, 0);
                    System.Threading.Thread.Sleep(interval);
                    ScreenUtil.keybd_event(ScreenUtil.keycode[activeCaptcha[i].ToString()], 0, 0x2, 0);
                }
            } System.Threading.Thread.Sleep(50);
            logger.Info("\tEND   input CAPTCHA");

            logger.Info("\tBEGIN click BUTTON[确定]");
            logger.DebugFormat("\tBUTTON[确定]({0}, {1})", x + bid.submit.buttons[0].x, y + bid.submit.buttons[0].y);
            ScreenUtil.SetCursorPos(x + bid.submit.buttons[0].x, y + bid.submit.buttons[0].y);
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
        private void submit(String URL, tobid.rest.position.BidStep2 bid, CaptchaInput input) {

            //ScreenUtil.keybd_event(ScreenUtil.keycode["CTRL"], 0, 0x2, 0);
            KeyBoardUtil.simulateKeyUP("LCONTROL");
            KeyBoardUtil.simulateKeyUP("RCONTROL");

            System.Threading.Thread startSubmit = new System.Threading.Thread(delegate() {

                if (System.Threading.Monitor.TryEnter(this.lockSubmit)) {

                    int interval = this.interval;
                    Point origin = findOrigin();
                    int x = origin.X;
                    int y = origin.Y;

                    logger.WarnFormat("BEGIN submitCAPTCHA({0})", input);
                    logger.Info("\tBEGIN make INPUT blank");
                    logger.DebugFormat("\tINPUT BOX({0}, {1})", x + bid.submit.inputBox.x, y + bid.submit.inputBox.y);
                    ScreenUtil.SetCursorPos(x + bid.submit.inputBox.x, y + bid.submit.inputBox.y);
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                    System.Threading.Thread.Sleep(50);

                    SendKeys.SendWait("{BACKSPACE 4}");
                    SendKeys.SendWait("{DEL 4}");
                    logger.Info("\tEND   make INPUT blank");

                    logger.Info("\tBEGIN identify CAPTCHA...");
                    logger.DebugFormat("\tCAPTURE CAPTCHA({0}, {1})", x + bid.submit.captcha[0].x, y + bid.submit.captcha[0].y);
                    byte[] binaryCaptcha = new ScreenUtil().screenCaptureAsByte(x + bid.submit.captcha[0].x, y + bid.submit.captcha[0].y, 148, 28);
                    File.WriteAllBytes("CAPTCHA.BMP", binaryCaptcha);
                    String txtCaptcha = this.m_orcCaptcha.IdentifyStringFromPic(new Bitmap(new MemoryStream(binaryCaptcha)));

                    logger.DebugFormat("\tCAPTURE TIPS({0}, {1})", x + bid.submit.captcha[1].x, y + bid.submit.captcha[1].y);
                    byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(x + bid.submit.captcha[1].x, y + bid.submit.captcha[1].y, 132, 16);
                    File.WriteAllBytes("TIPS.BMP", binaryTips);
                    String txtActive = this.m_orcCaptchaTipsUtil.getActive(txtCaptcha, new Bitmap(new MemoryStream(binaryTips)));
                    logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}", txtCaptcha, txtActive);

                    
                    logger.Info("\tBEGIN input CAPTCHA");
                    {
                        if (CaptchaInput.LEFT == input) {

                            KeyBoardUtil.sendMessage(txtCaptcha.Substring(0, 4), this.interval);
                        }
                        if (CaptchaInput.MIDDLE == input) {

                            KeyBoardUtil.sendMessage(txtCaptcha.Substring(1, 4), this.interval);
                        }
                        if (CaptchaInput.RIGHT == input) {

                            KeyBoardUtil.sendMessage(txtCaptcha.Substring(2, 4), this.interval);
                        }
                        if (CaptchaInput.AUTO == input) {

                            KeyBoardUtil.sendMessage(txtActive, this.interval);
                        }
                    }
                    System.Threading.Thread.Sleep(100);
                    logger.Info("\tEND   input CAPTCHA");

                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                    System.Threading.Thread.Sleep(50);
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

                    System.Threading.Monitor.Exit(this.lockSubmit);
                    return;

                    MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
                    DialogResult dr = MessageBox.Show("确定要提交出价吗?", "提交出价", messButton, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    if (dr == DialogResult.OK) {

                        logger.InfoFormat("用户选择确定出价");
                        logger.DebugFormat("\tBUTTON[确定]({0}, {1})", x + bid.submit.buttons[0].x, y + bid.submit.buttons[0].y);
                        ScreenUtil.SetCursorPos(x + bid.submit.buttons[0].x, y + bid.submit.buttons[0].y);//确定按钮
                        ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                        logger.Info("END   submitCAPTCHA");
                    } else {

                        logger.InfoFormat("用户选择取消出价");
                        logger.DebugFormat("\tBUTTON[取消]({0}, {1})", x + bid.submit.buttons[0].x + 188, y + bid.submit.buttons[0].y);
                        ScreenUtil.SetCursorPos(x + bid.submit.buttons[1].x, y + bid.submit.buttons[1].y);//取消按钮
                        ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                        logger.Info("END   submitCAPTCHA");
                    }
                } else
                    logger.Error("SUBMIT Lock is not released!");
            });
            startSubmit.SetApartmentState(System.Threading.ApartmentState.STA);
            startSubmit.Start();
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

        private void buttonUpdateCustom_Click(object sender, EventArgs e) {

            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("确定要更新出价策略吗?", "更新策略",
                messButton, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (dr == DialogResult.OK){

                this.textBox1.Text = this.dateTimePickerCustomPrice.Value.ToString("MM/dd HH:mm:ss");
                object obj = this.comboBoxCustomDelta.SelectedItem;
                if (obj == null) {
                    MessageBox.Show("请选择价格");
                    this.comboBoxCustomDelta.Focus();
                    return;
                }
                this.textBox2.Text = this.comboBoxCustomDelta.Text;

                String fire1 = this.dateTimePickerCustomPrice.Value.ToString("yyyy-MM-dd HH:mm:ss");
                String fire2 = this.dateTimePickerCustomInputCaptcha.Value.ToString("yyyy-MM-dd HH:mm:ss");
                String fire3 = this.dateTimePickerCustomSubmitCaptcha.Value.ToString("yyyy-MM-dd HH:mm:ss");

                List<Task> tasks = new List<Task>();
                Task taskInputPrice = new tobid.scheduler.jobs.action.Task(
                    action: new tobid.scheduler.jobs.action.InputPriceAction(delta: Int32.Parse(this.comboBoxCustomDelta.Text), repo: this),
                    notify: this,
                    fireTime: fire1); tasks.Add(taskInputPrice);

                if (checkBoxInputCaptcha.Checked) {

                    InputCaptchaAction actionInputCaptcha = new tobid.scheduler.jobs.action.InputCaptchaAction(repo: this);
                    SubmitCaptchaAction actionSubmitCaptcha = new tobid.scheduler.jobs.action.SubmitCaptchaAction(repo: this);
                    if (checkBoxSubmitCaptcha.Checked) {
                        Task taskInputCaptcha = new Task(action: actionInputCaptcha, notify: this, fireTime: fire2); tasks.Add(taskInputCaptcha);
                        Task taskSubmitCaptcha = new Task(action: actionSubmitCaptcha, notify: this, fireTime: fire3); tasks.Add(taskSubmitCaptcha);
                    } else {

                        //SequenceAction actionSeq = new SequenceAction(new List<IBidAction>() { actionInputCaptcha, actionSubmitCaptcha });
                        //Task taskCaptcha = new Task(action: actionSeq, notify: this, fireTime: fire2); tasks.Add(taskCaptcha);
                        Task taskInputCaptcha = new Task(action: actionInputCaptcha, notify: this, fireTime: fire2); tasks.Add(taskInputCaptcha);
                    }

                } else {

                    SubmitCaptchaAction actionSubmitCaptcha = new tobid.scheduler.jobs.action.SubmitCaptchaAction(repo: this);
                    if (checkBoxSubmitCaptcha.Checked) {
                        Task taskSubmitCaptcha = new Task(action: actionSubmitCaptcha, notify: this, fireTime: fire3); tasks.Add(taskSubmitCaptcha);
                    }
                }

                if (null != this.customThread)
                    this.customThread.Abort();

                SchedulerConfiguration customConf = new SchedulerConfiguration(1000);
                customConf.Job = new CustomJob(tasks: tasks);
                this.m_schedulerCustom = new Scheduler(customConf);

                System.Threading.ThreadStart customThreadStart = new System.Threading.ThreadStart(this.m_schedulerCustom.Start);
                this.customThread = new System.Threading.Thread(customThreadStart);
                this.customThread.Name = "customThread";
                this.customThread.Start();

                
            }
        }

        private void btnUpdateV2_Click(object sender, EventArgs e)
        {
            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("确定要更新出价策略吗?", "更新策略",
                messButton, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (dr == DialogResult.OK)
            {
                Step2Operation bidOps = SubmitPriceStep2Job.bidOperation;
                bidOps.updateTime = DateTime.Now;
                bidOps.startTime = this.dateTimePicker2.Value;
                bidOps.expireTime = bidOps.startTime.AddHours(1);

                this.textBox1.Text = this.dateTimePicker2.Value.ToString("MM/dd HH:mm:ss");
                int delay = Int32.Parse(this.textBoxDelay.Text);

                {
                    object obj = this.comboBoxDelta.SelectedItem;
                    if (obj == null)
                    {
                        MessageBox.Show("请选择价格");
                        this.comboBoxDelta.Focus();
                        return;
                    }
                    this.textBox2.Text = this.comboBoxDelta.Text;
                    bidOps.price = Int32.Parse(this.comboBoxDelta.Text);
                    SubmitPriceV2Job.setConfig(bidOps, delay * 1000);
                }

                if (null != this.submitPriceV2Thread)
                    this.submitPriceV2Thread.Abort();
                System.Threading.ThreadStart submitPriceThreadStart = new System.Threading.ThreadStart(this.m_schedulerSubmitStepV2.Start);
                this.submitPriceV2Thread = new System.Threading.Thread(submitPriceThreadStart);
                this.submitPriceV2Thread.Name = "submitPriceV2Thread";
                this.submitPriceV2Thread.Start();
            }

        }

        private void button_updatePolicy_Click(object sender, EventArgs e) {

            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            //DialogResult dr = MessageBox.Show("确定要更新出价策略吗?", "更新策略", 
            //    messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            DialogResult dr = MessageBox.Show("确定要更新出价策略吗?", "更新策略", 
                messButton, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
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
                    SubmitPriceStep2Job.setConfig(bidOps, this.checkPriceOnly.Checked, updatePos:false);
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
        /*protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {

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
        }*/
        #endregion

        #region 菜单ACTION
        private void 国拍ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.国拍ToolStripMenuItem.Checked = true;
            this.模拟ToolStripMenuItem.Checked = false;
            try
            {
                this.loadResource("real");
                this.enableForm();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                this.disableForm();
            }
        }

        private void 模拟ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.国拍ToolStripMenuItem.Checked = false;
            this.模拟ToolStripMenuItem.Checked = true;
            try
            {
                this.loadResource("simulate");
                this.enableForm();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                this.disableForm();
            }
        }

        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.step2Dialog.ShowDialog(this);
        }

        private void ToolStripMenuItemAuthCode_Click(object sender, EventArgs e)
        {
            String credential = ConfigurationManager.AppSettings["credential"];
            String authCode = Microsoft.VisualBasic.Interaction.InputBox("请输入授权码", "", credential);
            if (!String.IsNullOrEmpty(authCode))
            {
                Warrant warrant = new Warrant(authCode);
                String hostName = System.Net.Dns.GetHostName();
                String epKeepAlive = this.EndPoint + "/rest/service/command/register/" + hostName;
                RestClient rest = new RestClient(epKeepAlive, HttpVerb.POST, warrant);
                try
                {
                    String rtn = rest.MakeRequest(null, false);

                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["principal"].Value = hostName;
                    config.AppSettings.Settings["credential"].Value = authCode;
                    config.Save();
                    ConfigurationManager.RefreshSection("appSettings");

                    MessageBoxButtons messButton = MessageBoxButtons.OK;
                    DialogResult dr = MessageBox.Show(String.Format("请重新启动应用程序!\r\n主机名:{0}\r\n授权码:{1}", hostName, authCode), "授权成功", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    MessageBoxButtons messButton = MessageBoxButtons.OK;
                    DialogResult dr = MessageBox.Show("请重新输入授权码!", "授权失败", messButton, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                }
            }
        }
        #endregion

        #region 网页ACTION
        private void buttonIE_Click(object sender, EventArgs e) {

            IEUtil.openIE(this.category);
        }

        private void buttonURL_Click(object sender, EventArgs e) {

            if (this.entries.Length > 1) {
                this.entryForm.Entries = this.entries;
                this.entryForm.ShowDialog(this);
                if(this.entryForm.SelectedEntry != null)
                    IEUtil.openURL(this.category, this.entryForm.SelectedEntry);
            } else
                IEUtil.openURL(this.category, this.entries[0]);
            
        }

        private void buttonLogin_Click_1(object sender, EventArgs e)
        {
            LoginJob job = new LoginJob(m_orcLogin);
            job.Execute();
        }
        #endregion

        #region tabControl事件
        private TabPage lastSelTab;
        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e) {

            String selecting = this.tabControl1.SelectedTab.Text;
            System.Console.WriteLine("selecting " + selecting);

            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show(String.Format("确定使用{0}吗?", selecting), "策略选择",
                messButton, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (dr == DialogResult.OK) {

                if(this.groupBoxLocal.Text.Equals(selecting)){
                    this.groupBoxLocal.Enabled = true;
                    this.groupBoxLocalV2.Enabled = false;
                    this.groupBoxCustom.Enabled = false;
                } 
                if(this.groupBoxLocalV2.Text.Equals(selecting)){

                    this.groupBoxLocal.Enabled = false;
                    this.groupBoxLocalV2.Enabled = true;
                    this.groupBoxCustom.Enabled = false;
                }
                if (this.groupBoxCustom.Text.Equals(selecting)) {

                    this.groupBoxLocal.Enabled = false;
                    this.groupBoxLocalV2.Enabled = false;
                    this.groupBoxCustom.Enabled = true;
                }

                if (null != this.keepAliveThread)
                    this.keepAliveThread.Abort();

                if (null != this.submitPriceStep2Thread)
                    this.submitPriceStep2Thread.Abort();

                if (null != this.submitPriceV2Thread)
                    this.submitPriceV2Thread.Abort();

                if (null != this.customThread)
                    this.customThread.Abort();
            } else
                e.Cancel = true;
            
        }

        private void tabControl1_Deselected(object sender, TabControlEventArgs e) {

            String deSelected = this.tabControl1.SelectedTab.Text;
            System.Console.WriteLine("deSelected " + deSelected);
            lastSelTab = this.tabControl1.SelectedTab;
        }
        #endregion

        private void checkBoxInputCaptcha_CheckedChanged(object sender, EventArgs e) {
            if (this.checkBoxInputCaptcha.Checked)
                this.dateTimePickerCustomInputCaptcha.Enabled = true;
            else
                this.dateTimePickerCustomInputCaptcha.Enabled = false;
        }

        private void checkBoxSubmitCaptcha_CheckedChanged(object sender, EventArgs e) {
            if (this.checkBoxSubmitCaptcha.Checked)
                this.dateTimePickerCustomSubmitCaptcha.Enabled = true;
            else
                this.dateTimePickerCustomSubmitCaptcha.Enabled = false;
        }

    }
}
