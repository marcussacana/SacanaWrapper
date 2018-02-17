//#define DebugPlugin
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SacanaWrapper
{
    public class Wrapper {
        string ImportPath = string.Empty;
        string ExportPath = string.Empty;
        private static string Lastest = string.Empty;
        DotNetVM Plugin;


        public string[] Import(string ScriptPath, bool PreventCorrupt = false, bool TryLastPluginFirst = false) {
            byte[] Script = File.ReadAllBytes(ScriptPath);
            string Extension = Path.GetExtension(ScriptPath);
            return Import(Script, Extension, PreventCorrupt, TryLastPluginFirst);
        }
        public string[] Import(byte[] Script, string Extension = null, bool PreventCorrupt = false, bool TryLastPluginFirst = false) {
            string[] Strings = null;
            string PluginDir = DotNetVM.AssemblyDirectory + "\\Plugins";

            if (File.Exists(Lastest) && TryLastPluginFirst) {
#if !DebugPlugin
                try {
                    string[] Result = TryImport(Lastest, Script);
                    if (Corrupted(Result) < Result.Length/2) {
                        return Result;
                    }
                } catch { }
#else
                string[] Result = TryImport(Lastest, Script);
                if (Corrupted(Result) < Result.Length / 2) {
                    return Result;
                }
#endif
            }

            Result[] Results;
            //Prepare Input Extension
            if (Extension != null && Extension.StartsWith(".")) {
                Extension = Extension.Substring(1, Extension.Length - 1);
            }
            if (Extension != null) {
                Extension = Extension.ToLower();

                Results = (from x in ListPlugins(PluginDir, Extension) select TestPlugin(x, Script)).ToArray();
                Results = (from x in Results where x.Compatible select x).ToArray();
                Strings = SelectBestPlugin(Results);

                if (Strings != null)
                    return Strings;
            }

            Results = (from x in ListPlugins(PluginDir) select TestPlugin(x, Script)).ToArray();
            Results = (from x in Results where x.Compatible select x).ToArray();
            Strings = SelectBestPlugin(Results);

            if (Strings == null)
                throw new Exception("Supported Plugin Not Found.");

            return Strings;
        }

        private string[] SelectBestPlugin(Result[] Plugins) {
            uint Min = uint.MaxValue;
            uint Best = uint.MaxValue;
            for (uint i = 0; i < Plugins.Length; i++) {
                if (Plugins[i].Error < Min) {
                    Best = i;
                    Min = Plugins[i].Error;
                }
            }

            if (Best == uint.MaxValue)
                return null;
            
            Result Sel = Plugins[Best];
            ImportPath = Sel.ImportPath;
            ExportPath = Sel.ExportPath;
            Lastest = Sel.Plugin;
            Plugin = Sel.Instance;

            return Sel.Content;
        }

        private Result TestPlugin(string Plugin, byte[] Script) {
            try {
                string[] Rst = TryImport(Plugin, Script);
                return new Result {
                    ImportPath = ImportPath,
                    ExportPath = ExportPath,
                    Content = Rst,
                    Error = Corrupted(Rst),
                    Plugin = Plugin,
                    Instance = this.Plugin,
                    Compatible = true
                };
            } catch {
                return new Result {
                    Compatible = false,
                    Plugin = Plugin,
                    Content = new string[0]
                };
            }
        }
        private string[] ListPlugins(string PluginDir, string Extension = null) {
            if (Extension == null)
                return GetFiles(PluginDir, "*.inf|*.ini|*.cfg");
            return (from x in GetFiles(PluginDir, "*.inf|*.ini|*.cfg") where GetExtensions(new string[] { x }).Contains(Extension.ToLower()) select x).ToArray();
        }
        
        private List<string> GetExtensions(string[] Plugins) {
            List<string> Exts = new List<string>();
            foreach (string Plugin in Plugins) {
                string PExt = Ini.GetConfig("Plugin", "Extensions;Extension;Ext;Exts;extensions;extension;ext;exts", Plugin, false);
                if (string.IsNullOrEmpty(PExt))
                    continue;
                foreach (string ext in PExt.ToLower().Split('|'))
                    Exts.Add(ext);
            }
            return Exts;
        }

        private uint Corrupted(string[] Strings) {
            if (Strings.Length == 0)
                return uint.MaxValue;

            char[] Corrupts = new char[] { '・' };

            uint Matchs = 0;
            foreach (string str in Strings) {
                if (str.Trim('\x0').Contains('\x0') || (from c in str.Trim('\x0') where (c & 0x7700) == 0x7700 || c < 10 || Corrupts.Contains(c) select c).Count() != 0)
                    Matchs++;
                else if (string.IsNullOrWhiteSpace(str) || string.IsNullOrWhiteSpace(str.Trim(str[0])))
                    Matchs++;
            }

            return Matchs;
        }

        public void Export(string[] Strings, string SaveAs) {
            byte[] Script = Export(Strings);
            File.WriteAllBytes(SaveAs, Script);
        }

        public byte[] Export(string[] Strings) {
            string[] Exp = ExportPath.Split('>');
            return (byte[])Plugin.Call(Exp[0], Exp[1], new object[] { Strings });
        }

        private string[] TryImport(string Plugin, byte[] Script) {
            ExportPath = Ini.GetConfig("Plugin", "Export;Exp;export;exp", Plugin, true);
            ImportPath = Ini.GetConfig("Plugin", "Import;Imp;import;imp", Plugin, true);
            string CustomSource = Ini.GetConfig("Plugin", "File;file;Archive;archive;Arc;arc", Plugin, false);

            string Path = System.IO.Path.GetDirectoryName(Plugin) + "\\",
             SourcePath = System.IO.Path.GetDirectoryName(Plugin) + "\\",
             SourcePath2 = System.IO.Path.GetDirectoryName(Plugin) + "\\";
             
            
            if (!string.IsNullOrWhiteSpace(CustomSource)){
                Path += CustomSource + ".dll";
                SourcePath += CustomSource + ".cs";
                SourcePath2 += CustomSource + ".vb";
            } else {
                Path += System.IO.Path.GetFileNameWithoutExtension(Plugin) + ".dll";
                SourcePath += System.IO.Path.GetFileNameWithoutExtension(Plugin) + ".cs";
                SourcePath2 += System.IO.Path.GetFileNameWithoutExtension(Plugin) + ".vb";
            }

            //Initialize Plugin
            bool InitializeWithScript = Ini.GetConfig("Plugin", "Initialize;InputOnCreate;initialize;inputoncreate", Plugin, false).ToLower() == "true";
            if (File.Exists(SourcePath))
                this.Plugin = new DotNetVM(File.ReadAllText(SourcePath, Encoding.UTF8), DotNetVM.Language.CSharp);
            else if (File.Exists(SourcePath2))
                this.Plugin = new DotNetVM(File.ReadAllText(SourcePath2, Encoding.UTF8), DotNetVM.Language.VisualBasic);
            else
                this.Plugin = new DotNetVM(File.ReadAllBytes(Path));

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

    internal struct Result {
        public string[] Content;
        public uint Error;
        public string ImportPath;
        public string ExportPath;
        public string Plugin;
        public DotNetVM Instance;
        public bool Compatible;
    }
}
