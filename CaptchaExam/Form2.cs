using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using tobid.util.http;

namespace CaptchaExam
{
    public partial class Form2 : Form
    {
        public Form2(int ms)
        {
            InitializeComponent();
            this.max = ms;
        }

        private Captcha[] repository;
        private String endPoint = "http://139.196.24.58/captcha.server";

        Random rd = new Random();
        private string captcha = "";
        private DateTime start;
        private DateTime end;
        private int max;

        public Boolean Result { get; set; }
        public TimeSpan Cost { get { return end - start; } }
        public Boolean isManual { get; set; }

        private void Form2_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;

            this.isManual = false;
            RestClient rest = new RestClient(this.endPoint + "/repository.json", HttpVerb.GET);
            String repository = rest.MakeRequest(false);
            this.repository = Newtonsoft.Json.JsonConvert.DeserializeObject<Captcha[]>(repository);

            System.Threading.Thread startCaptcha = new System.Threading.Thread(delegate()
            {
                int index = rd.Next(this.repository.Length);
                String url = String.Format("{0}/{1}", this.endPoint, this.repository[index].url);

                this.labelTips.Text = "                    ";
                this.pictureBoxCaptcha.Image = Properties.Resources.Loading;


                HttpUtil httpReq = new HttpUtil();
                System.IO.Stream ms = httpReq.getAsBinary(url);

                this.pictureBoxCaptcha.Image = new Bitmap(ms);
                this.captcha = this.repository[index].value;
                this.labelTips.Text = this.repository[index].tip;

                this.textBox1.Text = "";
                this.textBox1.Focus();

                this.start = DateTime.Now;
                for (int left = max; left>=0; left -= 250)
                {
                    this.label1.Text = String.Format("{0}ms", left);
                    System.Threading.Thread.Sleep(250);
                }

                this.label1.ForeColor = Color.Red;
                this.label1.Text = "TIMEOUT!";

                this.end = DateTime.Now;
                this.Result = this.textBox1.Text.Equals(this.captcha);
                this.Close();
            });
            startCaptcha.Start();
        }


        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.end = DateTime.Now;
            this.Result = false;
            this.Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.end = DateTime.Now;
            this.Result = this.textBox1.Text.Equals(this.captcha);
            this.Close();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ')
            {
                this.isManual = true;
                this.end = DateTime.Now;
                this.Result = this.textBox1.Text.Equals(this.captcha);
                this.Close();
            }
        }
    }
}
