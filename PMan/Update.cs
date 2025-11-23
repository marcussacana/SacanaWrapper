using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Forms;

namespace PMan {
    internal static class Updater {
        const string RepoPath = "https://raw.githubusercontent.com/marcussacana/SacanaWrapper/updater/Updater/";

        internal static string PluginDir => AppDomain.CurrentDomain.BaseDirectory + "Plugins/";
        internal static string WrapperPath => AppDomain.CurrentDomain.BaseDirectory + "SacanaWrapper.dll";
        internal static Plugin[] TreeRepositorie() {
            byte[] PluginList = DownloadData(RepoPath + "Updater.ini");
            uint PluginsCount = uint.Parse(Ini.GetConfig("Repo", "Count", PluginList, true));
            uint Version = uint.Parse(Ini.GetConfig("Repo", "Version", PluginList, true));
            var WrapperVer = new Version(Ini.GetConfig("Repo", "WrapperVer", PluginList, true));
            var CurrWrapperVer = new Version(FileVersionInfo.GetVersionInfo(WrapperPath).FileVersion);

            if (Version > 3) {
                throw new Exception("The Plugin Manager Is Outdated");
            }

            if (WrapperVer > CurrWrapperVer)
            {
                if (CurrWrapperVer != new Version(1, 0, 0, 0))
                    throw new Exception("The SacanaWrapper Is Outdated");
                MessageBox.Show(null, "You're Running a Debug Version of the SacanaWrapper", "Plugin Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

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
                        Dependencies = Ini.GetConfig("Plugin." + i, "Dependencies;References", PluginList, false),
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
            if (File.Exists(PluginDir + Plugin.File)) {
                return true;
            }
            if (File.Exists(PluginDir + Plugin.Old)) {
                return true;
            }
            return false;
        }
        internal static bool IsUpdated(Plugin Plugin) {
            if (!IsInstalled(Plugin))
                return false;

            if (File.Exists(PluginDir + Plugin.Old)) {
                return false;
            }

            bool Updated = true;
            string Ver = Ini.GetConfig("Plugin", "Version;Ver;Build", PluginDir + Plugin.File, false);
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

        internal static bool Install(Plugin Plugin, out string error) {
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
                } 
                catch (Exception ex)
                {
                    if (ex is WebException wex && wex.Response is HttpWebResponse hres && hres.StatusCode == (HttpStatusCode)429)
                    {
                        throw new Exception("Too Many Requests. Please try again later.");
                    }
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

                File.WriteAllBytes(PluginDir + Module, ModuleContent);
                File.WriteAllBytes(PluginDir + Plugin.File, PIni);
                Ini.SetConfig("Plugin", "Version", Plugin.LastVer, PluginDir + Plugin.File);
                
                if (File.Exists(PluginDir + Plugin.Old)) {
                    File.Delete(PluginDir + Plugin.Old);
                }

                if (!string.IsNullOrWhiteSpace(Plugin.Dependencies)) {
                    foreach (string Dependencie in Plugin.Dependencies.Split(';')) {
                        byte[] Data = DownloadData(RepoPath + Dependencie);
                        File.WriteAllBytes(PluginDir + Dependencie, Data);
                    }
                }
                error = null;
                return true;
            } catch  (Exception ex) {
                error = ex.Message;
                return false;
            }
        }

        internal static bool Uninstall(Plugin Plugin) {
            string PIni = PluginDir + Plugin.File;
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
        public string Name, Extensions, Type, LastVer, File, Old, Dependencies;
    }
}
