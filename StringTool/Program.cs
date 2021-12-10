using SacanaWrapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace StringTool
{
    class Program
    {

        internal static Font WordwrapFont => new Font(WordwrapSettings.FontName, WordwrapSettings.FontSize, WordwrapSettings.Bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);

        static WordwrapSettings? _WWSettings;
        internal static WordwrapSettings WordwrapSettings
        { 
            get {
                if (_WWSettings != null)
                    return _WWSettings.Value;

                if (!File.Exists("StringTool.ini")) {
                    using (var Reader = Assembly.GetExecutingAssembly().GetManifestResourceStream("StringTool.Settings.ini"))
                    using (var Writer = File.OpenWrite("StringTool.ini"))
                        Reader.CopyTo(Writer);
                }

                AdvancedIni.FastOpen(out WordwrapSettings Settings, "StringTool.ini");
                return (_WWSettings = Settings).Value;
            }
        }

        static void Main(string[] Args)
        {
            Console.Title = "StringTool - By Marcussacana";

            List<TaskInfo> Tasks = new List<TaskInfo>();
            for (int i = 0; i < Args.Length; i++) {
                string Arg = Args[i].Trim();

                string ParamA = i + 1 < Args.Length ? Args[i + 1] : null;
                string ParamB = i + 2 < Args.Length ? Args[i + 2] : null;
                string ParamC = i + 3 < Args.Length ? Args[i + 3] : null;

                bool IsFlag = Arg.StartsWith("\\") || Arg.StartsWith("/") || Arg.StartsWith("-");

                bool AIsFlag = ParamA == null || ParamA.StartsWith("\\") || ParamA.StartsWith("/") || ParamA.StartsWith("-");
                bool BIsFlag = ParamB == null || ParamB.StartsWith("\\") || ParamB.StartsWith("/") || ParamB.StartsWith("-");
                bool CIsFlag = ParamC == null || ParamC.StartsWith("\\") || ParamC.StartsWith("/") || ParamC.StartsWith("-");

                Arg = Arg.Trim('\\', '/', '-');

                ParamA = ParamA?.Trim('\\', '/', '-');
                ParamB = ParamB?.Trim('\\', '/', '-');
                ParamC = ParamC?.Trim('\\', '/', '-');

                if (IsFlag)
                {
                    switch (Arg.ToLower())
                    {
                        case "?":
                        case "help":
                            Console.WriteLine();
                            Console.WriteLine("Usage:");
                            Console.WriteLine("StringTool -Dump {InputScript} [OutputText]");
                            Console.WriteLine("StringTool -Insert {InputScript} [InputText] [OutputScript]");
                            Console.WriteLine("StringTool -Wordwrap {InputScript} [InputText] [OutputScript]");
                            break;
                        case "dump":
                            if (AIsFlag)
                                goto case "help";

                            if (BIsFlag)
                            {
                                if (File.Exists(ParamA))
                                {
                                    var Info = new TaskInfo(TaskType.Dump, ParamA);
                                    Tasks.Add(Info);
                                }
                                else if (Directory.Exists(ParamA))
                                {
                                    var Files = Directory.GetFiles(ParamA, "*.*", SearchOption.AllDirectories);
                                    foreach (var File in Files)
                                    {
                                        var Info = new TaskInfo(TaskType.Dump, File);
                                        Tasks.Add(Info);
                                    }
                                }
                                i++;
                                break;
                            }

                            i += 2;
                            if (File.Exists(ParamA))
                            {
                                var Info = new TaskInfo(TaskType.Dump, ParamA, null, ParamB);
                                Tasks.Add(Info);
                            }
                            else if (Directory.Exists(ParamA))
                            {
                                var Files = Directory.GetFiles(ParamA, "*.*", SearchOption.AllDirectories);
                                foreach (var File in Files)
                                {
                                    if (File.EndsWith(".dump.txt"))
                                        continue;

                                    var OutFile = Path.Combine(ParamB, Path.GetFileNameWithoutExtension(File) + ".dump.txt");
                                    var Info = new TaskInfo(TaskType.Dump, File, null, OutFile);
                                    Tasks.Add(Info);
                                }
                            }
                            break;
                        case "insert":
                            if (AIsFlag)
                                goto case "help";

                            if (BIsFlag)
                            {
                                if (File.Exists(ParamA))
                                {
                                    var Info = new TaskInfo(TaskType.Insert, ParamA);
                                    Tasks.Add(Info);
                                }
                                else if (Directory.Exists(ParamA))
                                {
                                    var Files = Directory.GetFiles(ParamA, "*.dump.txt", SearchOption.AllDirectories);
                                    if (Files.Length == 0)
                                        Files = Directory.GetFiles(ParamA, "*.txt", SearchOption.AllDirectories);

                                    foreach (var File in Files)
                                    {
                                        var Info = new TaskInfo(TaskType.Insert, File);
                                        Tasks.Add(Info);
                                    }
                                }
                                i++;
                                break;
                            }

                            if (CIsFlag)
                            {
                                if (File.Exists(ParamA) && File.Exists(ParamB))
                                {
                                    var Info = new TaskInfo(TaskType.Insert, ParamA, ParamB);
                                    Tasks.Add(Info);
                                }
                                i += 2;
                                break;
                            }

                            i += 3;
                            Tasks.Add(new TaskInfo(TaskType.Insert, ParamA, ParamB, ParamC));
                            break;
                        case "wordwrap":
                            if (AIsFlag)
                                goto case "help";

                            if (BIsFlag)
                            {
                                if (File.Exists(ParamA))
                                {
                                    Tasks.Add(new TaskInfo(TaskType.WordWrap, ParamA));
                                }
                                else
                                {
                                    var Files = Directory.GetFiles(ParamA, "*.*", SearchOption.AllDirectories);
                                    foreach (var File in Files)
                                    {
                                        if (File.EndsWith(".dump.txt"))
                                            continue;
                                        var Info = new TaskInfo(TaskType.WordWrap, File);
                                        Tasks.Add(Info);
                                    }
                                }
                                i++;
                                break;
                            }

                            if (CIsFlag)
                            {
                                if (File.Exists(ParamA))
                                {
                                    var Info = new TaskInfo(TaskType.WordWrap, ParamA, ParamB);
                                    Tasks.Add(Info);
                                }
                                else if (Directory.Exists(ParamA))
                                {
                                    var Files = Directory.GetFiles(ParamA, "*.*", SearchOption.AllDirectories);
                                    foreach (var File in Files)
                                    {
                                        if (File.EndsWith(".dump.txt"))
                                            continue;

                                        var TxtFile = Path.Combine(ParamB, Path.GetFileNameWithoutExtension(File) + ".dump.txt");
                                        var Info = new TaskInfo(TaskType.WordWrap, File, TxtFile);
                                        Tasks.Add(Info);
                                    }
                                }
                                i += 2;
                                break;
                            }

                            if (File.Exists(ParamA))
                            {
                                var Info = new TaskInfo(TaskType.WordWrap, ParamA, ParamB, ParamC);
                                Tasks.Add(Info);
                            }
                            else if (Directory.Exists(ParamA))
                            {
                                var Files = Directory.GetFiles(ParamA, "*.*", SearchOption.AllDirectories);
                                foreach (var File in Files)
                                {
                                    if (File.EndsWith(".dump.txt"))
                                        continue;

                                    var TxtFile = Path.Combine(ParamB, Path.GetFileNameWithoutExtension(File) + ".dump.txt");
                                    var OutFile = Path.Combine(ParamC, Path.GetFileNameWithoutExtension(File) + "_Wordwrap" + Path.GetExtension(File));
                                    var Info = new TaskInfo(TaskType.WordWrap, File, TxtFile, OutFile);
                                    Tasks.Add(Info);
                                }
                            }
                            i += 3;
                            break;
                        case "debug":
                        case "dbg":
                        case "test":
                            if (AIsFlag)
                                goto case "help";

                            i++;
                            if (File.Exists(ParamA))
                            {
                                Tasks.Add(new TaskInfo(TaskType.Debug, ParamA));
                            }
                            else
                            {
                                var Files = Directory.GetFiles(ParamA, "*.*", SearchOption.AllDirectories);
                                foreach (var File in Files)
                                {
                                    if (File.EndsWith(".dump.txt"))
                                        continue;

                                    var Info = new TaskInfo(TaskType.Debug, File);
                                    Tasks.Add(Info);
                                }
                            }
                            break;
                        default:
                            goto case "help";
                    }
                    continue;
                }
                else {
                    if (File.Exists(Arg))
                    {
                        Tasks.Add(new TaskInfo(Arg.EndsWith(".dump.txt") ? TaskType.Insert : TaskType.Dump, Arg));
                    }
                    else if (Directory.Exists(Arg)) {
                        foreach (var File in Directory.GetFiles(Arg, "*.*")) {
                            Tasks.Add(new TaskInfo(Arg.EndsWith(".dump.txt") ? TaskType.Insert : TaskType.Dump, File));
                        }
                    }
                }
            }

            Console.WriteLine("{0} Tasks to be Executed.", Tasks.Count);

            Wrapper Wrapper = new Wrapper();

            foreach (var Task in Tasks) {
                try
                {
                    switch (Task.Type)
                    {
                        case TaskType.Debug:
                            Console.WriteLine($"Debugging: {Path.GetFileName(Task.InputFile)}");
                            var Text = Wrapper.Import(Task.InputFile);
                            Wrapper.Export(Text, Task.InputFile);
                            break;
                        case TaskType.Dump:
                            Console.WriteLine($"Dumping: {Path.GetFileName(Task.InputFile)}");
                            var TXT = Wrapper.Import(Task.InputFile);
                            if (TXT == null || TXT.Length == 0)
                                break;
                            Escaper.Escape(TXT);
                            File.WriteAllLines(Task.OutputFile, TXT);
                            break;
                        case TaskType.Insert:
                            Console.WriteLine($"Inserting: {Path.GetFileName(Task.InputFile)}");
                            var OriText = Wrapper.Import(Task.InputFile);
                            var NewText = File.ReadAllLines(Task.InputText);
                            Escaper.Unescape(NewText);
                            for (int i = 0; i < OriText.Length && i < NewText.Length; i++)
                                OriText[i] = NewText[i];
                            Wrapper.Export(OriText, Task.OutputFile);
                            break;
                        case TaskType.WordWrap:
                            Console.WriteLine($"Processing: {Path.GetFileName(Task.InputFile)}");
                            var OriLines = Wrapper.Import(Task.InputFile);
                            var TlLines = File.ReadAllLines(Task.InputText);
                            Escaper.Unescape(TlLines);
                            for (int i = 0; i < OriLines.Length; i++)
                                if (TlLines[i] != OriLines[i])
                                    TlLines[i] = TlLines[i].WordWrap();
                            Wrapper.Export(TlLines, Task.OutputFile);
                            Escaper.Escape(TlLines);
                            File.WriteAllLines(Task.InputText, TlLines);
                            break;
                    }
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex.ToString());
                }
            }
        }
    }
}
