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
        }

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

                Form2 frm = new Form2(maxMS);
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
                String rtn = rest.MakeRequest(null, false);
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
                    String rtn = rest.MakeRequest(null, true);

                    this.groupBox2.Enabled = true;
                    this.button1.Enabled = true;
                } catch (Exception ex) {

                    this.groupBox2.Enabled = false;
                    this.button1.Enabled = false;

                    MessageBoxButtons messButton = MessageBoxButtons.OK;
                    DialogResult dr = MessageBox.Show("无效或已过期，请输入正确的授权码!", "授权失败", messButton, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                }
            }
            /*String credential = ConfigurationManager.AppSettings["credential"];
            String authCode = Microsoft.VisualBasic.Interaction.InputBox("请输入授权码", "", credential);
            if (!String.IsNullOrEmpty(authCode)) {

                Warrant warrant = new Warrant(authCode);
                String hostName = System.Net.Dns.GetHostName();
                String epKeepAlive = this.endPoint + "/rest/service/simulate/register/" + hostName;
                RestClient rest = new RestClient(epKeepAlive, HttpVerb.POST, warrant);
                try {
                    String rtn = rest.MakeRequest(null, false);

                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["credential"].Value = authCode;
                    config.Save();
                    ConfigurationManager.RefreshSection("appSettings");

                    MessageBoxButtons messButton = MessageBoxButtons.OK;
                    DialogResult dr = MessageBox.Show(String.Format("请重新启动应用程序!\r\n主机名:{0}\r\n授权码:{1}", hostName, authCode), "授权成功", messButton, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                }
                catch (Exception ex) {
                    System.Console.WriteLine(ex);
                    MessageBoxButtons messButton = MessageBoxButtons.OK;
                    DialogResult dr = MessageBox.Show("请重新输入授权码!", "授权失败", messButton, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                }
            }*/
        }

    }
}
