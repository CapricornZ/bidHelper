namespace Cheater {
    partial class Form1 {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.captchaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sToolStripMenuItem3s = new System.Windows.Forms.ToolStripMenuItem();
            this.sToolStripMenuItem5s = new System.Windows.Forms.ToolStripMenuItem();
            this.sToolStripMenuItem7s = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.captchaToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(115, 48);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "notifyIcon1";
            this.notifyIcon1.Visible = true;
            // 
            // captchaToolStripMenuItem
            // 
            this.captchaToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sToolStripMenuItem3s,
            this.sToolStripMenuItem5s,
            this.sToolStripMenuItem7s});
            this.captchaToolStripMenuItem.Name = "captchaToolStripMenuItem";
            this.captchaToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.captchaToolStripMenuItem.Text = "Captcha";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // sToolStripMenuItem3s
            // 
            this.sToolStripMenuItem3s.Name = "sToolStripMenuItem3s";
            this.sToolStripMenuItem3s.Size = new System.Drawing.Size(152, 22);
            this.sToolStripMenuItem3s.Text = "3s";
            this.sToolStripMenuItem3s.Click += new System.EventHandler(this.sToolStripMenuItem3s_Click);
            // 
            // sToolStripMenuItem5s
            // 
            this.sToolStripMenuItem5s.Name = "sToolStripMenuItem5s";
            this.sToolStripMenuItem5s.Size = new System.Drawing.Size(152, 22);
            this.sToolStripMenuItem5s.Text = "5s";
            this.sToolStripMenuItem5s.Click += new System.EventHandler(this.sToolStripMenuItem5s_Click);
            // 
            // sToolStripMenuItem7s
            // 
            this.sToolStripMenuItem7s.Name = "sToolStripMenuItem7s";
            this.sToolStripMenuItem7s.Size = new System.Drawing.Size(152, 22);
            this.sToolStripMenuItem7s.Text = "7s";
            this.sToolStripMenuItem7s.Click += new System.EventHandler(this.sToolStripMenuItem7s_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 112);
            this.Name = "Form1";
            this.ShowInTaskbar = false;
            this.Text = "Cheater";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem captchaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sToolStripMenuItem3s;
        private System.Windows.Forms.ToolStripMenuItem sToolStripMenuItem5s;
        private System.Windows.Forms.ToolStripMenuItem sToolStripMenuItem7s;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
    }
}

