using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using tobid.rest;
using tobid.scheduler.jobs;

namespace Cheater {
    public partial class Form1 : Form {

        [DllImport("user32.dll")]
        private static extern int GetDC(int hwnd);

        private String[] bitmaps;
        private Random ran = new Random();
        private String EndPoint { get; set; }
        public Form1(String endPoint) {

            InitializeComponent();
            this.EndPoint = endPoint;
        }

        private void Form1_Load(object sender, EventArgs e) {

            this.bitmaps = new String[]{
                "066289.bmp", "102224.bmp", "152610.bmp", "166573.bmp", "202979.bmp",
                "257757.bmp", "522857.bmp", "654714.bmp", "709177.bmp", "882277.bmp",
                "964932.bmp", "992986.bmp"
            };

            KeepAliveJob keepAliveJob = new KeepAliveJob(this.EndPoint, new ReceiveOperation(this.receiveOperation));
            keepAliveJob.Execute();
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

            int RandKey = ran.Next(0, 11);
            byte[] binary = System.IO.File.ReadAllBytes(this.bitmaps[RandKey]);
            Bitmap bitmap = new Bitmap(new System.IO.MemoryStream(binary));
            System.IntPtr p = (IntPtr)GetDC(0);// '取得屏幕
            Graphics g = Graphics.FromHdc(p);

            for (int i = 0; i < 20 * second; i++) {
                //g.DrawImage(bitmap, new Point(966, 482));
                g.DrawImage(bitmap, new Point(SubmitPriceStep2Job.getPosition().submit.captcha[0].x + 4, SubmitPriceStep2Job.getPosition().submit.captcha[0].y));
                System.Threading.Thread.Sleep(50);
            }
        }

        private void showLoading(int second) {

            byte[] binary = System.IO.File.ReadAllBytes("loading.bmp");
            Bitmap bitmap = new Bitmap(new System.IO.MemoryStream(binary));
            System.IntPtr p = (IntPtr)GetDC(0);// '取得屏幕
            Graphics g = Graphics.FromHdc(p);

            for (int i = 0; i < 20 * second; i++) {
                //g.DrawImage(bitmap, new Point(962, 486));
                g.DrawImage(bitmap, new Point(SubmitPriceStep2Job.getPosition().submit.captcha[0].x, SubmitPriceStep2Job.getPosition().submit.captcha[0].y + 4));
                System.Threading.Thread.Sleep(50);
            }
        }
        
    }
}
