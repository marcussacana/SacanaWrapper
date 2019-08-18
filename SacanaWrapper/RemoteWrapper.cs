//#define DebugPlugin
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace SacanaWrapper
{
    public class RemoteWrapper
    {
        public static string MapperFileName = "Updater.ini";
        public static string RemoteRepository = "https://raw.githubusercontent.com/marcussacana/SacanaWrapper/updater/Updater/";
        public static HttpClient HttpClient;
        string ImportPath = string.Empty;
        string ExportPath = string.Empty;
        string StrIP = string.Empty;
        string StrEP = string.Empty;
        private static PluginInfo Lastest;
        private static string LastExt = string.Empty;
        DotNetVM Plugin;

        public static string EnumSupportedExtensions() {
            string Result = string.Empty;
            PluginInfo[] Plugins = GetPlugins();
            foreach (var Plugin in Plugins) {
                if (string.IsNullOrWhiteSpace(Plugin.Extensions) || Plugin.Extensions.Trim(' ', '(', ')').ToLower() == "none")
                    continue;

                Result += Plugin.Extensions + "; ";
            }
            if (Result.EndsWith("; "))
                Result = Result.Substring(0, Result.Length - 2);
            return Result;
        }

        public string[] Import(string ScriptPath, bool PreventCorrupt = false, bool TryLastPluginFirst = false) {
            byte[] Script = File.ReadAllBytes(ScriptPath);
            string Extension = Path.GetExtension(ScriptPath);
            return Import(Script, Extension, PreventCorrupt, TryLastPluginFirst);
        }
        public string[] Import(byte[] Script, string Extension = null, bool PreventCorrupt = false, bool TryLastPluginFirst = false) {
            string[] Strings = null;

            PluginInfo[] Plugins = GetPlugins();

            List<string> Extensions = GetExtensions(Plugins);

            //Prepare Input Extension
            if (Extension != null && Extension.StartsWith(".")) {
                Extension = Extension.Substring(1, Extension.Length - 1);
            }
            if (Extension != null)
                Extension = Extension.ToLower();

            if (Extension == LastExt && TryLastPluginFirst) {
#if !DebugPlugin
                try {
#endif
                    Strings = TryImport(Lastest, Script);
                    if (!Corrupted(Strings))
                        return Strings;
#if !DebugPlugin
                } catch { }
#endif
            }

            LastExt = Extension;


            //Initial Detection
            if (Extension != null && CountMatch(Extensions, Extension) > 0) {
                uint Fails = 0;
                foreach (var Plugin in Plugins) {
                    List<string> Exts = GetExtensions(Plugin);
                    if (Exts.Contains(Extension)) {
#if !DebugPlugin
                        try {
#endif
                            Strings = TryImport(Plugin, Script);
                            if (Corrupted(Strings) && ++Fails < CountMatch(Extensions, Extension)) {
                                StrIP = ImportPath;
                                StrEP = ExportPath;
                                continue;
                            }
                            return Strings;
#if !DebugPlugin
                    } catch { }
#endif
                }
                }
            }

            //Brute Detection
            foreach (var Plugin in Plugins) {
#if !DebugPlugin
                try {
#endif
                    Strings = TryImport(Plugin, Script);
                    if (Corrupted(Strings)) {
                        StrIP = ImportPath;
                        StrEP = ExportPath;
                        continue;
                    }
                    return Strings;
#if !DebugPlugin
                } catch { }
#endif
            }
            if (Strings == null)
                throw new Exception("Supported Plugin Not Found.");

            if (Corrupted(Strings) && PreventCorrupt)
                return new string[0];
            ImportPath = StrIP;
            ExportPath = StrEP;
            return Strings;
        }

        private uint CountMatch(List<string> Strings, string Pattern) {
            return (uint)(from x in Strings where x == Pattern select x).LongCount(); ;
        }

        private List<string> GetExtensions(PluginInfo Plugin) => GetExtensions(new PluginInfo[] { Plugin });
        private List<string> GetExtensions(PluginInfo[] Plugins) {
            List<string> Exts = new List<string>();
            foreach (var Plugin in Plugins) {
                if (string.IsNullOrWhiteSpace(Plugin.Extensions) || Plugin.Extensions.Trim(' ', '(', ')').ToLower() == "none")
                    continue;

                foreach (string Ext in Plugin.Extensions.ToLower().Split(';'))
                    Exts.Add(Ext.Trim(' ', '*', '.'));
            }
            return Exts;
        }

        private bool Corrupted(string[] Strings) {
            if (Strings.Length == 0)
                return true;

            char[] Corrupts = new char[] { '・' };

            uint Matchs = 0;
            foreach (string str in Strings) {
                if (str.Trim('\x0').Contains('\x0') || (from c in str.Trim('\x0') where (c & 0x7700) == 0x7700 || c < 10 || Corrupts.Contains(c) select c).Count() != 0)
                    Matchs++;
				else if (string.IsNullOrWhiteSpace(str))
					Matchs++;
            }

            if (Matchs > Strings.Length / 2)
                return true;

            return false;
        }

        public void Export(string[] Strings, string SaveAs) {
            byte[] Script = Export(Strings);
            File.WriteAllBytes(SaveAs, Script);
        }

        public byte[] Export(string[] Strings) {
            string[] Exp = ExportPath.Split('>');
            return (byte[])Plugin.Call(Exp[0], Exp[1], new object[] { Strings });
        }

        private string[] TryImport(PluginInfo Plugin, byte[] Script) {
            if (string.IsNullOrWhiteSpace(Plugin.File))
                return null;

            if (!string.IsNullOrWhiteSpace(Plugin.Dependencies))
            {
                foreach (string Dependencie in Plugin.Dependencies.Split(';')) {
                    try
                    {
                        byte[] DepData = Download(Dependencie);
                        System.Reflection.Assembly.Load(DepData);
                    }
                    catch { }
                }
            }

            byte[] INI = Download(Plugin.File);
            ExportPath = Ini.GetConfig("Plugin", "Export;Exp;export;exp", INI, true);
            ImportPath = Ini.GetConfig("Plugin", "Import;Imp;import;imp", INI, true);
            string CustomSource = Ini.GetConfig("Plugin", "File;file;Archive;archive;Arc;arc", INI, false);

            int Mode = 0;
            byte[] Data;
            try
            {
                Data = Download(CustomSource + ".cs");
            }
            catch {
                try
                {
                    Mode = 1;
                    Data = Download(CustomSource + ".vb");
                }
                catch {
                    Mode = 2;
                    Data = Download(CustomSource + ".dll");
                }
            }

#if DebugPlugin
            bool Debug = true;
#else 
            bool Debug = false;
#endif

            //Initialize Plugin
            bool InitializeWithScript = Ini.GetConfig("Plugin", "Initialize;InputOnCreate;initialize;inputoncreate", INI, false).ToLower() == "true";
            switch (Mode) {
                case 0:
                    this.Plugin = new DotNetVM(Encoding.UTF8.GetString(Data), DotNetVM.Language.CSharp, null, Debug);
                    break;
                case 1:
                    this.Plugin = new DotNetVM(Encoding.UTF8.GetString(Data), DotNetVM.Language.VisualBasic, null, Debug);
                    break;
                default:
                    this.Plugin = new DotNetVM(Data);
                    break;
            }

            //Import
            Lastest = Plugin;
            string[] Imp = ImportPath.Split('>');
            if (InitializeWithScript) {
                this.Plugin.StartInstance(Imp[0], Script);
                return (string[])this.Plugin.Call(Imp[0], Imp[1]);
            }
            return (string[])this.Plugin.Call(Imp[0], Imp[1], Script);

        }

        private static byte[] Download(string Name) => HttpClient.GetByteArrayAsync(RemoteRepository + Name).GetAwaiter().GetResult();
        

        public static PluginInfo[] GetPlugins() {
            var Data = Download(MapperFileName);

            uint PluginsCount = uint.Parse(Ini.GetConfig("Repo", "Count", Data, true));
            uint Version = uint.Parse(Ini.GetConfig("Repo", "Version", Data, true));
            if (Version > 2)
            {
                throw new Exception("The SacanaWrapper Is Outdated");
            }

            List<PluginInfo> Plugins = new List<PluginInfo>();
            for (int i = 0; i < PluginsCount; i++) {
                Plugins.Add(new PluginInfo(Data, i));
            }

            return Plugins.ToArray();
        }
    }

    public struct PluginInfo : IEquatable<PluginInfo>
    {
        internal PluginInfo(byte[] Data, int ID)
        {
            Type = Ini.GetConfig("Plugin." + ID, "Mode;Modes", Data, true);

            if (Type.ToUpper().Contains("W") && Type.ToUpper().Contains("R"))
                Type = "Read/Write";
            else if (Type.Contains("R"))
                Type = "Read Only";
            else if (Type.Contains("W"))
                Type = "Write Only";

            Name = Ini.GetConfig("Plugin." + ID, "Name;Title", Data, true);
            Extensions = Ini.GetConfig("Plugin." + ID, "Extension;Formats", Data, true);
            LastVer = Ini.GetConfig("Plugin." + ID, "Build;Version", Data, true);
            File = Ini.GetConfig("Plugin." + ID, "File;Path", Data, true);
            Dependencies = Ini.GetConfig("Plugin." + ID, "Dependencies;References", Data, false);
            Old = Ini.GetConfig("Plugin." + ID, "Old;Obsolete", Data, false);
        }

        public string Name, Extensions, Type, LastVer, File, Old, Dependencies;

        public bool Equals(PluginInfo Other)
        {
            return Name == Other.Name && File == Other.File;
        }
    }

}
