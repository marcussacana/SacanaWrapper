using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SacanaWrapper
{
    public class Wrapper
    {
        string ImportPath = string.Empty;
        string ExportPath = string.Empty;
        string StrIP = string.Empty;
        string StrEP = string.Empty;
        private static string Lastest = string.Empty;
        HighLevelCodeProcessator Plugin;
        public string[] Import(byte[] Script, string Extension = null, bool PreventCorrupt = false) {
            string[] Strings = null;
            string PluginDir = HighLevelCodeProcessator.AssemblyDirectory + "\\Plugins";

            if (File.Exists(Lastest)) {
                try {
                    Strings = TryImport(Lastest, Script);
                    if (!ValidateResult(Strings))
                        return Strings;
                }
                catch { }
            }

            string[] Plugins = GetFiles(PluginDir, "*.inf|*.ini|*.cfg");


            //Prepare Input Extension
            if (Extension != null && Extension.StartsWith(".")) {
                Extension = Extension.Substring(1, Extension.Length - 1);
            }
            if (Extension != null)
                Extension = Extension.ToLower();


            //Initial Detection
            if (Extension != null) {
                foreach (string Plugin in Plugins) {
                    string PExt = Ini.GetConfig("Plugin", "Extensions", Plugin, false);
                    if (string.IsNullOrEmpty(PExt))
                        continue;
                    List<string> Exts = new List<string>(PExt.ToLower().Split('|'));
                    if (Exts.Contains(Extension))
                        try {
                            Strings = TryImport(Plugin, Script);
                            if (ValidateResult(Strings)) {
                                StrIP = ImportPath;
                                StrEP = ExportPath;
                                continue;
                            }
                            return Strings;
                        }
                        catch { }
                }
            }

            //Brute Detection
            foreach (string Plugin in Plugins) {
                try {
                    Strings = TryImport(Plugin, Script);
                    if (ValidateResult(Strings)) {
                        StrIP = ImportPath;
                        StrEP = ExportPath;
                        continue;
                    }
                    return Strings;
                }
                catch { }
            }
            if (Strings == null)
                throw new Exception("Supported Plugin Not Found.");

            if (ValidateResult(Strings) && PreventCorrupt)
                return new string[0];
            ImportPath = StrIP;
            ExportPath = StrEP;
            return Strings;
        }

        private bool ValidateResult(string[] Strings) {
            foreach (string str in Strings) {
                if (str.Contains('�') || str.Trim('\x0').Contains('\x0'))//If looks corrupted, try load with other plugin, if fail, return this content.
                    return true;
            }
            return false;
        }

        public byte[] Export(string[] Strings) {
            string[] Exp = ExportPath.Split('>');
            return (byte[])Plugin.Call(Exp[0], Exp[1], new object[] { Strings });
        }

        private string[] TryImport(string Plugin, byte[] Script) {
            ExportPath = Ini.GetConfig("Plugin", "Export", Plugin, true);
            ImportPath = Ini.GetConfig("Plugin", "Import", Plugin, true);
            string CustomSource = Ini.GetConfig("Plugin", "File", Plugin, false);

            string Path = System.IO.Path.GetDirectoryName(Plugin) + "\\",
             SourcePath = System.IO.Path.GetDirectoryName(Plugin) + "\\";
            
            if (!string.IsNullOrWhiteSpace(CustomSource)){
                Path += CustomSource + ".dll";
                SourcePath += CustomSource + ".cs";
            } else {
                Path += System.IO.Path.GetFileNameWithoutExtension(Plugin) + ".dll";
                SourcePath += System.IO.Path.GetFileNameWithoutExtension(Plugin) + ".cs";
            }

            //Initialize Plugin
            bool InitializeWithScript = Ini.GetConfig("Plugin", "Initialize", Plugin, true).ToLower() == "true";
            if (File.Exists(SourcePath))
                this.Plugin = new HighLevelCodeProcessator(File.ReadAllText(SourcePath, Encoding.UTF8));
            else
                this.Plugin = new HighLevelCodeProcessator(File.ReadAllBytes(Path));

            //Import
            Lastest = Plugin;
            string[] Imp = ImportPath.Split('>');
            if (InitializeWithScript) {
                this.Plugin.StartInstance(Imp[0], Script);
                return (string[])this.Plugin.Call(Imp[0], Imp[1]);
            }
            return (string[])this.Plugin.Call(Imp[0], Imp[1], Script);

        }

        private static string[] GetFiles(string Dir, string Search) {
            string[] Result = new string[0];
            foreach (string pattern in Search.Split('|'))
                Result = Result.Union(Directory.GetFiles(Dir, pattern)).ToArray();
            return Result;
        }
    }
}
