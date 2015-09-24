using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using tobid.rest.position;
using tobid.util.http;

namespace Admin {
    public partial class Step2Form : Form {
        public Step2Form() {
            InitializeComponent();
        }

        public BidStep2 bid { get; set; }
        public Boolean cancel { get; set; }
        public String endPoint { get; set; }

        private void object2InputBox(System.Windows.Forms.TextBox textBox, Position pos) {
            if (pos != null)
                textBox.Text = pos.x + "," + pos.y;
            else
                textBox.Text = "0,0";
        }
        private Position inputBox2Object(System.Windows.Forms.TextBox textBox) {
            string[] pos = textBox.Text.Split(new char[] { ',' });
            return new Position(Int16.Parse(pos[0]), Int16.Parse(pos[1]));
        }
        private Position inputBox2Object(System.Windows.Forms.TextBox textBox, int offsetX, int offsetY) {
            Position pos = this.inputBox2Object(textBox);
            pos.x += offsetX;
            pos.y += offsetY;
            return pos;
        }

        private void BidForm_Load(object sender, EventArgs e) {

            if (this.bid != null) {
                this.object2InputBox(this.textBox1, bid.give.price);
                this.object2InputBox(this.textBox2, bid.give.inputBox);
                this.object2InputBox(this.textBox3, bid.give.button);

                this.object2InputBox(this.textBox4, bid.submit.captcha[0]);
                this.object2InputBox(this.textBox5, bid.submit.captcha[1]);
                this.object2InputBox(this.textBox6, bid.submit.inputBox);
                this.object2InputBox(this.textBox7, bid.submit.buttons[0]);

                //this.object2InputBox(this.textBoxOrigin, bid.Origin);
            } else {
                this.object2InputBox(this.textBox1, new Position(0, 0));
                this.object2InputBox(this.textBox2, new Position(0, 0));
                this.object2InputBox(this.textBox3, new Position(0, 0));

                this.object2InputBox(this.textBox4, new Position(0, 0));
                this.object2InputBox(this.textBox5, new Position(0, 0));
                this.object2InputBox(this.textBox6, new Position(0, 0));
                this.object2InputBox(this.textBox7, new Position(0, 0));

                //this.object2InputBox(this.textBoxOrigin, new Position(0, 0));
            }
        }

        private void button_Ok_Click(object sender, EventArgs e) {

            GivePriceStep2 givePrice = new GivePriceStep2();
            givePrice.price = this.inputBox2Object(this.textBox1);//价格
            givePrice.inputBox = this.inputBox2Object(this.textBox2);//输入价格
            givePrice.button = this.inputBox2Object(this.textBox3);//出价按钮

            SubmitPrice submit = new SubmitPrice();
            submit.captcha = new Position[]{
                this.inputBox2Object(this.textBox4),//校验码
                this.inputBox2Object(this.textBox5)//校验码提示
            };
            submit.inputBox = this.inputBox2Object(this.textBox6);//输入校验码

            string[] posBtnOK = this.textBox7.Text.Split(new char[] { ',' });
            submit.buttons = new Position[]{
                this.inputBox2Object(this.textBox7),//确定按钮
                this.inputBox2Object(this.textBox7, offsetX:186, offsetY:0)//取消按钮
            };

            this.bid = new BidStep2();
            this.bid.give = givePrice;
            this.bid.submit = submit;
            //this.bid.Origin = this.inputBox2Object(this.textBoxOrigin);

            this.cancel = false;
            this.Close();
        }

        private void button_Cancel_Click(object sender, EventArgs e) {
            this.cancel = true;
            this.Close();
        }

        private void button_Post_Click(object sender, EventArgs e) {

            Rectangle screen = new Rectangle();
            screen = Screen.GetWorkingArea(this);

            GivePriceStep2 givePrice = new GivePriceStep2();
            givePrice.price = this.inputBox2Object(this.textBox1);//价格
            givePrice.inputBox = this.inputBox2Object(this.textBox2);//输入价格
            givePrice.button = this.inputBox2Object(this.textBox3);//出价按钮

            SubmitPrice submit = new SubmitPrice();
            submit.captcha = new Position[]{
                this.inputBox2Object(this.textBox4),//校验码
                this.inputBox2Object(this.textBox5)//校验码提示
            };
            submit.inputBox = this.inputBox2Object(this.textBox6);//输入校验码

            string[] posBtnOK = this.textBox7.Text.Split(new char[] { ',' });
            submit.buttons = new Position[]{
                this.inputBox2Object(this.textBox7),//确定按钮
                this.inputBox2Object(this.textBox7, offsetX:186, offsetY:0)//取消按钮
            };

            this.bid = new BidStep2();
            this.bid.give = givePrice;
            this.bid.submit = submit;

            MessageBoxButtons messButton = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("确定要提交该配置吗?", "提交BID配置", messButton);
            if (dr == DialogResult.OK) {
                string hostName = System.Net.Dns.GetHostName();
                string endpoint = this.endPoint + "/rest/service/command/operation/screenconfig/BidStep2";
                RestClient rest = new RestClient(endpoint: endpoint, method: HttpVerb.POST, postObj: this.bid);
                String response = rest.MakeRequest("?fromHost=" + String.Format("host:{0}, screen:{1}*{2}", hostName, screen.Width, screen.Height));
            }
        }
    }
}
