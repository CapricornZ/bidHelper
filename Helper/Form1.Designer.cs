﻿namespace Helper
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.groupBoxLocal = new System.Windows.Forms.GroupBox();
            this.checkPriceOnly = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.radioDeltaPrice = new System.Windows.Forms.RadioButton();
            this.radioPrice = new System.Windows.Forms.RadioButton();
            this.btnUpdatePolicy = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.textPrice2 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.radioManualPolicy = new System.Windows.Forms.RadioButton();
            this.radioLocalPolicy = new System.Windows.Forms.RadioButton();
            this.radioServPolicy = new System.Windows.Forms.RadioButton();
            this.groupBoxManual = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textInputPrice = new System.Windows.Forms.TextBox();
            this.pictureCaptcha = new System.Windows.Forms.PictureBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textInputCaptcha = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.groupBoxPolicy = new System.Windows.Forms.GroupBox();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.xToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.国拍ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.模拟ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.配置ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stepToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBoxInterval = new System.Windows.Forms.ToolStripTextBox();
            this.buttonIE = new System.Windows.Forms.Button();
            this.buttonURL = new System.Windows.Forms.Button();
            this.buttonLogin = new System.Windows.Forms.Button();
            this.groupBoxLocal.SuspendLayout();
            this.panel1.SuspendLayout();
            this.groupBoxManual.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureCaptcha)).BeginInit();
            this.panel2.SuspendLayout();
            this.groupBoxPolicy.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxLocal
            // 
            this.groupBoxLocal.Controls.Add(this.checkPriceOnly);
            this.groupBoxLocal.Controls.Add(this.panel1);
            this.groupBoxLocal.Controls.Add(this.btnUpdatePolicy);
            this.groupBoxLocal.Controls.Add(this.comboBox1);
            this.groupBoxLocal.Controls.Add(this.textPrice2);
            this.groupBoxLocal.Controls.Add(this.label1);
            this.groupBoxLocal.Controls.Add(this.dateTimePicker1);
            this.groupBoxLocal.Enabled = false;
            this.groupBoxLocal.Location = new System.Drawing.Point(197, 27);
            this.groupBoxLocal.Name = "groupBoxLocal";
            this.groupBoxLocal.Size = new System.Drawing.Size(187, 143);
            this.groupBoxLocal.TabIndex = 5;
            this.groupBoxLocal.TabStop = false;
            this.groupBoxLocal.Text = "自选策略";
            // 
            // checkPriceOnly
            // 
            this.checkPriceOnly.AutoSize = true;
            this.checkPriceOnly.Location = new System.Drawing.Point(9, 119);
            this.checkPriceOnly.Name = "checkPriceOnly";
            this.checkPriceOnly.Size = new System.Drawing.Size(62, 17);
            this.checkPriceOnly.TabIndex = 5;
            this.checkPriceOnly.Text = "仅出价";
            this.checkPriceOnly.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radioDeltaPrice);
            this.panel1.Controls.Add(this.radioPrice);
            this.panel1.Location = new System.Drawing.Point(6, 50);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(77, 56);
            this.panel1.TabIndex = 3;
            // 
            // radioDeltaPrice
            // 
            this.radioDeltaPrice.AutoSize = true;
            this.radioDeltaPrice.Checked = true;
            this.radioDeltaPrice.Location = new System.Drawing.Point(3, 3);
            this.radioDeltaPrice.Name = "radioDeltaPrice";
            this.radioDeltaPrice.Size = new System.Drawing.Size(61, 17);
            this.radioDeltaPrice.TabIndex = 6;
            this.radioDeltaPrice.TabStop = true;
            this.radioDeltaPrice.Text = "加价格";
            this.radioDeltaPrice.UseVisualStyleBackColor = true;
            // 
            // radioPrice
            // 
            this.radioPrice.AutoSize = true;
            this.radioPrice.Location = new System.Drawing.Point(3, 38);
            this.radioPrice.Name = "radioPrice";
            this.radioPrice.Size = new System.Drawing.Size(73, 17);
            this.radioPrice.TabIndex = 7;
            this.radioPrice.TabStop = true;
            this.radioPrice.Text = "绝对价格";
            this.radioPrice.UseVisualStyleBackColor = true;
            // 
            // btnUpdatePolicy
            // 
            this.btnUpdatePolicy.Location = new System.Drawing.Point(89, 115);
            this.btnUpdatePolicy.Name = "btnUpdatePolicy";
            this.btnUpdatePolicy.Size = new System.Drawing.Size(75, 23);
            this.btnUpdatePolicy.TabIndex = 4;
            this.btnUpdatePolicy.Text = "更新策略(&s)";
            this.btnUpdatePolicy.UseVisualStyleBackColor = true;
            this.btnUpdatePolicy.Click += new System.EventHandler(this.button_updatePolicy_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "+100",
            "+200",
            "+300",
            "+400",
            "+500",
            "+600",
            "+700",
            "+800",
            "+900",
            "+1000"});
            this.comboBox1.Location = new System.Drawing.Point(89, 50);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(82, 21);
            this.comboBox1.TabIndex = 2;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // textPrice2
            // 
            this.textPrice2.Location = new System.Drawing.Point(89, 86);
            this.textPrice2.Name = "textPrice2";
            this.textPrice2.Size = new System.Drawing.Size(82, 20);
            this.textPrice2.TabIndex = 3;
            this.textPrice2.TextChanged += new System.EventHandler(this.textPrice2_TextChanged);
            this.textPrice2.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textPrice2_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(26, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "出价时间";
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.CustomFormat = "";
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dateTimePicker1.Location = new System.Drawing.Point(89, 19);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.ShowUpDown = true;
            this.dateTimePicker1.Size = new System.Drawing.Size(82, 20);
            this.dateTimePicker1.TabIndex = 1;
            this.dateTimePicker1.Value = new System.DateTime(2015, 8, 10, 11, 34, 9, 0);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // radioManualPolicy
            // 
            this.radioManualPolicy.AutoCheck = false;
            this.radioManualPolicy.AutoSize = true;
            this.radioManualPolicy.Checked = true;
            this.radioManualPolicy.Location = new System.Drawing.Point(192, 5);
            this.radioManualPolicy.Name = "radioManualPolicy";
            this.radioManualPolicy.Size = new System.Drawing.Size(49, 17);
            this.radioManualPolicy.TabIndex = 0;
            this.radioManualPolicy.TabStop = true;
            this.radioManualPolicy.Text = "手动";
            this.radioManualPolicy.UseVisualStyleBackColor = true;
            this.radioManualPolicy.Click += new System.EventHandler(this.radioManualPolicy_Click);
            // 
            // radioLocalPolicy
            // 
            this.radioLocalPolicy.AutoCheck = false;
            this.radioLocalPolicy.AutoSize = true;
            this.radioLocalPolicy.Location = new System.Drawing.Point(113, 5);
            this.radioLocalPolicy.Name = "radioLocalPolicy";
            this.radioLocalPolicy.Size = new System.Drawing.Size(73, 17);
            this.radioLocalPolicy.TabIndex = 1;
            this.radioLocalPolicy.Text = "自选策略";
            this.radioLocalPolicy.UseVisualStyleBackColor = true;
            this.radioLocalPolicy.Click += new System.EventHandler(this.radioLocalPolicy_Click);
            // 
            // radioServPolicy
            // 
            this.radioServPolicy.AutoCheck = false;
            this.radioServPolicy.AutoSize = true;
            this.radioServPolicy.Location = new System.Drawing.Point(16, 5);
            this.radioServPolicy.Name = "radioServPolicy";
            this.radioServPolicy.Size = new System.Drawing.Size(85, 17);
            this.radioServPolicy.TabIndex = 2;
            this.radioServPolicy.TabStop = true;
            this.radioServPolicy.Text = "服务器策略";
            this.radioServPolicy.UseVisualStyleBackColor = true;
            this.radioServPolicy.Click += new System.EventHandler(this.radioServPolicy_Click);
            // 
            // groupBoxManual
            // 
            this.groupBoxManual.Controls.Add(this.label7);
            this.groupBoxManual.Controls.Add(this.label6);
            this.groupBoxManual.Controls.Add(this.textInputPrice);
            this.groupBoxManual.Controls.Add(this.pictureCaptcha);
            this.groupBoxManual.Controls.Add(this.label5);
            this.groupBoxManual.Controls.Add(this.textInputCaptcha);
            this.groupBoxManual.Controls.Add(this.label4);
            this.groupBoxManual.Location = new System.Drawing.Point(406, 27);
            this.groupBoxManual.Name = "groupBoxManual";
            this.groupBoxManual.Size = new System.Drawing.Size(195, 143);
            this.groupBoxManual.TabIndex = 9;
            this.groupBoxManual.TabStop = false;
            this.groupBoxManual.Text = "手动";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 125);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(157, 13);
            this.label7.TabIndex = 7;
            this.label7.Text = "输入验证码+回车，立即提交";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 41);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(181, 13);
            this.label6.TabIndex = 6;
            this.label6.Text = "输入价格+回车，自动显示验证码";
            // 
            // textInputPrice
            // 
            this.textInputPrice.Location = new System.Drawing.Point(75, 16);
            this.textInputPrice.Name = "textInputPrice";
            this.textInputPrice.Size = new System.Drawing.Size(100, 20);
            this.textInputPrice.TabIndex = 5;
            this.textInputPrice.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textInputPrice_KeyPress);
            // 
            // pictureCaptcha
            // 
            this.pictureCaptcha.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureCaptcha.Location = new System.Drawing.Point(75, 57);
            this.pictureCaptcha.Name = "pictureCaptcha";
            this.pictureCaptcha.Size = new System.Drawing.Size(100, 37);
            this.pictureCaptcha.TabIndex = 4;
            this.pictureCaptcha.TabStop = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(17, 63);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(43, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "校验码";
            // 
            // textInputCaptcha
            // 
            this.textInputCaptcha.Location = new System.Drawing.Point(75, 102);
            this.textInputCaptcha.Name = "textInputCaptcha";
            this.textInputCaptcha.Size = new System.Drawing.Size(100, 20);
            this.textInputCaptcha.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(17, 19);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(55, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "手动价格";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.radioManualPolicy);
            this.panel2.Controls.Add(this.radioLocalPolicy);
            this.panel2.Controls.Add(this.radioServPolicy);
            this.panel2.Location = new System.Drawing.Point(138, 177);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(246, 29);
            this.panel2.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "价格";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "出价时间";
            // 
            // textBox2
            // 
            this.textBox2.Enabled = false;
            this.textBox2.Location = new System.Drawing.Point(63, 74);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(89, 20);
            this.textBox2.TabIndex = 1;
            // 
            // textBox1
            // 
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(63, 34);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(89, 20);
            this.textBox1.TabIndex = 0;
            // 
            // groupBoxPolicy
            // 
            this.groupBoxPolicy.Controls.Add(this.label3);
            this.groupBoxPolicy.Controls.Add(this.label2);
            this.groupBoxPolicy.Controls.Add(this.textBox2);
            this.groupBoxPolicy.Controls.Add(this.textBox1);
            this.groupBoxPolicy.Location = new System.Drawing.Point(12, 27);
            this.groupBoxPolicy.Name = "groupBoxPolicy";
            this.groupBoxPolicy.Size = new System.Drawing.Size(166, 143);
            this.groupBoxPolicy.TabIndex = 7;
            this.groupBoxPolicy.TabStop = false;
            this.groupBoxPolicy.Text = "服务器策略";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(532, 19);
            this.toolStripStatusLabel2.Spring = true;
            this.toolStripStatusLabel2.Text = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.toolStripStatusLabel1.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(59, 19);
            this.toolStripStatusLabel1.Text = "XX:XX:XX";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2});
            this.statusStrip1.Location = new System.Drawing.Point(0, 206);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(606, 24);
            this.statusStrip1.TabIndex = 6;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.xToolStripMenuItem,
            this.配置ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(606, 24);
            this.menuStrip1.TabIndex = 10;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // xToolStripMenuItem
            // 
            this.xToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.国拍ToolStripMenuItem,
            this.模拟ToolStripMenuItem});
            this.xToolStripMenuItem.Name = "xToolStripMenuItem";
            this.xToolStripMenuItem.Size = new System.Drawing.Size(67, 20);
            this.xToolStripMenuItem.Text = "环境选择";
            // 
            // 国拍ToolStripMenuItem
            // 
            this.国拍ToolStripMenuItem.Checked = true;
            this.国拍ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.国拍ToolStripMenuItem.Name = "国拍ToolStripMenuItem";
            this.国拍ToolStripMenuItem.Size = new System.Drawing.Size(98, 22);
            this.国拍ToolStripMenuItem.Text = "国拍";
            this.国拍ToolStripMenuItem.Click += new System.EventHandler(this.国拍ToolStripMenuItem_Click);
            // 
            // 模拟ToolStripMenuItem
            // 
            this.模拟ToolStripMenuItem.Name = "模拟ToolStripMenuItem";
            this.模拟ToolStripMenuItem.Size = new System.Drawing.Size(98, 22);
            this.模拟ToolStripMenuItem.Text = "模拟";
            this.模拟ToolStripMenuItem.Click += new System.EventHandler(this.模拟ToolStripMenuItem_Click);
            // 
            // 配置ToolStripMenuItem
            // 
            this.配置ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stepToolStripMenuItem,
            this.toolStripTextBoxInterval});
            this.配置ToolStripMenuItem.Name = "配置ToolStripMenuItem";
            this.配置ToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.配置ToolStripMenuItem.Text = "配置";
            // 
            // stepToolStripMenuItem
            // 
            this.stepToolStripMenuItem.Name = "stepToolStripMenuItem";
            this.stepToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.stepToolStripMenuItem.Text = "Step2";
            this.stepToolStripMenuItem.Click += new System.EventHandler(this.stepToolStripMenuItem_Click);
            // 
            // toolStripTextBoxInterval
            // 
            this.toolStripTextBoxInterval.Name = "toolStripTextBoxInterval";
            this.toolStripTextBoxInterval.Size = new System.Drawing.Size(100, 23);
            // 
            // buttonIE
            // 
            this.buttonIE.Location = new System.Drawing.Point(421, 178);
            this.buttonIE.Name = "buttonIE";
            this.buttonIE.Size = new System.Drawing.Size(54, 23);
            this.buttonIE.TabIndex = 11;
            this.buttonIE.Text = "打开&IE";
            this.buttonIE.UseVisualStyleBackColor = true;
            this.buttonIE.Click += new System.EventHandler(this.buttonIE_Click);
            // 
            // buttonURL
            // 
            this.buttonURL.Location = new System.Drawing.Point(481, 178);
            this.buttonURL.Name = "buttonURL";
            this.buttonURL.Size = new System.Drawing.Size(54, 23);
            this.buttonURL.TabIndex = 12;
            this.buttonURL.Text = "打开页(&U)";
            this.buttonURL.UseVisualStyleBackColor = true;
            this.buttonURL.Click += new System.EventHandler(this.buttonURL_Click);
            // 
            // buttonLogin
            // 
            this.buttonLogin.Location = new System.Drawing.Point(542, 177);
            this.buttonLogin.Name = "buttonLogin";
            this.buttonLogin.Size = new System.Drawing.Size(51, 23);
            this.buttonLogin.TabIndex = 13;
            this.buttonLogin.Text = "登录(&L)";
            this.buttonLogin.UseVisualStyleBackColor = true;
            this.buttonLogin.Click += new System.EventHandler(this.buttonLogin_Click_1);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(606, 230);
            this.Controls.Add(this.buttonLogin);
            this.Controls.Add(this.buttonURL);
            this.Controls.Add(this.buttonIE);
            this.Controls.Add(this.groupBoxLocal);
            this.Controls.Add(this.groupBoxManual);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.groupBoxPolicy);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Form1";
            this.TopMost = true;
            this.Activated += new System.EventHandler(this.Form1_Activated);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBoxLocal.ResumeLayout(false);
            this.groupBoxLocal.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBoxManual.ResumeLayout(false);
            this.groupBoxManual.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureCaptcha)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.groupBoxPolicy.ResumeLayout(false);
            this.groupBoxPolicy.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxLocal;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton radioDeltaPrice;
        private System.Windows.Forms.RadioButton radioPrice;
        private System.Windows.Forms.Button btnUpdatePolicy;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.TextBox textPrice2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.RadioButton radioManualPolicy;
        private System.Windows.Forms.RadioButton radioLocalPolicy;
        private System.Windows.Forms.RadioButton radioServPolicy;
        private System.Windows.Forms.GroupBox groupBoxManual;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textInputPrice;
        private System.Windows.Forms.PictureBox pictureCaptcha;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textInputCaptcha;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.GroupBox groupBoxPolicy;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.CheckBox checkPriceOnly;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem xToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 国拍ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 模拟ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 配置ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stepToolStripMenuItem;
        private System.Windows.Forms.Button buttonIE;
        private System.Windows.Forms.Button buttonURL;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxInterval;
        private System.Windows.Forms.Button buttonLogin;
    }
}

