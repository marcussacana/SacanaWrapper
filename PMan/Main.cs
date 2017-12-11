using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PMan {
    public partial class Main : Form {
        bool Installing = false;
        Plugin[] Plugins = null;
        public Main() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            Plugins = Updater.TreeRepositorie();

            foreach (Plugin Plugin in Plugins) {
                PluginList.Items.Add(new ListViewItem(new string[] {
                    Plugin.Name,
                    Plugin.Extensions,
                    Plugin.Type,
                    (Updater.IsInstalled(Plugin) ? "Yes" : "No"),
                    (Updater.IsUpdated(Plugin) ? "Yes" : "No")
                }));
            }
        }

        private void DownloadBNT_Click(object sender, EventArgs e) {
            Installing = true;

            foreach (ListViewItem Name in PluginList.SelectedItems) {
                Plugin Plugin = (from x in Plugins where Name.Text == x.Name select x).First();
                Updater.Install(Plugin);
                Application.DoEvents();
            }

            Installing = false;
        }
    }
}
