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
            this.repository = Newtonsoft.Json.JsonConvert.DeserializeObject<Captcha[]>(repository);
        }

        private void button1_Click(object sender, EventArgs e) {

            this.button1.Enabled = false;
            int result = 0;
            int max = 25;
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

                totalMS += frm.Cost.TotalMilliseconds;
                if (frm.isManual && frm.Cost.TotalMilliseconds <= (maxMS + 500) && frm.Result)
                    result++;

                if (!frm.isManual && frm.Result)
                    result++;
            }

            float score = result * 100 / max;
            double avgMS = totalMS / max;
            this.label1.Text = String.Format("测试结束\r\n正确：{0}, 分数：{1}\r\n平均耗时：{2}", result, score, avgMS);
            if (score >= 80)
                this.pictureBox1.Image = Properties.Resources.like;
            else
                this.pictureBox1.Image = Properties.Resources.dislike;
            this.button1.Enabled = true;
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

    }
}
