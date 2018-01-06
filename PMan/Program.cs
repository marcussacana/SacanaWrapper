using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PMan {
    static class Program {
        /// <summary>
        /// Ponto de entrada principal para o aplicativo.
        /// </summary>
        [STAThread]
        static void Main(string[] Args) {
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Plugins"))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Plugins");

            if (Args?.Length > 0 && Args[0].Trim(' ', '-', '/').ToLower() == "update") {
                AutoUpdate();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }

        private static void AutoUpdate() {
            Console.WriteLine("Checking Updates...");

            Plugin[] Plugins = (from x in Updater.TreeRepositorie() where Updater.IsInstalled(x) select x).ToArray();
            bool Updated = true;
            foreach (Plugin Plugin in Plugins) {
                if (!Updater.IsUpdated(Plugin) && Ini.GetConfig("Plugin", "AutoUpdate", AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File, false) != "false") {
                    if (Updated) {
                        AllocConsole();
                        Updated = false;
                    }
                    Console.WriteLine("Updating {0}", Plugin.Name);
                    Updater.Install(Plugin);
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}
