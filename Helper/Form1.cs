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
using System.Configuration;
using tobid.rest.position;
using tobid.scheduler.jobs.action;
using tobid.util.hook;
using System.Threading;

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

        public Form1(String endPoint, String timePos, IntPtr consoleHWND){

            this.EndPoint = endPoint;
            this.ConsoleHWND = consoleHWND;

            string[] pos = timePos.Split(new char[] { ',' });
            this.TimePos = new Point(Convert.ToInt16(pos[0]), Convert.ToInt16(pos[1]));
            InitializeComponent();
        }

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Form1));
        private String EndPoint { get; set; }
        private IntPtr ConsoleHWND { get; set; }

        #region IRepository
        public Boolean deltaPriceOnUI { get; set; }
        public Point TimePos { get; set; }
        public String endPoint { get { return this.EndPoint; } }
        public IOrc orcTitle { get { return this.m_orcTitle; } }
        public IOrc orcCaptcha { get { return this.m_orcCaptcha; } }
        public IOrc orcPrice { get { return this.m_orcPrice; } }
        public IOrc orcPriceSM { get { return this.m_orcPriceSM; } }
        public IOrc orcTime { get { return this.m_orcTime; } }
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

        public GivePriceStep2 givePriceStep2 { get { return SubmitPriceStep2Job.getPosition().give; } }
        public SubmitPrice submitPrice { get { return SubmitPriceStep2Job.getPosition().submit; } }

        public DateTime lastSubmit { get; set; }
        public TimeSpan lastCost { get; set; }
        #endregion

        private IOrc m_orcLogin;
        private IOrc m_orcTitle;
        private IOrc m_orcCaptcha;
        private IOrc m_orcPrice;
        private IOrc m_orcPriceSM;
        private IOrc m_orcTime;
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
        private System.Threading.Thread wifiMonitorThread;

        private Step2ConfigDialog step2Dialog;
        private FloatingForm floatingForm;
        private EntrySelForm entryForm;
        private Object lockSubmit = new Object();
        private Object lockPrice = new Object();

        private KeyboardHook kh;
        private String triggerF11;
        private String triggerSetPolicy;
        private String triggerLoadResource;
        private String m_submitHotKey;

        private IDictionary<int, int> m_F9Strategy;

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

            if (null != this.wifiMonitorThread)
                this.wifiMonitorThread.Abort();

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
            Hotkey.UnregisterHotKey(this.Handle, 225);

            if(null != kh)
                kh.UnHook();

            logger.Info("Application Form Closed");
        }

        private void disableForm(){
            this.groupBoxPolicy.Enabled = false;
            this.panelIE.Enabled = false;
            this.groupBoxInternetTime.Enabled = false;

        }

        private void enableForm(){
            this.groupBoxPolicy.Enabled = true;
            this.panelIE.Enabled = true;
            this.groupBoxInternetTime.Enabled = true;
        }

        private void loadResource(String category){

            this.m_F9Strategy = new Dictionary<int, int>();
            this.m_F9Strategy.Add(50, 500);
            this.m_F9Strategy.Add(51, 500);
            this.m_F9Strategy.Add(52, 400);
            this.m_F9Strategy.Add(53, 400);
            this.m_F9Strategy.Add(54, 300);
            this.m_F9Strategy.Add(55, 300);
            this.m_F9Strategy.Add(56, 300);
            this.m_F9Strategy.Add(57, 300);
            this.m_F9Strategy.Add(58, 300);
            this.m_F9Strategy.Add(59, 300);

            //加载配置项1
            IGlobalConfig configResource = Resource.getInstance(this.EndPoint, category);//加载配置
            
            this.Text = configResource.tag;
            this.m_orcLogin = configResource.Login;
            this.m_orcTitle = configResource.Title;
            this.m_orcCaptcha = configResource.Captcha;//
            this.m_orcPrice = configResource.Price;//价格识别
            this.m_orcPriceSM = configResource.PriceSM;//价格（小）
            this.m_orcTime = configResource.Time;
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

        
        private void Form1_Load(object sender, EventArgs e) {

            Version localVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            logger.Info("Helper Version : " + localVer.ToString());

            //HELPER FORM位置设置
            this.Location = new Point(Screen.PrimaryScreen.Bounds.Width - this.Width, Screen.PrimaryScreen.Bounds.Height - this.Height - 150);
            //DEBUG窗口位置设置
            if (this.ConsoleHWND != IntPtr.Zero) {

                int SWP_NOSIZE = 1;
                Rect rect = new Rect();
                WindowHelper.GetWindowRect(this.ConsoleHWND, out rect);
                WindowHelper.SetWindowPos(this.ConsoleHWND, 0,
                    Screen.PrimaryScreen.Bounds.Width - (rect.Right - rect.Left), 0, 0, 0, SWP_NOSIZE);
            }
            //检查新版本
            {
                MessageBoxButtons messButton = MessageBoxButtons.OK;
                Version remoteVer = null;
                try {

                    String endPoint = ConfigurationManager.AppSettings["CHECKUPDATE"];
                    String ver = new HttpUtil().getAsPlain(endPoint + "/Release.ver");
                    remoteVer = new Version(ver);
                }
                catch (Exception ex) {
                    System.Console.WriteLine(ex);
                    MessageBox.Show(String.Format("软件为最新版. {0}", localVer), "UP TO DATE", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                    return;
                }

                if (remoteVer.CompareTo(localVer) > 0)
                    MessageBox.Show(String.Format("请更新软件\r\nREMOTE:{0}\r\nLOCAL:{1}", remoteVer, localVer), "发现新版本", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            }

            //关闭KK录像机
            {
                //System.Diagnostics.Process[] myProcesses = System.Diagnostics.Process.GetProcessesByName("KK");
                //foreach (System.Diagnostics.Process instance in myProcesses)
                //    instance.Kill();
            }

            this.triggerF11 = ConfigurationManager.AppSettings["triggerF11"];
            this.triggerSetPolicy = ConfigurationManager.AppSettings["triggerSetPolicyCustom"];
            this.triggerLoadResource = ConfigurationManager.AppSettings["triggerLoadResource"];

            //键盘HOOK
            kh = new KeyboardHook();
            kh.SetHook();
            kh.OnKeyDownEvent += kh_OnKeyDownEvent;

            Form.CheckForIllegalCrossThreadCalls = false;
            this.dateTimePicker1.Value = DateTime.Now;
            this.dateTimePicker2.Value = DateTime.Now;
            this.dateTimePickerCustomInputCaptcha.Value = DateTime.Now;
            this.dateTimePickerCustomPrice.Value = DateTime.Now;
            this.dateTimePickerCustomSubmitCaptcha.Value = DateTime.Now;

            this.step2Dialog = new Step2ConfigDialog(this);
            this.floatingForm = new FloatingForm();
            this.entryForm = new EntrySelForm();

            String keyInterval = System.Configuration.ConfigurationManager.AppSettings["KeyInterval"];
            this.toolStripTextBoxInterval.Text = keyInterval;

            try {
                this.loadResource("real");
                this.enableForm();
            }
            catch (System.Net.WebException webEx) {
                MessageBoxButtons messButton = MessageBoxButtons.OK;
                if (webEx.Status == System.Net.WebExceptionStatus.ConnectFailure)
                    MessageBox.Show(webEx.InnerException.ToString(), "网络连接异常", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                if (webEx.Status == System.Net.WebExceptionStatus.ProtocolError)
                    MessageBox.Show("请按[菜单]->[配置]->[授权码]步骤，输入有效的授权码", "需要授权码", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);

                this.disableForm();
                logger.Error(webEx);
            }
            catch (Exception ex) {
                this.disableForm();
                logger.Error(ex);
            }

            this.m_submitHotKey = System.Configuration.ConfigurationManager.AppSettings["SubmitHotKey"];
            Boolean isOk = false;
            //isOk = Hotkey.RegisterHotKey(this.Handle, 103, Hotkey.KeyModifiers.Ctrl, Keys.D3);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 104, Hotkey.KeyModifiers.Ctrl, Keys.D4);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 105, Hotkey.KeyModifiers.Ctrl, Keys.D5);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 106, Hotkey.KeyModifiers.Ctrl, Keys.D6);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 107, Hotkey.KeyModifiers.Ctrl, Keys.D7);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 108, Hotkey.KeyModifiers.Ctrl, Keys.D8);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 109, Hotkey.KeyModifiers.Ctrl, Keys.D9);

            //isOk = Hotkey.RegisterHotKey(this.Handle, 202, Hotkey.KeyModifiers.Ctrl, Keys.Up);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 201, Hotkey.KeyModifiers.Ctrl, Keys.Left);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 203, Hotkey.KeyModifiers.Ctrl, Keys.Right);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 204, Hotkey.KeyModifiers.Ctrl, Keys.Enter);

            //isOk = Hotkey.RegisterHotKey(this.Handle, 221, Hotkey.KeyModifiers.None, Keys.Escape);
            //String hotKey = System.Configuration.ConfigurationManager.AppSettings["SubmitHotKey"];
            //if("ENTER".Equals(hotKey))
            //    isOk = Hotkey.RegisterHotKey(this.Handle, 222, Hotkey.KeyModifiers.None, Keys.Enter);
            //if("SPACE".Equals(hotKey))
            //    isOk = Hotkey.RegisterHotKey(this.Handle, 222, Hotkey.KeyModifiers.None, Keys.Space);

            //isOk = Hotkey.RegisterHotKey(this.Handle, 223, Hotkey.KeyModifiers.None, Keys.F9);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 224, Hotkey.KeyModifiers.None, Keys.F11);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 225, Hotkey.KeyModifiers.None, Keys.F4);
            //isOk = Hotkey.RegisterHotKey(this.Handle, 226, Hotkey.KeyModifiers.Ctrl, Keys.R);


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

            this.floatingForm.StartPosition = FormStartPosition.Manual;
            this.floatingForm.Location = this.TimePos;
            this.floatingForm.Show();
        }

        void monitorWifi() {

            BidStep2 step2 = SubmitPriceStep2Job.getPosition();

            Point origin = findOrigin();
            int x = origin.X, y = origin.Y;
            if (step2.wifi != null) {
                x += step2.wifi.x;
                y += step2.wifi.y;
            }
            ScreenUtil screenUtil = new ScreenUtil();
            int count = 0;
            while (true) {

                logger.DebugFormat("IE frame origin({0},{1})", origin.X, origin.Y);
                logger.DebugFormat("scan CONNECTION STATUS({0},{1}) ...", x, y);
                count++;

                if (origin.X == 0 && origin.Y == 0) {

                    this.toolStripStatusLabelStatus.BackColor = Color.Orange;
                    this.toolStripStatusLabelStatus.Text = "[未知]";
                }
                else {

                    byte[] wifiSpot = screenUtil.screenCaptureAsByte(x, y, 6, 5);
                    File.WriteAllBytes(@"wifi-spot.bmp", wifiSpot);
                    if (CaptchaHelper.isWifiRed(Bitmap.FromStream(new MemoryStream(wifiSpot)) as Bitmap)) {
                        
                        this.toolStripStatusLabelStatus.BackColor = Color.Red;
                        this.toolStripStatusLabelStatus.Text = "[离线]";
                        IEUtil.findBrowser().Refresh();
                        this.notifyIcon1.ShowBalloonTip(60000, "国拍", "检测到掉线，已自动F5刷新或关掉IE重新登录", ToolTipIcon.Warning);
                    }
                    else {
                        this.toolStripStatusLabelStatus.BackColor = Color.Lime;
                        this.toolStripStatusLabelStatus.Text = "[在线]";
                    }
                }

                if (count % 10 == 0) {//5mins
                    try {
                        origin = findOrigin();
                        if (step2.wifi != null) {
                            x = origin.X + step2.wifi.x;
                            y = origin.Y + step2.wifi.y;
                        }
                    }
                    catch (Exception ex) {
                        logger.Error(ex.Message);
                    }
                }
                System.Threading.Thread.Sleep(30000);
            }
        }

        void kh_OnKeyDownEvent(object sender, KeyEventArgs e) {

            System.Console.WriteLine(e.KeyCode);
            //System.Console.WriteLine("," + e.KeyData);
            switch (e.KeyData) {
                case Keys.Control | Keys.D3:
                case Keys.Control | Keys.D4:
                case Keys.Control | Keys.D5:
                case Keys.Control | Keys.D6:
                case Keys.Control | Keys.D7:
                case Keys.Control | Keys.D8:
                case Keys.Control | Keys.D9:
                    int number = ((int)e.KeyData) & 0xFF - (int)Keys.D0;
                    logger.InfoFormat("HOT KEY [CTRL + {0}] trigger", number);
                    this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), number*100);
                    break;
                case Keys.Control|Keys.Left:
                    logger.Info("HOT KEY [CTRL + LEFT] trigger");
                    this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.LEFT);
                    break;
                case Keys.Control | Keys.Right:
                    logger.Info("HOT KEY [CTRL + Right] trigger");
                    this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.RIGHT);
                    break;
                case Keys.Control | Keys.Up:
                case Keys.Control | Keys.Down:
                    logger.Info("HOT KEY [CTRL + UP|DOWN] trigger");
                    this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.MIDDLE);
                    break;
                case Keys.Control|Keys.Enter:
                    logger.Info("HOT KEY [CTRL + ENTER] trigger");
                    this.submit(this.EndPoint, SubmitPriceStep2Job.getPosition(), CaptchaInput.AUTO);
                    break;
                case Keys.Control|Keys.R:
                    logger.Info("HOT KEY [CTRL + R] trigger");
                    this.refreshCaptcha(SubmitPriceStep2Job.getPosition());
                    break;
                case Keys.F4:
                    logger.Info("HOT KEY [F4] trigger : STOP current JOB");
                    this.stopCurrentJob();
                    break;
                case Keys.F9:
                    //logger.InfoFormat("HOT KEY [F9] trigger");
                    //this.fire(SubmitPriceStep2Job.getPosition(), 300);
                    this.fireAutoSubmit(SubmitPriceStep2Job.getPosition());
                    break;
                case Keys.F11:
                    logger.InfoFormat("HOT KEY [F11] trigger : +1000");
                    this.fire(SubmitPriceStep2Job.getPosition(), 1200);
                    break;
                case Keys.Enter:
                    logger.InfoFormat("HOT KEY [ENTER] trigger : submit CAPTCHA");
                    if(this.m_submitHotKey.Equals("ENTER"))
                        this.processEnter(SubmitPriceStep2Job.getPosition());
                    break;
                case Keys.Space:
                    logger.InfoFormat("HOT KEY [SPACE] trigger : submit CAPTCHA");
                    if (this.m_submitHotKey.Equals("SPACE"))
                        this.processEnter(SubmitPriceStep2Job.getPosition());
                    break;
                case Keys.Escape:
                    logger.InfoFormat("HOT KEY [ESCAPE] trigger : close DIALOG");
                    this.processEsc(SubmitPriceStep2Job.getPosition());
                    break;
            }
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
                            //this.closeDialog(SubmitPriceStep2Job.getPosition());
                            this.processEsc(SubmitPriceStep2Job.getPosition());
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
                        case 224://F11
                            logger.Info("HOT KEY F11 (224) trigger, +1000");
                            this.exam4Fire(1000);
                            //this.fire(SubmitPriceStep2Job.getPosition(), 1000);
                            break;
                        case 225://F4
                            logger.Info("HOT KEY F4 (225) trigger");
                            this.stopCurrentJob();
                            break;
                        case 226:
                            logger.Info("HOT KEY CTRL+R (226) trigger");
                            this.refreshCaptcha(SubmitPriceStep2Job.getPosition());
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        private void receiveLogin(Client client, Trigger trigger) {

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

                this.toolStripStatusLabel3.Text = client.memo.Replace("\r\n", ";");
                if (null != trigger) {
                    int idx = trigger.deltaPrice / 100 - 1;
                    this.comboBoxCustomDelta.SelectedItem = this.comboBoxCustomDelta.Items[idx];

                    this.dateTimePickerCustomPrice.Text = trigger.priceTime;
                    this.checkBoxInputCaptcha.Checked = null != trigger.captchaTime;
                    if (null != trigger.captchaTime)
                        this.dateTimePickerCustomInputCaptcha.Text = trigger.captchaTime;

                    this.checkBoxSubmitCaptcha.Checked = null != trigger.submitTime;
                    if (null != trigger.submitTime)
                        this.dateTimePickerCustomSubmitCaptcha.Text = trigger.submitTime;

                    if (trigger.submitReachPrice > 0) {
                        this.checkBoxReachPrice.Checked = true;
                        this.comboBoxReachPrice.Enabled = true;
                        int index = trigger.submitReachPrice / 100 - 1;
                        this.comboBoxReachPrice.SelectedItem = this.comboBoxReachPrice.Items[index];

                    } else {
                        this.checkBoxReachPrice.Checked = false;
                        this.comboBoxReachPrice.Enabled = false;
                    }
                }
                //MessageBoxButtons messButton = MessageBoxButtons.OK;
                //DialogResult dr = MessageBox.Show(client.memo, "操作策略", messButton);
                this.notifyIcon1.ShowBalloonTip(5000, "操作提示", client.memo, ToolTipIcon.Info);
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

            DateTime now = DateTime.Now;
            String current = now.ToString("HH:mm:ss");
            this.toolStripStatusLabel2.Text = String.Format("NOW:{0}", current);

            if (current.Equals(this.triggerF11)) {//F11
                logger.InfoFormat("@{0} auto F11 triggered", current);
                this.fire(SubmitPriceStep2Job.getPosition(), 1200);
            }
            if (current.Equals(this.triggerSetPolicy)) {//设置策略
                logger.InfoFormat("@{0} auto SET Custom Policy triggered", current);
                this.updateCustomPolicy();
            }
            if (current.Equals(this.triggerLoadResource)) {//加载配置
                logger.InfoFormat("@{0} auto LOAD Configuration triggered", current);
                if (this.国拍ToolStripMenuItem.Checked)
                    this.loadResource("real");
                if (this.模拟ToolStripMenuItem.Checked)
                    this.loadResource("simulate");
            }
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

                    logger.WarnFormat("BEGIN givePRICE(delta : {0})", delta);
                    if(this.deltaPriceOnUI && bid.give.delta != null)
                    {
                        logger.Info("\tBEGIN make delta PRICE blank...");
                        ScreenUtil.SetCursorPos(x + bid.give.delta.inputBox.x, y + bid.give.delta.inputBox.y);
                        ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                        System.Threading.Thread.Sleep(50);
                        logger.Info("\tEND   make delta PRICE blank.");

                        logger.Info("\tBEGIN input delta PRICE...");
                        KeyBoardUtil.sendMessage(Convert.ToString(delta), interval:interval, needClean:true);
                        logger.Info("\tEND   input delta PRICE.");
                        System.Threading.Thread.Sleep(50);

                        logger.Info("\tBEGIN click delta PRICE button");
                        ScreenUtil.SetCursorPos(x + bid.give.delta.button.x, y + bid.give.delta.button.y);
                        ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                        logger.Info("\tEND   click delta PRICE button");
                        
                    }
                    else
                    {
                        //INPUT BOX
                        logger.Info("\tBEGIN click INPUTBOX");
                        ScreenUtil.SetCursorPos(x + bid.give.inputBox.x, y + bid.give.inputBox.y);
                        ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                        System.Threading.Thread.Sleep(50);
                        logger.Info("\tEND   click INPUTBOX.");

                        //SendKeys.SendWait("{BACKSPACE 5}{DEL 5}");
                        //System.Threading.Thread.Sleep(50);

                        logger.Info("\tBEGIN identify PRICE...");
                        byte[] content = new ScreenUtil().screenCaptureAsByte(x + bid.give.price.x, y + bid.give.price.y, 52, 18);
                        String txtPrice = this.m_orcPrice.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
                        int price = Int32.Parse(txtPrice);
                        price += delta;
                        txtPrice = String.Format("{0:D}", price);
                        logger.InfoFormat("\tEND   identified PRICE = {0}", txtPrice);

                        logger.InfoFormat("\tBEGIN input PRICE : {0}", txtPrice);
                        KeyBoardUtil.sendMessage(txtPrice, interval:interval, needClean:true);
                        logger.Info("\tEND   input PRICE");
                    }

                    System.Threading.Thread.Sleep(150);
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

        private void submitCaptcha(tobid.rest.position.BidStep2 bid) {

            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;

            logger.Info("提交验证码");
            ScreenUtil.SetCursorPos(x+bid.submit.buttons[0].x, y+bid.submit.buttons[0].y);//确定按钮
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
        }

        /*private void closeDialog(tobid.rest.position.BidStep2 bid) {

            //System.Threading.Thread closeDialog = new System.Threading.Thread(delegate(){
                Point origin = findOrigin();
                int x = origin.X;
                int y = origin.Y;

                logger.Info("关闭校验码窗口");
                logger.DebugFormat("close BUTTON POS({0}, {1})", x + bid.submit.buttons[1].x, y + bid.submit.buttons[1].y);
                ScreenUtil.SetCursorPos(x + bid.submit.buttons[1].x, y + bid.submit.buttons[1].y);//取消按钮
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            //});
        }*/

        private void refreshCaptcha(tobid.rest.position.BidStep2 bid) {

            System.Threading.Thread refreshCaptcha = new System.Threading.Thread(delegate() {
                Point origin = findOrigin();
                int x = origin.X;
                int y = origin.Y;

                ScreenUtil.SetCursorPos(x + bid.submit.captcha[0].x + 55, y + bid.submit.captcha[0].y + 12);//校验码区域
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                
                System.Threading.Thread.Sleep(250);

                ScreenUtil.SetCursorPos(x + bid.submit.inputBox.x, y + bid.submit.inputBox.y);//校验码输入框
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            });
            refreshCaptcha.Start();
        }

        private void processEsc(tobid.rest.position.BidStep2 bid) {

            System.Threading.Thread processEsc = new System.Threading.Thread(delegate() {
                Point origin = findOrigin();
                int x = origin.X;
                int y = origin.Y;

                byte[] title = new ScreenUtil().screenCaptureAsByte(x + bid.title.x, y + bid.title.y, 170, 50);
                Bitmap bitTitle = new Bitmap(new MemoryStream(title));
                String strTitle = this.m_orcTitle.IdentifyStringFromPic(bitTitle);
                logger.Debug(strTitle);

                if ("系统提示".Equals(strTitle)) {

                    logger.Info("proces ESC in [系统提示]");
                    ScreenUtil.SetCursorPos(x + bid.okButton.x, y + bid.okButton.y);
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                }
                else if ("投标拍卖".Equals(strTitle)) {

                    logger.Info("proces ESC in [投标拍卖]");
                    ScreenUtil.SetCursorPos(x + bid.submit.buttons[1].x, y + bid.submit.buttons[1].y);//取消按钮
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                }
            });
            processEsc.Start();
        }

        private void processEnter(tobid.rest.position.BidStep2 bid) {

            System.Threading.Thread processEnter = new System.Threading.Thread(delegate() {
                /*Point origin = findOrigin();
                int x = origin.X;
                int y = origin.Y;

                byte[] title = new ScreenUtil().screenCaptureAsByte(x + bid.title.x, y + bid.title.y, 170, 50);
                Bitmap bitTitle = new Bitmap(new MemoryStream(title));
                String strTitle = this.m_orcTitle.IdentifyStringFromPic(bitTitle);
                logger.Debug(strTitle);
                if ("系统提示".Equals(strTitle)) {

                    logger.Info("proces Enter in [系统提示]");
                    ScreenUtil.SetCursorPos(x + bid.okButton.x, y + bid.okButton.y);
                    ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                }
                else if ("投标拍卖".Equals(strTitle)) {

                    logger.Info("proces Enter in [投标拍卖]");
                    this.submitCaptcha(SubmitPriceStep2Job.getPosition());
                }*/
                SubmitCaptchaAction submitCaptcha = new SubmitCaptchaAction(this);
                submitCaptcha.execute();
            });
            processEnter.Start();

        }

        private void stopCurrentJob() {

            if (null != this.submitPriceStep2Thread) {
                this.submitPriceStep2Thread.Abort();
                this.submitPriceStep2Thread = null;
            }

            if (null != this.submitPriceV2Thread) {
                this.submitPriceV2Thread.Abort();
                this.submitPriceV2Thread = null;
            }

            if (null != this.customThread) {
                this.customThread.Abort();
                this.customThread = null;
            }
        }

        private void fire(tobid.rest.position.BidStep2 bid, int delta) {

            System.Threading.Thread startFire = new System.Threading.Thread(delegate() {
                Point origin = findOrigin();
                int x = origin.X;
                int y = origin.Y;

                ScreenUtil.SetCursorPos(x + bid.okButton.x, y + bid.okButton.y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                System.Threading.Thread.Sleep(100);
                
                IBidAction actionInputPrice = new InputPriceAction(delta: delta, repo: this);
                IBidAction actionPreCaptcha = new PreCaptchaAction(repo: this);
                IBidAction actionInputCaptcha = new InputCaptchaAction(repo: this);
                //IAction actions = new SequenceAction(new List<IBidAction>() { actionInputPrice, actionPreCaptcha, actionInputCaptcha });
                IAction actions = new SequenceAction(new List<IBidAction>() { actionInputPrice, actionPreCaptcha });
                actions.execute();
            });
            startFire.Start();
        }

        private void fireAutoSubmit(tobid.rest.position.BidStep2 bid)
        {
            System.Threading.Thread startFire = new System.Threading.Thread(delegate()
            {
                Point origin = findOrigin();
                int x = origin.X;
                int y = origin.Y;

                ScreenUtil.SetCursorPos(x + bid.okButton.x, y + bid.okButton.y);
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                System.Threading.Thread.Sleep(50);

                DateTime now = DateTime.Now;
                int delta = 300;
                if(this.m_F9Strategy.ContainsKey(now.Second))
                    delta = this.m_F9Strategy[now.Second];
                logger.DebugFormat("trigger Delta Price : {0}", delta);
                IBidAction actionInputPrice = new InputPriceAction(delta: delta, repo: this);
                IBidAction actionPreCaptcha = new PreCaptchaAction(repo: this);
                IAction actions = new SequenceAction(new List<IBidAction>() { actionInputPrice, actionPreCaptcha });
                IBidAction actionSubmitCaptcha = new SubmitCaptchaPureAction(repo:this);
                actions.execute();
                

                //x<=1.5 提交时间超过(5500)ms
                //x>1.5 提交时间为(4000+x)ms
                int maxWait = 5500;
                if (this.lastCost != null && this.lastCost.TotalMilliseconds > 1500)
                        maxWait += (int)this.lastCost.TotalMilliseconds - 1500;
                    //maxWait = 4000 + (this.lastCost.TotalMilliseconds > this.lastCost.TotalMilliseconds - 1500 ? (int)this.lastCost.TotalMilliseconds - 1500 : 1500);
                logger.DebugFormat("lastCost : {0}, maxWait : {1}", this.lastCost == null ? 0 : this.lastCost.TotalMilliseconds, maxWait);
                while (true)
                {
                    System.Threading.Thread.Sleep(100);
                    TimeSpan diff = DateTime.Now - this.lastSubmit;
                    logger.DebugFormat("lastSubmit : {0}, left : {1}", lastSubmit.ToString("HH:mm:ss.ffff"), diff.TotalMilliseconds);
                    if (diff.TotalMilliseconds > maxWait)
                    {
                        actionSubmitCaptcha.execute();
                        break;
                    }
                }
            });
            startFire.Start();
        }

        private void exam4Fire(int delta) {

            System.Threading.Thread startExam = new System.Threading.Thread(delegate() {
                String fire1 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                String fire2 = DateTime.Now.AddSeconds(2).ToString("yyyy-MM-dd HH:mm:ss");

                List<ITask> tasks = new List<ITask>();
                TaskTimeBased taskInputPrice = new tobid.scheduler.jobs.action.TaskTimeBased(
                    action: new tobid.scheduler.jobs.action.InputPriceAction(delta: delta, repo: this),
                    notify: this,
                    fireTime: fire1);
                tasks.Add(taskInputPrice);

                ProcessCaptchaAction actionInputCaptcha = new tobid.scheduler.jobs.action.ProcessCaptchaAction(repo: this);
                TaskTimeBased taskInputCaptcha = new TaskTimeBased(action: actionInputCaptcha, notify: this, fireTime: fire2);
                tasks.Add(taskInputCaptcha);

                CustomJob job = new CustomJob(tasks: tasks);
                for (int i = 0; i < 6; i++) {
                    job.Execute();
                    System.Threading.Thread.Sleep(500);
                }
            });
            startExam.Start();
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

                    //SendKeys.SendWait("{BACKSPACE 4}{DEL 4}");
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

                            KeyBoardUtil.sendMessage(txtCaptcha.Substring(0, 4), this.interval, true);
                        }
                        if (CaptchaInput.MIDDLE == input) {

                            KeyBoardUtil.sendMessage(txtCaptcha.Substring(1, 4), this.interval, true);
                        }
                        if (CaptchaInput.RIGHT == input) {

                            KeyBoardUtil.sendMessage(txtCaptcha.Substring(2, 4), this.interval, true);
                        }
                        if (CaptchaInput.AUTO == input) {

                            KeyBoardUtil.sendMessage(txtActive, this.interval, true);
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

        void updateCustomPolicy() {

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

            List<ITask> tasks = new List<ITask>();
            InputPriceAction inputPriceAction = new tobid.scheduler.jobs.action.InputPriceAction(delta: Int32.Parse(this.comboBoxCustomDelta.Text), repo: this);
            TaskTimeBased taskInputPrice = new tobid.scheduler.jobs.action.TaskTimeBased(action: inputPriceAction, notify: this, fireTime: fire1);
            tasks.Add(taskInputPrice);

            if (checkBoxInputCaptcha.Checked) {

                ProcessCaptchaAction actionInputCaptcha = new tobid.scheduler.jobs.action.ProcessCaptchaAction(repo: this);
                SubmitCaptchaAction actionSubmitCaptcha = new tobid.scheduler.jobs.action.SubmitCaptchaAction(repo: this);
                TaskTimeBased taskInputCaptcha = new TaskTimeBased(action: actionInputCaptcha, notify: this, fireTime: fire2); tasks.Add(taskInputCaptcha);
                if (checkBoxSubmitCaptcha.Checked) {
                    TaskTimeBased taskSubmitCaptchaTimeBased = new TaskTimeBased(action: actionSubmitCaptcha, notify: this, fireTime: fire3); 
                    if (checkBoxReachPrice.Checked) {

                        TaskPriceBased taskSubmitCaptchaPriceBased = new TaskPriceBased(
                            action: actionSubmitCaptcha,
                            inputPriceAction: inputPriceAction, 
                            submitReachPrice: Int32.Parse(this.comboBoxReachPrice.Text), 
                            repository: this);
                        ComboTask comboTask = new ComboTask(new List<ITask>() {
                            taskSubmitCaptchaTimeBased, taskSubmitCaptchaPriceBased
                        });
                        tasks.Add(comboTask);
                    } else {
                        tasks.Add(taskSubmitCaptchaTimeBased);
                    }
                } else {

                    if (checkBoxReachPrice.Checked) {
                        TaskPriceBased taskSubmitCaptchaPriceBased = new TaskPriceBased(action: actionSubmitCaptcha, inputPriceAction: inputPriceAction, submitReachPrice: Int32.Parse(this.comboBoxReachPrice.Text), repository: this);
                        tasks.Add(taskSubmitCaptchaPriceBased);
                    }
                }

            } else {

                SubmitCaptchaAction actionSubmitCaptcha = new tobid.scheduler.jobs.action.SubmitCaptchaAction(repo: this);
                TaskTimeBased taskSubmitCaptchaTimeBased = new TaskTimeBased(action: actionSubmitCaptcha, notify: this, fireTime: fire3);
                if (checkBoxSubmitCaptcha.Checked) {//定时提交

                    if (checkBoxReachPrice.Checked) {//定时提交&按三边价格提交
                        TaskPriceBased taskSubmitCaptchaPriceBased = new TaskPriceBased(action: actionSubmitCaptcha, inputPriceAction: inputPriceAction, submitReachPrice: Int32.Parse(this.comboBoxReachPrice.Text), repository: this);
                        ComboTask comboTask = new ComboTask(new List<ITask>() {
                            taskSubmitCaptchaTimeBased, taskSubmitCaptchaPriceBased
                        });
                        tasks.Add(comboTask);
                    } else {//仅定时提交
                        tasks.Add(taskSubmitCaptchaTimeBased);
                    }
                    
                } else {//不用定时提交

                    if (checkBoxReachPrice.Checked)
                    {//不定时提交，但按三边价格提交
                        TaskPriceBased taskSubmitCaptchaPriceBased = new TaskPriceBased(action: actionSubmitCaptcha, inputPriceAction: inputPriceAction, submitReachPrice: Int32.Parse(this.comboBoxReachPrice.Text), repository: this);
                        tasks.Add(taskSubmitCaptchaPriceBased);
                    }
                }
            }

            if (null != this.customThread)
                this.customThread.Abort();

            SchedulerConfiguration customConf = new SchedulerConfiguration(500);
            customConf.Job = new CustomJob(tasks: tasks);
            this.m_schedulerCustom = new Scheduler(customConf);

            System.Threading.ThreadStart customThreadStart = new System.Threading.ThreadStart(this.m_schedulerCustom.Start);
            this.customThread = new System.Threading.Thread(customThreadStart);
            this.customThread.Name = "customThread";
            this.customThread.Start();
        }

        private void buttonUpdateCustom_Click(object sender, EventArgs e) {

            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show(this, "确定要更新出价策略吗?", "更新策略",
                messButton, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (dr == DialogResult.OK){

                this.updateCustomPolicy();
            }
        }

        private void btnUpdateV2_Click(object sender, EventArgs e)
        {
            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show(this, "确定要更新出价策略吗?", "更新策略",
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
            DialogResult dr = MessageBox.Show(this, "确定要更新出价策略吗?", "更新策略",
                messButton, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (dr == DialogResult.OK) {

                Step2Operation bidOps = SubmitPriceStep2Job.getConfig();
                bidOps.updateTime = DateTime.Now;
                bidOps.startTime = this.dateTimePicker1.Value;
                bidOps.expireTime = bidOps.startTime.AddHours(1);

                String fireTime = this.dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss");
                this.textBox1.Text = this.dateTimePicker1.Value.ToString("MM/dd HH:mm:ss");
                this.textBox2.Text = this.comboBox1.Text;

                /*
                if (radioDeltaPrice.Checked) {
                    object obj = this.comboBox1.SelectedItem;
                    if (obj == null) {
                        MessageBox.Show("请选择价格");
                        this.comboBox1.Focus();
                        return;
                    }
                    this.textBox2.Text = this.comboBox1.Text;
                    bidOps.price = Int32.Parse(this.comboBox1.Text);
                    SubmitPriceStep2Job.setConfig(bidOps, this.checkPriceOnly.Checked, updatePos: false);
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
                */

                {
                    int delta = Int32.Parse(this.comboBox1.Text);

                    IBidAction actionInputPrice = new InputPriceAction(delta: delta, repo: this);
                    IBidAction actionPreCaptcha = new PreCaptchaAction(repo: this);
                    IBidAction actionInputCaptcha = new InputCaptchaAction(repo: this);
                    IBidAction actions = null;
                    if(!this.checkPriceOnly.Checked)
                        actions = new SequenceAction(new List<IBidAction>() { actionInputPrice, actionPreCaptcha, actionInputCaptcha });
                    else
                        actions = new SequenceAction(new List<IBidAction>() { actionInputPrice, actionPreCaptcha});

                    TaskTimeBased scheduledTask = new TaskTimeBased(action: actions, notify: this, fireTime: fireTime);

                    if (null != this.submitPriceStep2Thread)
                        this.submitPriceStep2Thread.Abort();

                    SchedulerConfiguration customConf = new SchedulerConfiguration(500);
                    customConf.Job = new CustomJob(tasks: new List<ITask>() { scheduledTask });
                    Scheduler schedulerCustom = new Scheduler(customConf);

                    System.Threading.ThreadStart customThreadStart = new System.Threading.ThreadStart(schedulerCustom.Start);
                    this.submitPriceStep2Thread = new System.Threading.Thread(customThreadStart);
                    this.submitPriceStep2Thread.Name = "submitV1";
                    this.submitPriceStep2Thread.Start();                
                }
                
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

        private void CheckUpdateToolStripMenuItem_Click(object sender, EventArgs e) {

            MessageBoxButtons messButton = MessageBoxButtons.OK;
            String localVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            String remoteVer = null;
            try {

                String endPoint = ConfigurationManager.AppSettings["CHECKUPDATE"];
                remoteVer = new HttpUtil().getAsPlain(endPoint + "/Release.ver");
            } catch (Exception ex) {
                System.Console.WriteLine(ex);
                MessageBox.Show(String.Format("软件为最新版. {0}", localVer), "UP TO DATE", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                return;
            }

            if (!localVer.Equals(remoteVer))
                MessageBox.Show(String.Format("请更新软件\r\nREMOTE:{0}\r\nLOCAL:{1}", remoteVer, localVer), "发现新版本", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            else
                MessageBox.Show(String.Format("软件为最新版. {0}", localVer), "UP TO DATE", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }

        private void toolStripMenuReload_Click(object sender, EventArgs e) {

            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("放弃当前坐标配置，重新加载服务器配置？", "?", messButton, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (dr == System.Windows.Forms.DialogResult.Cancel)
                return;

            if (this.国拍ToolStripMenuItem.Checked) {

                try {
                    this.loadResource("real");
                    this.enableForm();
                } catch (Exception ex) {
                    logger.Error(ex);
                    this.disableForm();
                }
            }
            if (this.模拟ToolStripMenuItem.Checked) {

                try {
                    this.loadResource("simulate");
                    this.enableForm();
                } catch (Exception ex) {
                    logger.Error(ex);
                    this.disableForm();
                }
            }
        }

        private void 国拍ToolStripMenuItem_Click(object sender, EventArgs e){

            this.国拍ToolStripMenuItem.Checked = true;
            this.模拟ToolStripMenuItem.Checked = false;
            try{

                this.loadResource("real");
                this.enableForm();
            } catch (Exception ex) {
                logger.Error(ex);
                this.disableForm();
            }
        }

        private void ToolStripMenuItem_Simulate51_Click(object sender, EventArgs e){

            this.国拍ToolStripMenuItem.Checked = false;
            this.模拟ToolStripMenuItem.Checked = true;
            try {

                this.loadResource("simulate");
                this.enableForm();
            } catch (Exception ex) {
                logger.Error(ex);
                this.disableForm();
            }
        }

        private void stepToolStripMenuItem_Click(object sender, EventArgs e){
            this.step2Dialog.ShowDialog(this);
        }

        private void AuthCodeToolStripMenuItem_Click(object sender, EventArgs e) {

            String credential = ConfigurationManager.AppSettings["credential"];
            String authCode = Microsoft.VisualBasic.Interaction.InputBox("请输入授权码", "", credential);
            if (!String.IsNullOrEmpty(authCode)) {

                Warrant warrant = new Warrant(authCode);
                String hostName = System.Net.Dns.GetHostName();
                String epKeepAlive = this.EndPoint + "/rest/service/command/register/" + hostName;
                RestClient rest = new RestClient(epKeepAlive, HttpVerb.POST, warrant);
                try {
                    String rtn = rest.MakeRequest(null, false);

                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["principal"].Value = hostName;
                    config.AppSettings.Settings["credential"].Value = authCode;
                    config.Save();
                    ConfigurationManager.RefreshSection("appSettings");

                    MessageBoxButtons messButton = MessageBoxButtons.OK;
                    DialogResult dr = MessageBox.Show(String.Format("请重新启动应用程序!\r\n主机名:{0}\r\n授权码:{1}", hostName, authCode), "授权成功", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                } catch (Exception ex) {
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

            if (this.wifiMonitorThread != null)
                this.wifiMonitorThread.Abort();
            this.wifiMonitorThread = new System.Threading.Thread(new ThreadStart(this.monitorWifi));
            this.wifiMonitorThread.Start();
        }

        private void buttonLogin_Click_1(object sender, EventArgs e){

            //LoginJob job = new LoginJob(m_orcLogin);
            //job.Execute();

            //this.giveDeltaPrice(SubmitPriceStep2Job.getPosition(), 300);

            //for (int i = 1; i < 22; i++) {
            //    Image img = Image.FromFile(@"e:\captcha" + i + ".png");
            //    String val = this.m_orcLogin.IdentifyStringFromPic((Bitmap)img);
            //    System.Console.WriteLine(val);
            //}

            //214,203
            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y;
            byte[] content = new ScreenUtil().screenCaptureAsByte(x+214, y+203, 6, 5);
            File.WriteAllBytes(@"e:\point.bmp", content);
            //H:143-184
        }
        #endregion

        #region tabControl事件
        private TabPage lastSelTab;
        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e) {

            String selecting = this.tabControl1.SelectedTab.Text;
            System.Console.WriteLine("selecting " + selecting);

            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show(this, String.Format("确定使用{0}吗?", selecting), "策略选择",
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

        #region 时间
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

        private void checkBoxReachPrice_CheckedChanged(object sender, EventArgs e) {
            if (this.checkBoxReachPrice.Checked)
                this.comboBoxReachPrice.Enabled = true;
            else
                this.comboBoxReachPrice.Enabled = false;
        }
        #endregion

        #region 北京时间
        private void buttonSync_Click(object sender, EventArgs e) {
            
            //logger.Debug("internet time sync");
            //SystemTimeUtil.SetInternetTime();

            logger.Debug("bid time sync");
            BidStep2 bidStep = SubmitPriceStep2Job.getPosition();
            Point origin = tobid.util.IEUtil.findOrigin();
            Position pos = bidStep.time;

            byte[] content = new ScreenUtil().screenCaptureAsByte(origin.X + pos.x, origin.Y + pos.y, 140, 24);
            String strTime = this.orcTime.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
            
            char[] array = strTime.ToArray<char>();
            String timestamp = String.Format("{0}{1}:{2}{3}:{4}{5}", array[0], array[1], array[2], array[3], array[4], array[5]);
            SystemTimeUtil.SetCustomTime(Int16.Parse(strTime.Substring(0,2)), Int16.Parse(strTime.Substring(2,2)), Int16.Parse(strTime.Substring(4,2)));
        }

        private void buttonAdd_Click(object sender, EventArgs e) {
            logger.Debug("+1 second");
            SystemTimeUtil.addMilliSecond(500);
        }

        private void buttonMinus_Click(object sender, EventArgs e) {
            logger.Debug("-1 second");
            SystemTimeUtil.addMilliSecond(-500);
        }
        #endregion

        
    }
}
