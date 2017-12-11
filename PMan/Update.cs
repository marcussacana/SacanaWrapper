using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace PMan {
    internal static class Updater {
        const string RepoPath = "https://raw.githubusercontent.com/marcussacana/SacanaWrapper/updater/Updater/";


        internal static Plugin[] TreeRepositorie() {
            const string UpdateFile = RepoPath + "Updater.ini";
            string[] PluginList = DownloadString(UpdateFile).Replace("\r\n", "\n").Split('\n');
            uint PluginsCount = uint.Parse(Ini.GetConfig("Main", "Count", PluginList, true));

            List<Plugin> Plugins = new List<Plugin>();
            for (uint i = 0; i < PluginsCount; i++) {
                try {
                    string TP = Ini.GetConfig("Plugin." + i, "Mode;Modes", PluginList, true);

                    if (TP.ToUpper().Contains("W") && TP.ToUpper().Contains("R"))
                        TP = "Read/Write";
                    else if (TP.Contains("R"))
                        TP = "Read Only";
                    else if (TP.Contains("W"))
                        TP = "Write Only";

                    Plugins.Add(new Plugin() {
                        Name = Ini.GetConfig("Plugin." + i, "Name;Title", PluginList, true),
                        Extensions = Ini.GetConfig("Plugin." + i, "Extension;Formats", PluginList, true),
                        Type = TP,
                        LastVer = Ini.GetConfig("Plugin." + i, "Build;Version", PluginList, true),
                        File = Ini.GetConfig("Plugin." + i, "File;Path", PluginList, true)
                    });
                } catch {
                    continue;
                }
            }

            return Plugins.ToArray();
        }

        internal static bool IsInstalled(Plugin Plugin) {
            bool Installed = false;
            Installed |= File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File + ".inf");
            Installed |= File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File + ".ini");
            Installed |= File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File + ".cfg");

            return Installed;
        }

        internal static bool IsUpdated(Plugin Plugin) {
            if (!IsInstalled(Plugin))
                return false;

            bool Updated = false;
            Updated |= Ini.GetConfig("Plugin", "Version;Ver;Build", AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File + ".inf", false) == Plugin.LastVer;
            Updated |= Ini.GetConfig("Plugin", "Version;Ver;Build", AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File + ".ini", false) == Plugin.LastVer;
            Updated |= Ini.GetConfig("Plugin", "Version;Ver;Build", AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File + ".cfg", false) == Plugin.LastVer;

            return Updated;
        }

        internal static bool Install(Plugin Plugin) {
            try {
                string[] PIni = DownloadString(RepoPath + Plugin.File).Replace("\r\n", "\n").Split('\n');

                string Module = string.Empty;
                if (Ini.GetConfig("Plugin", "File;file;Archive;archive;Arc;arc", PIni, false) != string.Empty) {
                    Module = Ini.GetConfig("Plugin", "File;file;Archive;archive;Arc;arc", PIni, true);
                } else {
                    Module = Path.GetFileName(Plugin.File);
                }

                byte[] ModuleContent;
                try {
                    ModuleContent = DownloadData(RepoPath + Module + ".cs");
                } catch {
                    try {
                        ModuleContent = DownloadData(RepoPath + Module + ".vb");
                    } catch {
                        try {
                            ModuleContent = DownloadData(RepoPath + Module + ".dll");
                        } catch {
                            throw new Exception("Failed to Download the Plugin");
                        }
                    }
                }

                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Module, ModuleContent);
                File.WriteAllLines(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File, PIni, Encoding.UTF8);

                return true;
            }catch {
                return false;
            }
        }

        private static string DownloadString(string Url) => Encoding.UTF8.GetString(DownloadData(Url));

        private static byte[] DownloadData(string Url) {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Url);
            Request.UseDefaultCredentials = true;
            Request.Method = "GET";
            WebResponse Response = Request.GetResponse();
            byte[] FC = new byte[0];
            using (MemoryStream Data = new MemoryStream())
            using (Stream Reader = Response.GetResponseStream()) {
                byte[] Buffer = new byte[1024];
                int bytesRead;
                do {
                    bytesRead = Reader.Read(Buffer, 0, Buffer.Length);
                    Data.Write(Buffer, 0, bytesRead);
                    Application.DoEvents();
                } while (bytesRead > 0);
                FC = Data.ToArray();
            }
            return FC;
        }
    }

    internal struct Plugin {
        public string Name, Extensions, Type, LastVer, File;
    }
}
