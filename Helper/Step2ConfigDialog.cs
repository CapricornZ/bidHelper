using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using tobid.rest.position;
using tobid.scheduler.jobs;
using tobid.rest;
using tobid.util;
using System.IO;

namespace Helper
{
    public partial class Step2ConfigDialog : Form
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Step2ConfigDialog));
        public Boolean cancel { get; set; }
        private PictureBox[] m_pictureSubs;
        private IRepository m_repository;

        public Step2ConfigDialog(IRepository repo)
        {
            InitializeComponent();

            this.m_repository = repo;
            this.m_pictureSubs = new PictureBox[]{
                this.pictureSub1,
                this.pictureSub2,
                this.pictureSub3,
                this.pictureSub4,
                this.pictureSub5,
                this.pictureSub6
            };
        }

        private void object2InputBox(System.Windows.Forms.TextBox textBox, Position pos)
        {
            if (pos != null)
                textBox.Text = pos.x + "," + pos.y;
            else
                textBox.Text = "0,0";
        }

        private Position inputBox2Object(System.Windows.Forms.TextBox textBox)
        {
            string[] pos = textBox.Text.Split(new char[] { ',' });
            return new Position(Int16.Parse(pos[0]), Int16.Parse(pos[1]));
        }

        private Position inputBox2Object(System.Windows.Forms.TextBox textBox, int offsetX, int offsetY)
        {
            Position pos = this.inputBox2Object(textBox);
            pos.x += offsetX;
            pos.y += offsetY;
            return pos;
        }

        public BidStep2 BidStep2
        {
            get;
            set;
        }

        private void Step2ConfigDialog_Load(object sender, EventArgs e)
        {
            BidStep2 bid = SubmitPriceStep2Job.getPosition();
            this.checkboxDelta.Checked = this.m_repository.deltaPriceOnUI;
            if (bid != null) {
                this.object2InputBox(this.textBox1, bid.give.price);
                this.object2InputBox(this.textBox2, bid.give.inputBox);
                this.object2InputBox(this.textBox3, bid.give.button);
                if (bid.give.delta != null)
                {
                    this.object2InputBox(this.textDeltaInput, bid.give.delta.inputBox);
                    this.object2InputBox(this.textDeltaButton, bid.give.delta.button);
                }
                else
                {
                    this.object2InputBox(this.textDeltaButton, new Position(0, 0));
                    this.object2InputBox(this.textDeltaInput, new Position(0, 0));
                }

                this.object2InputBox(this.textBox4, bid.submit.captcha[0]);
                this.object2InputBox(this.textBox5, bid.submit.captcha[1]);
                this.object2InputBox(this.textBox6, bid.submit.inputBox);
                this.object2InputBox(this.textBox7, bid.submit.buttons[0]);

                this.object2InputBox(this.textBoxTitle, bid.title);
                this.object2InputBox(this.textBoxTitleOk, bid.okButton);
                this.object2InputBox(this.textBoxPriceSM, bid.price);
                this.object2InputBox(this.textBoxTime, bid.time);
            } else {

                this.object2InputBox(this.textBox1, new Position(0, 0));
                this.object2InputBox(this.textBox2, new Position(0, 0));
                this.object2InputBox(this.textBox3, new Position(0, 0));
                this.object2InputBox(this.textDeltaButton, new Position(0, 0));
                this.object2InputBox(this.textDeltaInput, new Position(0, 0));

                this.object2InputBox(this.textBox4, new Position(0, 0));
                this.object2InputBox(this.textBox5, new Position(0, 0));
                this.object2InputBox(this.textBox6, new Position(0, 0));
                this.object2InputBox(this.textBox7, new Position(0, 0));

                this.object2InputBox(this.textBoxTitle, new Position(0, 0));
                this.object2InputBox(this.textBoxTitleOk, new Position(0, 0));
                this.object2InputBox(this.textBoxPriceSM, new Position(0, 0));
                this.object2InputBox(this.textBoxTime, new Position(0, 0));
            }
        }

        private void buttonOK_Click(object sender, EventArgs e) {

            Delta delta = new Delta();
            delta.inputBox = this.inputBox2Object(this.textDeltaInput);
            delta.button = this.inputBox2Object(this.textDeltaButton);

            GivePriceStep2 givePrice = new GivePriceStep2();
            givePrice.price = this.inputBox2Object(this.textBox1);//价格
            givePrice.inputBox = this.inputBox2Object(this.textBox2);//输入价格
            givePrice.button = this.inputBox2Object(this.textBox3);//出价按钮
            givePrice.delta = delta;//

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

            BidStep2 bid = new BidStep2();
            bid.give = givePrice;
            bid.submit = submit;
            //bid.Origin = this.inputBox2Object(this.textBoxOrigin);
            bid.title = this.inputBox2Object(this.textBoxTitle);
            bid.okButton = this.inputBox2Object(this.textBoxTitleOk);
            bid.price = this.inputBox2Object(this.textBoxPriceSM);
            bid.time = this.inputBox2Object(this.textBoxTime);
            SubmitPriceStep2Job.setPosition(bid);

            this.BidStep2 = bid;

            this.m_repository.deltaPriceOnUI = this.checkboxDelta.Checked;
            
            this.cancel = false;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e) {

            this.cancel = true;
            this.Close();
        }

        #region 测试按钮组
        private void btnPrice_Click(object sender, EventArgs e) {

            Point origin = tobid.util.IEUtil.findOrigin();
            Position pos = this.inputBox2Object(this.textBox1);

            foreach (PictureBox picBox in this.m_pictureSubs)
                picBox.Image = null;
            
            byte[] content = new ScreenUtil().screenCaptureAsByte(origin.X + pos.x, origin.Y + pos.y, 100, 24);
            this.pictureBox1.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));
            String txtPrice = this.m_repository.orcPrice.IdentifyStringFromPic(new Bitmap(this.pictureBox1.Image));
            for (int i = 0; i < this.m_repository.orcPrice.SubImgs.Count; i++)
                this.m_pictureSubs[i].Image = this.m_repository.orcPrice.SubImgs[i];
            this.labelResult.Text = txtPrice;
        }

        private void btnPriceSM_Click(object sender, EventArgs e)
        {
            Point origin = tobid.util.IEUtil.findOrigin();
            Position pos = this.inputBox2Object(this.textBoxPriceSM);

            foreach (PictureBox picBox in this.m_pictureSubs)
                picBox.Image = null;

            byte[] content = new ScreenUtil().screenCaptureAsByte(origin.X + pos.x, origin.Y + pos.y, 100, 24);
            this.pictureBox1.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));
            String txtPrice = this.m_repository.orcPriceSM.IdentifyStringFromPic(new Bitmap(this.pictureBox1.Image));
            for (int i = 0; i < this.m_repository.orcPriceSM.SubImgs.Count; i++)
                this.m_pictureSubs[i].Image = this.m_repository.orcPriceSM.SubImgs[i];
            this.labelResult.Text = txtPrice;
        }

        private void btnCaptcha_Click(object sender, EventArgs e) {

            Point origin = tobid.util.IEUtil.findOrigin();
            Position pos = this.inputBox2Object(this.textBox4);

            foreach (PictureBox picBox in this.m_pictureSubs)
                picBox.Image = null;

            byte[] content = new ScreenUtil().screenCaptureAsByte(origin.X + pos.x, origin.Y + pos.y, 128, 28);
            this.pictureBox1.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));
            File.WriteAllBytes("CAPTCHA.BMP", content);
            String strCaptcha = this.m_repository.orcCaptcha.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
            //String[] array = Newtonsoft.Json.JsonConvert.DeserializeObject<String[]>(strCaptcha);

            for (int i = 0; i < 6; i++)
                this.m_pictureSubs[i].Image = this.m_repository.orcCaptcha.SubImgs[i];
            this.labelResult.Text = strCaptcha;
        }

        private void btnTips_Click(object sender, EventArgs e) {

            Point origin = tobid.util.IEUtil.findOrigin();
            Position pos = this.inputBox2Object(this.textBox5);

            foreach (PictureBox picBox in this.m_pictureSubs)
                picBox.Image = null;

            byte[] content = new ScreenUtil().screenCaptureAsByte(origin.X + pos.x, origin.Y + pos.y, 140, 24);
            this.pictureBox1.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));

            this.labelTips.Text = this.m_repository.orcCaptchaTipsUtil.getActive("一二三四五六", new Bitmap(new System.IO.MemoryStream(content)));
            for (int i = 0; i < this.m_repository.orcCaptchaTipsUtil.SubImgs.Count; i++)
                this.m_pictureSubs[i].Image = this.m_repository.orcCaptchaTipsUtil.SubImgs[i];
        }

        private void buttonTime_Click(object sender, EventArgs e) {

            Point origin = tobid.util.IEUtil.findOrigin();
            Position pos = this.inputBox2Object(this.textBoxTime);

            foreach (PictureBox picBox in this.m_pictureSubs)
                picBox.Image = null;

            byte[] content = new ScreenUtil().screenCaptureAsByte(origin.X + pos.x, origin.Y + pos.y, 140, 24);
            this.pictureBox1.Image = Bitmap.FromStream(new System.IO.MemoryStream(content));

            String strTime = this.m_repository.orcTime.IdentifyStringFromPic(new Bitmap(new System.IO.MemoryStream(content)));
            for (int i = 0; i < 6; i++)
                this.m_pictureSubs[i].Image = this.m_repository.orcTime.SubImgs[i];
            this.labelResult.Text = strTime;
        }
        #endregion

        private void btnGoto_Click(object sender, EventArgs e)
        {
            Position pos = this.inputBox2Object(this.textBox8);
            Point origin = tobid.util.IEUtil.findOrigin();
            System.Console.WriteLine(String.Format("origin : {{ x:{0}, y:{1} }}", origin.X, origin.Y));

            ScreenUtil.SetCursorPos(origin.X + pos.x, origin.Y + pos.y);
            //KeyBoardUtil.moveMouse(origin.X + pos.x, origin.Y + pos.y);
            System.Console.WriteLine(String.Format("goto : {{ x:{0}, y:{1} }}", pos.x, pos.y));
        }

        private void Delta_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkboxDelta.Checked)
            {
                this.textDeltaButton.Enabled = true;
                this.textDeltaInput.Enabled = true;
            }
            else
            {
                this.textDeltaInput.Enabled = false;
                this.textDeltaButton.Enabled = false;
            }
        }

        
    }
}
