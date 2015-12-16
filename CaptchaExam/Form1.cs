using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using tobid.util.http;
using Microsoft.Win32;
using CaptchaExam.Properties;
using System.Configuration;
using tobid.rest;

namespace CaptchaExam {
    
    public partial class Form1 : Form {

        public Form1() {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;

            this.formProxy = new FormProxy();
        }

        public Form1(string domain, string user, string password, string proxy) {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            
            this.formProxy = new FormProxy();
            formProxy.user = user;
            formProxy.password = password;
            formProxy.proxy = proxy;
            formProxy.domain = domain;
            this.formProxy.proxySetting = new Proxy(domain, user, password, proxy);
        }

        private FormProxy formProxy;
        private FormLogin formLogin = new FormLogin();
        private Captcha[] repository;
        private String endPoint = "http://139.196.24.58/captcha.server";

        private void Form1_Load(object sender, EventArgs e) {

            this.endPoint = ConfigurationManager.AppSettings["ENDPOINT"];
            //RestClient rest = new RestClient(this.endPoint + "/repository.json", HttpVerb.GET);
            //String repository = rest.MakeRequest(false);
            //this.repository = Newtonsoft.Json.JsonConvert.DeserializeObject<Captcha[]>(repository);
            
            this.label2.Text = String.Format("训练数量:{0}", this.hScrollBar1.Value);
        }

        private void button1_Click(object sender, EventArgs e) {

            this.button1.Enabled = false;
            int result = 0;
            int max = this.hScrollBar1.Value;
            double totalMS = 0;
            for (int i = 0; i < max; i++)
            {
                int maxMS = 0;
                if (radioEasy.Checked)
                    maxMS = 3000;
                if (radioNormal.Checked)
                    maxMS = 2000;
                if (radioHard.Checked)
                    maxMS = 1500;

                Form2 frm = new Form2(maxMS, this.formProxy.proxySetting);
                frm.ShowDialog();
                System.Console.WriteLine("COST:{0}, RESULT:{1}", frm.Cost.TotalMilliseconds, frm.Result);

                if (frm.isManual && frm.Cost.TotalMilliseconds <= (maxMS + 500) && frm.Result) {
                    result++;
                    totalMS += frm.Cost.TotalMilliseconds;
                }

                if (!frm.isManual && frm.Result) {
                    result++;
                    totalMS += frm.Cost.TotalMilliseconds;
                }
            }

            float score = result * 100 / max;
            double avgMS = totalMS / result;
            this.label1.Text = String.Format("测试结束\r\n正确：{0}, 分数：{1}\r\n平均耗时：{2:000.000}", result, score, avgMS);
            if (score >= 80)
                this.pictureBox1.Image = Properties.Resources.like;
            else
                this.pictureBox1.Image = Properties.Resources.dislike;
            this.button1.Enabled = true;

            ExamRecord record = new ExamRecord();
            if (this.radioEasy.Checked)
                record.level = "EASY";
            if (this.radioHard.Checked)
                record.level = "HARD";
            if (this.radioNormal.Checked)
                record.level = "NORMAL";
            record.total = max;
            record.correct = result;
            record.avgCost = (float)avgMS;

            //String hostName = System.Net.Dns.GetHostName();
            if (result > 0) {
                String hostName = this.formLogin.userName;
                String epKeepAlive = this.endPoint + "/rest/service/exam/client/" + hostName + "/record";
                RestClient rest = new RestClient(epKeepAlive, HttpVerb.POST, record);
                String rtn = rest.MakeRequest(null, false, this.formProxy.proxySetting);
            }
        }

        Random rd = new Random();
        private void timer1_Tick(object sender, EventArgs e) {
            
            int index = rd.Next(this.repository.Length);
            String url = String.Format("{0}/{1}", this.endPoint, this.repository[index]);
            HttpUtil httpReq = new HttpUtil();
            System.IO.Stream ms = httpReq.getAsBinary(url);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {
        }

        private void GET_IE(object sender, EventArgs e)
        {
            RegistryKey mreg;
            mreg = Registry.LocalMachine;
            mreg = mreg.CreateSubKey("software\\Microsoft\\Internet Explorer");
            string IEVersion="当前IE浏览器的版本信息："+(String)mreg.GetValue("Version");
            mreg.Close();
            label1.Text = IEVersion;
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e) {
            this.label2.Text = String.Format("训练数量:{0}", this.hScrollBar1.Value);
        }

        private void toolStripMenuItemRegister_Click(object sender, EventArgs e) {

            this.formLogin.ShowDialog(this);
            if (!this.formLogin.isCancel) {

                String credential = this.formLogin.passWord;
                String hostName = this.formLogin.userName;
                String epKeepAlive = this.endPoint + "/rest/service/simulate/keepAlive/" + hostName;
                RestClient rest = new RestClient(epKeepAlive, HttpVerb.PUT, hostName + ":" + credential);
                try {
                    String rtn = rest.MakeRequest(null, true, this.formProxy.proxySetting);

                    this.groupBox2.Enabled = true;
                    this.button1.Enabled = true;
                } catch (Exception ex) {

                    this.groupBox2.Enabled = false;
                    this.button1.Enabled = false;

                    MessageBoxButtons messButton = MessageBoxButtons.OK;
                    DialogResult dr = MessageBox.Show("无效或已过期，请输入正确的授权码!\r\n" + ex.ToString(), "授权失败", messButton, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                }
            }
        }

        private void ToolStripMenuItemProxy_Click(object sender, EventArgs e) {

            DialogResult dr = this.formProxy.ShowDialog(this);
        }

    }
}
