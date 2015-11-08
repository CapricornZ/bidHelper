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

namespace CaptchaExam {
    
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private Captcha[] repository;
        private String endPoint = "http://139.196.24.58/captcha.server";

        private void Form1_Load(object sender, EventArgs e) {

            RestClient rest = new RestClient(this.endPoint + "/repository.json", HttpVerb.GET);
            String repository = rest.MakeRequest(false);
            //repository = repository.Replace("{", "").Replace("}", "");
            this.repository = Newtonsoft.Json.JsonConvert.DeserializeObject<Captcha[]>(repository);
        }

        private void button1_Click(object sender, EventArgs e) {
            
            this.startCaptcha = new System.Threading.Thread(delegate() {

                this.button1.Enabled = false;
                int correct = 0;

                int lastIndex = -1;
                for (int i = 0; i < 15; i++) {
                    reGen:
                        int index = rd.Next(this.repository.Length);
                    if (index != lastIndex)
                        lastIndex = index;
                    else
                        goto reGen;
                    String url = String.Format("{0}/{1}.bmp", this.endPoint, this.repository[index].value);
                    this.label2.Text = this.repository[index].tip;
                    this.pictureBox2.Image = null;

                    HttpUtil httpReq = new HttpUtil();
                    System.IO.Stream ms = httpReq.getAsBinary(url);
                    this.pictureBox1.Image = new Bitmap(ms);

                    this.textBox1.Text = "";
                    this.textBox1.Focus();

                    if(radioEasy.Checked){

                        for (int sleep = 6; sleep > 0; sleep--) {
                            this.label1.Text = String.Format("{0:f}", (float)sleep / 2);
                            System.Threading.Thread.Sleep(500);
                        }
                    }
                    if (radioNormal.Checked) {
                        for (int sleep = 4; sleep > 0; sleep--) {
                            this.label1.Text = String.Format("{0:f}", (float)sleep / 2);
                            System.Threading.Thread.Sleep(500);
                        }
                    }
                    if (radioHard.Checked) {
                        for (int sleep = 3; sleep > 0; sleep--) {
                            this.label1.Text = String.Format("{0:f}", (float)sleep / 2);
                            System.Threading.Thread.Sleep(500);
                        }
                    }

                    if (this.textBox1.Text.Equals(this.repository[index].value)) {

                        correct++;
                        this.pictureBox2.Image = Resources.OK_24px_1114048_easyicon_net;
                    } else
                        this.pictureBox2.Image = Resources.Error_24px_1114051_easyicon_net;
                    System.Threading.Thread.Sleep(1000);
                }

                this.label1.Text = String.Format("测试结束\r\n正确：{0}, 分数：{1}", correct, correct*100/15);
                this.button1.Enabled = true;
            });

            startCaptcha.SetApartmentState(System.Threading.ApartmentState.MTA);
            startCaptcha.Start();
            
        }

        Random rd = new Random();
        System.Threading.Thread startCaptcha;
        private void timer1_Tick(object sender, EventArgs e) {
            
            int index = rd.Next(this.repository.Length);
            String url = String.Format("{0}/{1}", this.endPoint, this.repository[index]);
            HttpUtil httpReq = new HttpUtil();
            System.IO.Stream ms = httpReq.getAsBinary(url);
            this.pictureBox1.Image = new Bitmap(ms);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {
            if (null != this.startCaptcha)
                this.startCaptcha.Abort();
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
    }
}
