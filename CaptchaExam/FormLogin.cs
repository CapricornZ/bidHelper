using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CaptchaExam {
    public partial class FormLogin : Form {
        public FormLogin() {
            InitializeComponent();
        }

        public String userName { get; set; }
        public String passWord { get; set; }
        public Boolean isCancel { get; set; }

        private void button1_Click(object sender, EventArgs e) {

            this.userName = this.textBoxUser.Text;
            this.passWord = this.textBoxPass.Text;
            this.isCancel = false;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e) {
            this.isCancel = true;
            this.Close();
        }
    }
}
