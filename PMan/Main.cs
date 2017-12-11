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
        bool Loading {
            set {
                Text = value ? "Plugin Manager - Working..." : "Plugin Manager";
            }
        }
        Plugin[] Plugins = null;
        

        public Main() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            Loading = true;
            Plugins = Updater.TreeRepositorie();

            ListPlugins(Plugins);
            
            Loading = false;
        }

        private void DownloadBNT_Click(object sender, EventArgs e) {
            Loading = true;
            string Failed = "Failed to Install:\n";
            bool OK = true;
            foreach (ListViewItem Name in PluginList.SelectedItems) {
                Plugin Plugin = (from x in Plugins where Name.Text == x.Name && Name.SubItems[1].Text == x.Extensions select x).First();

                if (!Updater.Install(Plugin)) {
                    Failed += Plugin.Name + "\n";
                    OK = false;
                }

                Application.DoEvents();
            }

            SearchBNT_Click(null, null);

            if (!OK)
                MessageBox.Show(Failed, "Plugin Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        
            Loading = false;
        }

        private void UnistallBNT_Click(object sender, EventArgs e) {
            Loading = true;


            foreach (ListViewItem Name in PluginList.SelectedItems) {
                Plugin Plugin = (from x in Plugins where Name.Text == x.Name && Name.SubItems[1].Text == x.Extensions select x).First();
                Updater.Unistall(Plugin);
                Application.DoEvents();
            }

            SearchBNT_Click(null, null);

            Loading = false;
        }


        private void ListPlugins(Plugin[] Plugins) {
            PluginList.Items.Clear();

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


        private void SearchBNT_Click(object sender, EventArgs e) {
            string Search = SearchTB.Text.ToLower().Trim();

            if (string.IsNullOrWhiteSpace(Search))
                ListPlugins(Plugins);
            else {
                Plugin[] Results = (from x in Plugins where x.Name.Trim().ToLower().Contains(Search) || x.Extensions.Trim().ToLower().Contains(Search) select x).ToArray();
                ListPlugins(Results);
            }
        }
    }
}
