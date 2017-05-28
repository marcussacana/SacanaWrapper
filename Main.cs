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
        string Lastest = string.Empty;
        HighLevelCodeProcessator Plugin;
        public string[] Import(byte[] Script) {
            string PluginDir = HighLevelCodeProcessator.AssemblyDirectory + "\\Plugins";
            if (File.Exists(Lastest)) {
                try {
                    return TryImport(Lastest, Script);
                } catch { }
            }
            string[] Plugins = GetFiles(PluginDir, "*.inf|*.ini|*.cfg");
            foreach (string Plugin in Plugins) {
                try {
                    return TryImport(Plugin, Script);
                }
                catch { }
            }
            throw new Exception("Supported Plugin Not Found.");
        }

        public byte[] Export(string[] Strings) {
            string[] Exp = ExportPath.Split('>');
            return (byte[])Plugin.Call(Exp[0], Exp[1], new object[] { Strings });
        }

        private string[] TryImport(string Plugin, byte[] Script) {
            ImportPath = Ini.GetConfig("Plugin", "Import", Plugin, true);
            ExportPath = Ini.GetConfig("Plugin", "Export", Plugin, true);
            string CustomSource = Ini.GetConfig("Plugin", "File", Plugin, false);
            string Path = System.IO.Path.GetDirectoryName(Plugin) + "\\", SourcePath = System.IO.Path.GetDirectoryName(Plugin) + "\\";
            if (!string.IsNullOrWhiteSpace(CustomSource)){
                Path += CustomSource + ".dll";
                SourcePath += CustomSource + ".cs";
            } else {
                Path += System.IO.Path.GetFileNameWithoutExtension(Plugin) + ".dll";
                SourcePath += System.IO.Path.GetFileNameWithoutExtension(Plugin) + ".cs";
            }
            bool InitializeWithScript = Ini.GetConfig("Plugin", "Initialize", Plugin, true).ToLower() == "true";
            if (File.Exists(SourcePath))
                this.Plugin = new HighLevelCodeProcessator(File.ReadAllText(SourcePath, Encoding.UTF8));
            else
                this.Plugin = new HighLevelCodeProcessator(File.ReadAllBytes(Path));
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
