using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using tobid.scheduler.jobs;
using tobid.util;
using System.IO;

namespace Helper {
    public partial class CaptchaDlg : Form {

        private IRepository repository;

        const int WM_NCHITTEST = 0x0084;
        const int HTCLIENT = 0x0001;
        const int HTCAPTION = 0x0002;
        const int WM_NCLBUTTONDBLCLK = 0xA3;

        protected override void WndProc(ref Message m) {

            switch (m.Msg) {
                case WM_NCHITTEST:
                    base.WndProc(ref m);
                    if (m.Result == (IntPtr)HTCLIENT)
                        m.Result = (IntPtr)HTCAPTION;
                    break;
                case WM_NCLBUTTONDBLCLK:
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        public CaptchaDlg() {

            InitializeComponent();
        }

        public CaptchaDlg(IRepository repo) {
            
            InitializeComponent();
            this.repository = repo;
        }

        private void CaptchaDlg_Load(object sender, EventArgs e) {
            this.timer1.Enabled = true;
        }

        private void CaptchaDlg_FormClosed(object sender, FormClosedEventArgs e) {
            this.timer1.Enabled = false;
        }

        private void timer1_Tick(object sender, EventArgs e) {

            Point origin = tobid.util.IEUtil.findOrigin();
            tobid.rest.position.Rect rectCaptcha = this.repository.bidStep2.submit.captcha[0];
            byte[] content = new ScreenUtil().screenCaptureAsByte(origin.X + rectCaptcha.x, origin.Y + rectCaptcha.y, rectCaptcha.width, rectCaptcha.height);
            
            Bitmap bmp = new Bitmap(new MemoryStream(content));
            Bitmap tmpbmp = new Bitmap(rectCaptcha.width * 2, rectCaptcha.height * 2);

            Graphics g = Graphics.FromImage(tmpbmp);
            Rectangle oldrct = new Rectangle(0, 0, bmp.Width, bmp.Height);
            Rectangle newrct = new Rectangle(0, 0, tmpbmp.Width, tmpbmp.Height);

            g.DrawImage(bmp, newrct, oldrct, GraphicsUnit.Pixel);//newrct是你的目标矩形位置，oldrct是你原始图片的起始矩形位置 
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            this.pictureBox1.Image = tmpbmp;
            g.Dispose();
            this.pictureBox1.Update();
        }
    }
}
