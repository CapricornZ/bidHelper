using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Helper {
    public partial class FloatingForm : Form {
        public FloatingForm() {
            InitializeComponent();
        }

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

        private void timer1_Tick(object sender, EventArgs e) {
            this.labelTime.Text = DateTime.Now.ToString("HH:mm:ss.fff");
        }
    }
}
