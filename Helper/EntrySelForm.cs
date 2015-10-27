using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using tobid.rest;

namespace Helper {
    public partial class EntrySelForm : Form {
        public EntrySelForm() {
            InitializeComponent();
        }

        public Entry[] Entries { get; set; }
        public Entry SelectedEntry { get; set; }
        private Button[] buttons;

        private void EntrySelForm_Load(object sender, EventArgs e) {

            buttons = new Button[]{
                this.button1,
                this.button2
            };

            for (int i = 0; i < this.Entries.Length; i++)
                this.buttons[i].Text = this.Entries[i].description;
            this.SelectedEntry = null;
        }

        private void button1_Click(object sender, EventArgs e) {
            this.SelectedEntry = this.Entries[0];
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e) {
            this.SelectedEntry = this.Entries[1];
            this.Close();
        }
    }
}
