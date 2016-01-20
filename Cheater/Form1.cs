using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

using System.Runtime.InteropServices;

using tobid.rest;
using tobid.scheduler.jobs;
using System.IO;
using tobid.util.orc;
using tobid.util;
using tobid.rest.position;
using Microsoft.Win32;

namespace Cheater {
    public partial class Form1 : Form, IRepository {

        [DllImport("user32.dll")]
        private static extern int GetDC(int hwnd);

        private String[] bitmaps;
        private Random ran = new Random();
        tobid.util.orc.OrcUtil orc;
        private String EndPoint { get; set; }
        public Form1(String endPoint) {

            InitializeComponent();
            this.EndPoint = endPoint;
            //orc = tobid.util.orc.OrcUtil.getInstance(new int[] { 0, 8, 16, 24, 32 }, 0, 0, 7, 10, @"price");
        }

        private IOrc m_orcLogin;
        private IOrc m_orcTitle;
        private IOrc m_orcCaptcha;
        private IOrc m_orcPrice;
        private IOrc m_orcPriceSM;
        private IOrc m_orcCaptchaLoading;
        private IOrc[] m_orcCaptchaTip;
        private Entry[] m_entries;
        private CaptchaUtil m_orcCaptchaTipsUtil;


        #region IRepository
        public Boolean deltaPriceOnUI { get; set; }
        public Point TimePos { get; set; }
        public String endPoint { get { return this.EndPoint; } }
        public IOrc orcTitle { get { return this.m_orcTitle; } }
        public IOrc orcCaptcha { get { return this.m_orcCaptcha; } }
        public IOrc orcPrice { get { return this.m_orcPrice; } }
        public IOrc orcPriceSM { get { return this.m_orcPriceSM; } }
        public IOrc orcTime{get{return null;}}
        public IOrc orcCaptchaLoading { get { return this.m_orcCaptchaLoading; } }
        public IOrc[] orcCaptchaTip { get { return this.m_orcCaptchaTip; } }
        public Entry[] entries { get { return this.m_entries; } }
        public CaptchaUtil orcCaptchaTipsUtil { get { return this.m_orcCaptchaTipsUtil; } }
        public int interval { get { return 0; } }
        public String category { get { return "simulate"; } }

        public BidStep2 bidStep2 { get { return SubmitPriceStep2Job.getPosition(); } }
        public GivePriceStep2 givePriceStep2 { get { return SubmitPriceStep2Job.getPosition().give; } }
        public SubmitPrice submitPrice { get { return SubmitPriceStep2Job.getPosition().submit; } }

        public DateTime lastSubmit { get; set; }
        public TimeSpan lastCost { get; set; }
        public Boolean isReady { get; set; }
        #endregion

        private void loadResource(String category) {
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

        private void Form1_Load(object sender, EventArgs e) {

            this.bitmaps = new String[]{
                "066289.bmp", "102224.bmp", "152610.bmp", "166573.bmp", "202979.bmp",
                "257757.bmp", "522857.bmp", "654714.bmp", "709177.bmp", "882277.bmp",
                "964932.bmp", "992986.bmp"
            };

            //KeepAliveJob keepAliveJob = new KeepAliveJob(this.EndPoint, 
            //    new ReceiveLogin(this.receiveLogin),
            //    new ReceiveOperation[]{
            //        new ReceiveOperation(this.receiveOperation),
            //        new ReceiveOperation(this.receiveOperation)},
            //    null);
            //keepAliveJob.Execute();
            this.loadResource("simulate");
            
            this.chart1.Series.Clear();
            Series series = new Series();
            series.ChartType = SeriesChartType.Spline;
            this.chart1.Series.Add(series);
            this.chart1.ChartAreas[0].AxisY.Maximum = 85000;
            this.chart1.ChartAreas[0].AxisY.Minimum = 79900;
        }

        private void receiveLogin(Client client, ITrigger trigger) {
        }
        private void receiveOperation(Operation operation) {
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {

            if (MessageBox.Show("R U about to exit application?", "Confirm", MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.OK) {

                notifyIcon1.Visible = false;
                this.Close();
                this.Dispose();
                Application.Exit();
            }
        }

        private void sToolStripMenuItem3s_Click(object sender, EventArgs e) {

            this.showLoading(3);
            this.showCaptcha(5);
        }

        private void sToolStripMenuItem5s_Click(object sender, EventArgs e) {

            this.showLoading(5);
            this.showCaptcha(3);
        }

        private void sToolStripMenuItem7s_Click(object sender, EventArgs e) {

            this.showLoading(7);
            this.showCaptcha(1);
        }

        private void showCaptcha(int second) {

            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y + 4;
            int RandKey = ran.Next(0, 11);
            byte[] binary = System.IO.File.ReadAllBytes(this.bitmaps[RandKey]);
            Bitmap bitmap = new Bitmap(new System.IO.MemoryStream(binary));
            System.IntPtr p = (IntPtr)GetDC(0);// '取得屏幕
            Graphics g = Graphics.FromHdc(p);

            for (int i = 0; i < 20 * second; i++) {
                //g.DrawImage(bitmap, new Point(966, 482));
                g.DrawImage(bitmap, new Point(SubmitPriceStep2Job.getPosition().submit.captcha[0].x + x, SubmitPriceStep2Job.getPosition().submit.captcha[0].y + y));
                System.Threading.Thread.Sleep(50);
            }
        }

        private void showLoading(int second) {

            Point origin = findOrigin();
            int x = origin.X;
            int y = origin.Y + 4;
            byte[] binary = System.IO.File.ReadAllBytes("loading.bmp");
            Bitmap bitmap = new Bitmap(new System.IO.MemoryStream(binary));
            System.IntPtr p = (IntPtr)GetDC(0);// '取得屏幕
            Graphics g = Graphics.FromHdc(p);

            for (int i = 0; i < 20 * second; i++) {
                //g.DrawImage(bitmap, new Point(962, 486));
                g.DrawImage(bitmap, new Point(SubmitPriceStep2Job.getPosition().submit.captcha[0].x + x, SubmitPriceStep2Job.getPosition().submit.captcha[0].y + y));
                System.Threading.Thread.Sleep(50);
            }
        }

        private void timer1_Tick(object sender, EventArgs e) {

            byte[] content = new tobid.util.ScreenUtil().screenCaptureAsByte(380, 507, 43, 14);
            Bitmap bitmap = new Bitmap(new System.IO.MemoryStream(content));
            
            String price = orc.IdentifyStringFromPic(bitmap);

            Series series = this.chart1.Series[0];
            DataPoint dp = new DataPoint();
            dp.SetValueXY("01", Int32.Parse(price));
            series.Points.Add(dp);
        }

        private void button1_Click(object sender, EventArgs e)
        {

            String ieVer = Convert.ToString(new WebBrowser().Version.Major);

            System.Console.WriteLine(System.Environment.OSVersion.Version);
            String ver = Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.MajorRevision;

            verRepo.Add("10.0", "win10");
            verRepo.Add("6.3", "win8.1");
            verRepo.Add("6.2", "win8");
            verRepo.Add("6.1", "win7");
            verRepo.Add("6.0", "winVista");
            verRepo.Add("5.2", "win2003");
            verRepo.Add("5.1", "winXP");
            verRepo.Add("5.0", "win2000");

            String readableVer = verRepo[ver];
            System.Console.WriteLine("ie" + ieVer + "," + readableVer);
        }

        IDictionary<String, String> verRepo = new Dictionary<String, String>();
    }
}
