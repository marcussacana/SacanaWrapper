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
            byte[] PluginList = DownloadData(UpdateFile);
            uint PluginsCount = uint.Parse(Ini.GetConfig("Main", "Count", PluginList, true));

            List<string> Names = new List<string>();
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

                    string Name = Ini.GetConfig("Plugin." + i, "Name;Title", PluginList, true);
                    Plugin Plg = new Plugin() {
                        Name = Ini.GetConfig("Plugin." + i, "Name;Title", PluginList, true),
                        Extensions = Ini.GetConfig("Plugin." + i, "Extension;Formats", PluginList, true),
                        Type = TP,
                        LastVer = Ini.GetConfig("Plugin." + i, "Build;Version", PluginList, true),
                        File = Ini.GetConfig("Plugin." + i, "File;Path", PluginList, true),
                        Old = Ini.GetConfig("Plugin." + i, "Old;Obsolete", PluginList, false)
                    };

                    Names.Add(Name);
                    Plugins.Add(Plg);
                } catch {
                    continue;
                }
            }


            var Ret = Plugins.ToArray();
            var Nms = Names.ToArray();
            Array.Sort(Nms, Ret);

            return Ret;
        }

        internal static bool IsInstalled(Plugin Plugin) {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File)) {
                return true;
            }
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.Old)) {
                return true;
            }
            return false;
        }
        internal static bool IsUpdated(Plugin Plugin) {
            if (!IsInstalled(Plugin))
                return false;

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.Old)) {
                return false;
            }

            bool Updated = true;
            string Ver = Ini.GetConfig("Plugin", "Version;Ver;Build", AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File, false);
            if (string.IsNullOrWhiteSpace(Ver))
                Updated = false;

            string[] LVer = Ver.Trim().Split('.');
            string[] OVer = Plugin.LastVer.Trim().Split('.');

            int i = 0;
            while (Updated) {
                if (LVer.Length <= i)
                    break;
                if (OVer.Length <= i) {
                    Updated = false;
                    break;
                }


                if (int.Parse(LVer[i]) < int.Parse(OVer[i]))
                    Updated = false;
                i++;
            }

            return Updated;
        }

        internal static bool Install(Plugin Plugin) {
            try {
                byte[] PIni = DownloadData(RepoPath + Plugin.File);

                string Module = string.Empty;
                if (Ini.GetConfig("Plugin", "File;file;Archive;archive;Arc;arc", PIni, false) != string.Empty) {
                    Module = Ini.GetConfig("Plugin", "File;file;Archive;archive;Arc;arc", PIni, true);
                } else {
                    Module = Path.GetFileNameWithoutExtension(Plugin.File);
                }

                byte[] ModuleContent;
                try {
                    ModuleContent = DownloadData(RepoPath + Module + ".cs");
                    Module += ".cs";
                } catch {
                    try {
                        ModuleContent = DownloadData(RepoPath + Module + ".vb");
                        Module += ".vb";
                    } catch {
                        try {
                            ModuleContent = DownloadData(RepoPath + Module + ".dll");
                            Module += ".dll";
                        } catch {
                            throw new Exception("Failed to Download the Plugin");
                        }
                    }
                }

                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Module, ModuleContent);
                File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File, PIni);
                Ini.SetConfig("Plugin", "Version", Plugin.LastVer, AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File);
                
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.Old)) {
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.Old);
                }

                return true;
            }catch {
                return false;
            }
        }

        internal static bool Unistall(Plugin Plugin) {
            string PIni = AppDomain.CurrentDomain.BaseDirectory + "Plugins\\" + Plugin.File;
            try {
                if (File.Exists(PIni))
                    File.Delete(PIni);
            } catch { return false; }

            return true;
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
        public string Name, Extensions, Type, LastVer, File, Old;
    }
}
