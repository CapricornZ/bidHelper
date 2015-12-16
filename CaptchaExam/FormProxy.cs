using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using tobid.util.http;
using System.Configuration;

namespace CaptchaExam {
    public partial class FormProxy : Form {
        public FormProxy() {
            InitializeComponent();
        }

        public string domain{get;set;}
        public string user{get;set;}
        public string password { get; set; }
        public string proxy { get; set; }

        public Proxy proxySetting { get; set; }

        private void FormProxy_Load(object sender, EventArgs e) {

            if (this.proxySetting == null)
                this.checkBox1.Checked = false;
            else {
                this.checkBox1.Checked = true;
                this.textBoxDomain.Text = this.domain;
                this.textBoxUser.Text = this.user;
                this.textBoxPass.Text = this.password;

                if (string.IsNullOrEmpty(this.proxy)) {
                    WebProxy proxy = (WebProxy)WebProxy.GetDefaultProxy();
                    if (proxy.Address != null)
                        this.proxy = proxy.Address.ToString();
                }
                this.textBoxProxy.Text = this.proxy;
            }
        }

        private void button1_Click(object sender, EventArgs e) {

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (this.checkBox1.Checked) {
                this.domain = this.textBoxDomain.Text;
                this.user = this.textBoxUser.Text;
                this.password = this.textBoxPass.Text;
                this.proxy = this.textBoxProxy.Text;

                config.AppSettings.Settings["domain"].Value = domain;
                config.AppSettings.Settings["user"].Value = user;
                config.AppSettings.Settings["password"].Value = password;
                config.AppSettings.Settings["proxy"].Value = proxy;
                config.AppSettings.Settings["useProxy"].Value = "true";
                this.proxySetting = new Proxy(this.domain, this.user, this.password, this.proxy);
            } else {

                config.AppSettings.Settings["useProxy"].Value = "false";
                this.proxySetting = null;
            }
            config.Save();
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) {
            if (this.checkBox1.Checked)
                this.groupBox1.Enabled = true;
            else
                this.groupBox1.Enabled = false;
        }

    }
}
