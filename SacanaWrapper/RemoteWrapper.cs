//#define DebugPlugin
using ImpromptuInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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

        public async static Task<string> EnumSupportedExtensions() {
            string Result = string.Empty;
            PluginInfo[] Plugins = await GetPlugins();
            foreach (var Plugin in Plugins) {
                if (string.IsNullOrWhiteSpace(Plugin.Extensions) || Plugin.Extensions.Trim(' ', '(', ')').ToLower() == "none")
                    continue;

                Result += Plugin.Extensions + "; ";
            }
            if (Result.EndsWith("; "))
                Result = Result.Substring(0, Result.Length - 2);
            return Result;
        }

        public async Task<string[]> Import(string ScriptPath, bool PreventCorrupt = false, bool TryLastPluginFirst = false) {
            byte[] Script = File.ReadAllBytes(ScriptPath);
            string Extension = Path.GetExtension(ScriptPath);
            return await Import(Script, Extension, PreventCorrupt, TryLastPluginFirst);
        }
        public async Task<string[]> Import(byte[] Script, string Extension = null, bool PreventCorrupt = false, bool TryLastPluginFirst = false) {
            string[] Strings = null;

            PluginInfo[] Plugins = await GetPlugins();

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
                    Strings = await TryImport(Lastest, Script);
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
                            Strings = await TryImport(Plugin, Script);
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
                    Strings = await TryImport(Plugin, Script);
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

        private async Task<string[]> TryImport(PluginInfo Plugin, byte[] Script) {
            if (string.IsNullOrWhiteSpace(Plugin.File))
                return null;

            if (!string.IsNullOrWhiteSpace(Plugin.Dependencies))
            {
                foreach (string Dependencie in Plugin.Dependencies.Split(';')) {
                    try
                    {
                        byte[] DepData = await Download(Dependencie);
                        System.Reflection.Assembly.Load(DepData);
                    }
                    catch { }
                }
            }

            var Handler = await GetPluginHandler(Plugin);
            this.Plugin = Handler.VM;

            ImportPath = Handler.ImportPath;
            ExportPath = Handler.ExportPath;
            bool InitializeWithScript = Handler.InitializeWithScript;

            //Import
            Lastest = Plugin;
            string[] Imp = ImportPath.Split('>');
            if (InitializeWithScript) {
                this.Plugin.StartInstance(Imp[0], Script);
                return (string[])this.Plugin.Call(Imp[0], Imp[1]);
            }
            return (string[])this.Plugin.Call(Imp[0], Imp[1], Script);

        }

        private async Task<PluginHandler> GetPluginHandler(PluginInfo Plugin)
        {
            bool Online = false;
            var VerName = Plugin.Name + " Version";
            var ModeName = Plugin.Name + " Mode";
            if (Cache.ContainsKey(VerName)) {
                Version LastVersion = new Version(Plugin.LastVer);
                Version LocalVersion = new Version(Encoding.UTF8.GetString(Cache[VerName]));
                if (LastVersion > LocalVersion)
                    Online = true;
            }

            byte[] INI = await Download(Plugin.File, Online);
            var Handler = new PluginHandler();
            Handler.ExportPath = Ini.GetConfig("Plugin", "Export;Exp;export;exp", INI, true);
            Handler.ImportPath = Ini.GetConfig("Plugin", "Import;Imp;import;imp", INI, true);
            string Source = Ini.GetConfig("Plugin", "File;file;Archive;archive;Arc;arc", INI, false);

            if (string.IsNullOrWhiteSpace(Source))
                Source = Path.GetFileNameWithoutExtension(Plugin.File);

            int Mode = 0;
            byte[] Data;
            if (!Online && Cache.ContainsKey(ModeName))
            {
                Mode = int.Parse(Encoding.UTF8.GetString(Cache[ModeName]));
                switch (Mode) {
                    case 0:
                        Data = await Download(Source + ".cs", Online);
                        break;
                    case 1:
                        Data = await Download(Source + ".vb", Online);
                        break;
                    case 2:
                        Data = await Download(Source + ".dll", Online);
                        break;
                    default:
                        throw new Exception("Invalid Mode");
                }
            }
            else
            {
                try
                {
                    Data = await Download(Source + ".cs", Online);
                }
                catch
                {
                    try
                    {
                        Mode = 1;
                        Data = await Download(Source + ".vb", Online);
                    }
                    catch
                    {
                        Mode = 2;
                        Data = await Download(Source + ".dll", Online);
                    }
                }
            }
#if DebugPlugin
            bool Debug = true;
#else 
            bool Debug = false;
#endif

            //Initialize Plugin
            Handler.InitializeWithScript = Ini.GetConfig("Plugin", "Initialize;InputOnCreate;initialize;inputoncreate", INI, false).ToLower() == "true";
            switch (Mode)
            {
                case 0:
                    Handler.VM = new DotNetVM(Encoding.UTF8.GetString(Data), DotNetVM.Language.CSharp, null, Debug);
                    break;
                case 1:
                    Handler.VM = new DotNetVM(Encoding.UTF8.GetString(Data), DotNetVM.Language.VisualBasic, null, Debug);
                    break;
                default:
                    Handler.VM = new DotNetVM(Data);
                    break;
            }

            Cache[VerName] = Encoding.UTF8.GetBytes(Plugin.LastVer);
            Cache[ModeName] = Encoding.UTF8.GetBytes(Mode.ToString());

            return Handler;
        }

        public async IAsyncEnumerable<IPluginCreator> GetAllPlugins(Action<string> ProgressChanged = null) {
            foreach (var Plugin in await GetPlugins()) {
                IPluginCreator Creator;

                try
                {
                    ProgressChanged?.Invoke(Plugin.Name);

                    var Handler = await GetPluginHandler(Plugin);
                    if (!Handler.InitializeWithScript)
                        continue;

                  Creator = new PluginCreator(Handler.VM, Handler.ImportPath.Split('>')[0], Plugin.Name, Plugin.Extensions);
                }
                catch { continue; }

                yield return Creator;
            }
        }
        public static bool CacheChanged = false;
        public static Dictionary<string, byte[]> Cache = new Dictionary<string, byte[]>();
        private async static Task<byte[]> Download(string Name, bool TryOnline = false)
        {
            string CacheName = "Cache_" + Name;
            if (Cache.ContainsKey(CacheName)){
                if (TryOnline)
                {
                    try
                    {
                        var OnlineResult = await HttpClient.GetByteArrayAsync(RemoteRepository + Name);
                        if (OnlineResult != null) {
                            if (Cache.ContainsKey(CacheName) && Cache[CacheName].Length == OnlineResult.Length)
                                return OnlineResult;
                            CacheChanged = true;
                            return Cache[CacheName] = OnlineResult;
                        }
                    }
                    catch { }
                }
                return Cache[CacheName];
            }

            CacheChanged = true;
            return Cache[CacheName] = await HttpClient.GetByteArrayAsync(RemoteRepository + Name);
        }

        public async static Task<PluginInfo[]> GetPlugins() {
            var Data = await Download(MapperFileName);

            uint PluginsCount = uint.Parse(Ini.GetConfig("Repo", "Count", Data, true));
            uint Version = uint.Parse(Ini.GetConfig("Repo", "Version", Data, true));
            if (Version > 3)
            {
                throw new Exception("The SacanaWrapper Is Outdated");
            }

            List<PluginInfo> Plugins = new List<PluginInfo>();
            for (int i = 0; i < PluginsCount; i++) {
                try {
                    Plugins.Add(new PluginInfo(Data, i));
                } catch { }
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

    struct PluginHandler {
        public DotNetVM VM;
        public string ImportPath;
        public string ExportPath;
        public bool InitializeWithScript;
    }

    class PluginCreator : IPluginCreator
    {
        DotNetVM VM;
        string Class;
        string _Name;
        string _Filter;
        public string Filter => _Filter;
        public string Name => _Name;
        public PluginCreator(DotNetVM VM, string Class, string Name, string Filter) {
            this.VM = VM;
            this.Class = Class;
            _Name = Name;
            _Filter = Filter;
        }

        public IPlugin Create(byte[] Script)
        {
            VM.StartInstance(Class, Script);
            return VM.Instance.ActLike<IPlugin>();
        }
    }
}
