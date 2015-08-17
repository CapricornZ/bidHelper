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
        LEFT, MIDDLE, RIGHT
    }

    public partial class Form1 : Form
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

        private IOrc m_orcCaptcha;
        private IOrc m_orcPrice;
        private IOrc m_orcCaptchaLoading;
        private IOrc[] m_orcCaptchaTip;
        private CaptchaUtil m_orcCaptchaTipsUtil;

        private Scheduler m_schedulerKeepAlive;
        private Scheduler m_schedulerSubmit;
        private Scheduler m_schedulerLogin;

        private System.Threading.Thread keepAliveThread;
        private System.Threading.Thread submitPriceThread;
        private System.Threading.Thread loginThread;

        private void Form1_Activated(object sender, EventArgs e) {

            this.textInputPrice.Focus();
            logger.Info("Application Form Activated");
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {

            if (null != this.keepAliveThread && (this.keepAliveThread.ThreadState == System.Threading.ThreadState.Running || this.keepAliveThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin))
                this.keepAliveThread.Abort();

            if (null != this.submitPriceThread && (this.submitPriceThread.ThreadState == System.Threading.ThreadState.Running || this.submitPriceThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin))
                this.submitPriceThread.Abort();

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
            logger.Info("Application Form Closed");
        }

        private void Form1_Load(object sender, EventArgs e){

            logger.Info("Application Form Load");

            Form.CheckForIllegalCrossThreadCalls = false;
            this.dateTimePicker1.Value = DateTime.Now;

            //加载配置项1
            IGlobalConfig configResource = Resource.getInstance(this.EndPoint);//加载配置

            this.Text = configResource.tag;
            this.m_orcCaptcha = configResource.Captcha;//
            this.m_orcPrice = configResource.Price;//价格识别
            this.m_orcCaptchaLoading = configResource.Loading;//LOADING识别
            this.m_orcCaptchaTip = configResource.Tips;//验证码提示（文字）
            this.m_orcCaptchaTipsUtil = new CaptchaUtil(m_orcCaptchaTip);

            //加载配置项2
            KeepAliveJob keepAliveJob = new KeepAliveJob(this.EndPoint, new ReceiveOperation(this.receiveOperation));
            keepAliveJob.Execute();

            //keepAlive任务配置
            SchedulerConfiguration config1M = new SchedulerConfiguration(1000 * 60 * 1);
            config1M.Job = new KeepAliveJob(this.EndPoint, new ReceiveOperation(this.receiveOperation));
            this.m_schedulerKeepAlive = new Scheduler(config1M);

            //Action任务配置
            SchedulerConfiguration config1S = new SchedulerConfiguration(1000);
            config1S.Job = new SubmitPriceJob(this.EndPoint, this.m_orcPrice, this.m_orcCaptchaLoading, this.m_orcCaptchaTipsUtil, this.m_orcCaptcha);
            m_schedulerSubmit = new Scheduler(config1S);

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
                            this.giveDeltaPrice(SubmitPriceJob.getPosition(), 300);
                            ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 104://CTRL+4
                            logger.Info("HOT KEY CTRL + 4 (104)");
                            this.giveDeltaPrice(SubmitPriceJob.getPosition(), 400);
                            ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 105://CTRL+5
                            logger.Info("HOT KEY CTRL + 5 (105)");
                            this.giveDeltaPrice(SubmitPriceJob.getPosition(), 500);
                            ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 106://CTRL+6
                            logger.Info("HOT KEY CTRL + 6 (106)");
                            this.giveDeltaPrice(SubmitPriceJob.getPosition(), 600);
                            ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 107://CTRL+7
                            logger.Info("HOT KEY CTRL + 7 (107)");
                            this.giveDeltaPrice(SubmitPriceJob.getPosition(), 700);
                            ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 108://CTRL+8
                            logger.Info("HOT KEY CTRL + 8 (108)");
                            this.giveDeltaPrice(SubmitPriceJob.getPosition(), 800);
                            ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 109://CTRL+9
                            logger.Info("HOT KEY CTRL + 9 (109)");
                            this.giveDeltaPrice(SubmitPriceJob.getPosition(), 900);
                            ScreenUtil.SetForegroundWindow(this.Handle);
                            break;
                        case 201://LEFT
                            logger.Info("HOT KEY CTRL + LEFT (201)");
                            this.submit(this.EndPoint, SubmitPriceJob.getPosition(), CaptchaInput.LEFT);
                            break;
                        case 202://UP
                            logger.Info("HOT KEY CTRL + UP (202)");
                            this.submit(this.EndPoint, SubmitPriceJob.getPosition(), CaptchaInput.MIDDLE);
                            break;
                        case 203://RIGHT
                            logger.Info("HOT KEY CTRL + RIGIHT (203)");
                            this.submit(this.EndPoint, SubmitPriceJob.getPosition(), CaptchaInput.RIGHT);
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        private void receiveOperation(Operation operation) {
            BidOperation bid = operation as BidOperation;
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
        private void givePrice(tobid.rest.position.Bid bid, String txtPrice) {

            logger.InfoFormat("BEGIN givePRICE({0})", txtPrice);

            //INPUT BOX
            logger.WarnFormat("\tBEGIN input PRICE : {0}", txtPrice);
            ScreenUtil.SetCursorPos(bid.give.inputBox.x, bid.give.inputBox.y);
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

            for (int i = 0; i < txtPrice.Length; i++) {
                System.Threading.Thread.Sleep(50);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
            }
            logger.Info("\tEND   input PRICE");

            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            System.Threading.Thread.Sleep(50);
            ScreenUtil.SetCursorPos(bid.give.button.x, bid.give.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");

            //启动线程截校验码
            System.Threading.Thread loading = new System.Threading.Thread(delegate() {
                ScreenUtil screen = new ScreenUtil();
                for (int i = 0; i < 30; i++)//3秒钟
                {
                    logger.Info("LOADING captcha...");
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
        private void giveDeltaPrice(tobid.rest.position.Bid bid, int delta) {

            logger.InfoFormat("BEGIN givePRICE(delta : {0})", delta);
            logger.Info("\tBEGIN identify PRICE...");
            byte[] content = new ScreenUtil().screenCaptureAsByte(bid.give.price.x, bid.give.price.y, 52, 18);
            String txtPrice = this.m_orcPrice.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
            int price = Int32.Parse(txtPrice);
            price += delta;
            logger.InfoFormat("\tEND   identified PRICE = {0}", txtPrice);
            txtPrice = String.Format("{0:D}", price);

            //INPUT BOX
            logger.WarnFormat("\tBEGIN input PRICE : {0}", txtPrice);
            ScreenUtil.SetCursorPos(bid.give.inputBox.x, bid.give.inputBox.y);
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

            for (int i = 0; i < txtPrice.Length; i++) {
                System.Threading.Thread.Sleep(50);
                ScreenUtil.keybd_event(ScreenUtil.keycode[txtPrice[i].ToString()], 0, 0, 0);
            }
            logger.Info("\tEND   input PRICE");

            //点击出价
            logger.Info("\tBEGIN click BUTTON[出价]");
            System.Threading.Thread.Sleep(50);
            ScreenUtil.SetCursorPos(bid.give.button.x, bid.give.button.y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[出价]");
            logger.Info("END   givePRICE");

            //启动线程截校验码
            System.Threading.Thread loading = new System.Threading.Thread(delegate() {
                ScreenUtil screen = new ScreenUtil();
                for (int i = 0; i < 30; i++)//3秒钟
                {
                    logger.Info("LOADING captcha...");
                    byte[] binaryCaptcha = new ScreenUtil().screenCaptureAsByte(bid.submit.captcha[0].x, bid.submit.captcha[0].y, 128, 28);
                    this.pictureCaptcha.Image = Bitmap.FromStream(new MemoryStream(binaryCaptcha));
                    System.Threading.Thread.Sleep(100);
                }
            });
            loading.Start();
            this.textInputCaptcha.Focus();
        }

        private void closeDialog(tobid.rest.position.Bid bid) {

            logger.Info("关闭校验码窗口");
            ScreenUtil.SetCursorPos(bid.submit.buttons[0].x + 188, bid.submit.buttons[0].y);//取消按钮
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
        }

        private void submit(tobid.rest.position.Bid bid, String activeCaptcha) {

            logger.InfoFormat("BEGIN submitCAPTCHA({0})", activeCaptcha);

            logger.Info("\tBEGIN make INPUT blank");
            ScreenUtil.SetCursorPos(bid.submit.inputBox.x, bid.submit.inputBox.y);
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

            logger.Info("\tBEGIN input CAPTCHA");
            {
                for (int i = 0; i < activeCaptcha.Length; i++) {
                    ScreenUtil.keybd_event(ScreenUtil.keycode[activeCaptcha[i].ToString()], 0, 0, 0);
                    System.Threading.Thread.Sleep(50);
                }
            }
            logger.Info("\tEND   input CAPTCHA");

            logger.Info("\tBEGIN click BUTTON[确定]");
            ScreenUtil.SetCursorPos(bid.submit.buttons[0].x, bid.submit.buttons[0].y);
            ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
            logger.Info("\tEND   click BUTTON[确定]");

            ScreenUtil.SetCursorPos(bid.submit.buttons[0].x + 188 / 2, bid.submit.buttons[0].y - 10);//确定按钮

            logger.InfoFormat("END submit({0})", activeCaptcha);
        }

        /// <summary>
        /// 出校验码，【for CTRL+左、上、右】
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="bid"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        private Boolean submit(String URL, tobid.rest.position.Bid bid, CaptchaInput input) {

            logger.InfoFormat("BEGIN submitCAPTCHA({0})", input);
            logger.Info("\tBEGIN make INPUT blank");
            ScreenUtil.SetCursorPos(bid.submit.inputBox.x, bid.submit.inputBox.y);
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
            byte[] binaryCaptcha = new ScreenUtil().screenCaptureAsByte(bid.submit.captcha[0].x, bid.submit.captcha[0].y, 128, 28);
            File.WriteAllBytes("CAPTCHA.BMP", binaryCaptcha);
            String txtCaptcha = this.m_orcCaptcha.IdentifyStringFromPic(new Bitmap(new MemoryStream(binaryCaptcha)));

            byte[] binaryTips = new ScreenUtil().screenCaptureAsByte(bid.submit.captcha[1].x, bid.submit.captcha[1].y, 112, 16);
            File.WriteAllBytes("TIPS.BMP", binaryTips);
            String txtActive = this.m_orcCaptchaTipsUtil.getActive(txtCaptcha, new Bitmap(new MemoryStream(binaryTips)));
            logger.InfoFormat("\tEND   identify CAPTCHA = {0}, ACTIVE = {1}", txtCaptcha, txtActive);

            logger.Info("\tBEGIN input CAPTCHA");
            {
                if (CaptchaInput.LEFT == input) {
                    for (int i = 0; i <= 3; i++) {
                        ScreenUtil.keybd_event(ScreenUtil.keycode[txtCaptcha[i].ToString()], 0, 0, 0);
                        System.Threading.Thread.Sleep(50);
                    }
                }
                if (CaptchaInput.MIDDLE == input) {
                    for (int i = 1; i <= 4; i++) {
                        ScreenUtil.keybd_event(ScreenUtil.keycode[txtCaptcha[i].ToString()], 0, 0, 0);
                        System.Threading.Thread.Sleep(50);
                    }
                }
                if (CaptchaInput.RIGHT == input) {
                    for (int i = 2; i <= 5; i++) {
                        ScreenUtil.keybd_event(ScreenUtil.keycode[txtCaptcha[i].ToString()], 0, 0, 0);
                        System.Threading.Thread.Sleep(50);
                    }
                }
            }
            logger.Info("\tEND   input CAPTCHA");

            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("确定要提交出价吗?", "提交出价", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            if (dr == DialogResult.OK) {
                logger.InfoFormat("用户选择确定出价");
                ScreenUtil.SetCursorPos(bid.submit.buttons[0].x, bid.submit.buttons[0].y);//确定按钮
                ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);

                System.Threading.Thread.Sleep(1000);
                ScreenUtil.SetCursorPos(bid.submit.buttons[0].x + 188 / 2, bid.submit.buttons[0].y - 10);//确定按钮
                //ScreenUtil.mouse_event((int)(MouseEventFlags.Absolute | MouseEventFlags.LeftDown | MouseEventFlags.LeftUp), 0, 0, 0, IntPtr.Zero);
                //ScreenUtil.SetCursorPos(bid.submit.buttons[0].x + 188 / 2, bid.submit.buttons[0].y - 10);//确定按钮
                logger.Info("END   submitCAPTCHA");
                return true;
            } else {
                logger.InfoFormat("用户选择取消出价");
                ScreenUtil.SetCursorPos(bid.submit.buttons[0].x + 188, bid.submit.buttons[0].y);//取消按钮
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
                BidOperation bidOps = SubmitPriceJob.getConfig();
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
                    SubmitPriceJob.setConfig(bidOps);
                }

                if (radioPrice.Checked) {
                    if ("".Equals(this.textPrice2.Text)) {
                        MessageBox.Show("请正确的价格");
                        this.textPrice2.Focus();
                        return;
                    }
                    this.textBox2.Text = this.textPrice2.Text;
                    SubmitPriceJob.setConfig(Int32.Parse(this.textPrice2.Text), bidOps);
                }

                if (null != this.submitPriceThread)
                    this.submitPriceThread.Abort();
                System.Threading.ThreadStart submitPriceThreadStart = new System.Threading.ThreadStart(this.m_schedulerSubmit.Start);
                this.submitPriceThread = new System.Threading.Thread(submitPriceThreadStart);
                this.submitPriceThread.Name = "submitPriceThread";
                this.submitPriceThread.Start();
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
                this.closeDialog(SubmitPriceJob.getPosition());
                this.pictureCaptcha.Image = null;
                ScreenUtil.SetForegroundWindow(this.Handle);
                this.textInputPrice.Focus();
                ScreenUtil.SetCursorPos(p1.X, p1.Y);
            }
            if (keyData == Keys.Enter) {
                if (this.textInputPrice.Focused) {
                    this.givePrice(SubmitPriceJob.getPosition(), this.textInputPrice.Text);
                    this.textInputCaptcha.Focus();
                } else if (this.textInputCaptcha.Focused) {
                    this.submit(SubmitPriceJob.getPosition(), this.textInputCaptcha.Text);
                    this.textInputCaptcha.Text = "";
                    this.textInputPrice.Focus();
                }
                ScreenUtil.SetForegroundWindow(this.Handle);
                ScreenUtil.SetCursorPos(p1.X, p1.Y);
            }
            //this.TopMost = true;
            //this.Show(this);
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

                    if (null != this.submitPriceThread)
                        this.submitPriceThread.Abort();

                    //加载配置项2
                    KeepAliveJob keepAliveJob = new KeepAliveJob(this.EndPoint, new ReceiveOperation(this.receiveOperation));
                    keepAliveJob.Execute();

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

                    if (null != this.submitPriceThread)
                        this.submitPriceThread.Abort();
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

                    if (null != this.submitPriceThread)
                        this.submitPriceThread.Abort();
                }
            }
        }
        #endregion        
    }
}
