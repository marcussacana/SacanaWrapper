using SacanaWrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StringTool
{
    class Program
    {
        static Dictionary<char, char> EncodeMapping;
        static Dictionary<char, char> DecodeMapping;
        static void Main(string[] args)
        {
            Console.Title = "Marcussacana String Tool For Newbie/Lazy - The First and Last.";
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("-dump \"C:\\GameScript.bin\" \"C:\\ScriptStr.txt\"");
                Console.WriteLine("-insert \"C:\\GameScript.bin\" \"C:\\ScriptStr.txt\"  \"C:\\NewScript.bin\"");
                Console.WriteLine("-remap \"C:\\Chars.lst\" -dump/-insert ...");
                Console.WriteLine("-support");
                Console.WriteLine("Or just drag&drop");
                Console.ReadKey();
                return;
            }
            bool AllExist = true;
            foreach (string f in args)
                if (!File.Exists(f))
                    AllExist = false;
            if (AllExist)
            {
                foreach (string f in args)
                    Process(new string[] { f });
            }
            else
            {
                AllExist = true;
                foreach (string f in args)
                    if (!Directory.Exists(f))
                        AllExist = false;
                if (AllExist)
                {
                    foreach (string d in args)
                    {
                        foreach (string f in Directory.GetFiles(d, "*.*"))
                            Process(new string[] { f });
                    }
                }
                else
                    Process(args);
            }
        }

        private static void Process(string[] args)
        {
            string Input = string.Empty, Input2 = string.Empty, Output = string.Empty, Remap = string.Empty;
            try
            {
                int ParInd = 0;
                bool FileFound = false;
                bool DumpMode = true;
                bool CountMode = false;
                bool SupportMode = false;
                CheckArgs(args, ref ParInd, ref FileFound, ref Input, ref Input2, ref Output, ref Remap, ref DumpMode, ref CountMode, ref SupportMode);

                if (SupportMode)
                {
                    Console.WriteLine("Supported Script Extensions:");
                    Console.WriteLine(Wrapper.EnumSupportedExtensions());
                    return;
                }

                EncodeMapping = new Dictionary<char, char>();
                DecodeMapping = new Dictionary<char, char>();

                if (Remap != string.Empty && File.Exists(Remap))
                {
                    foreach (var Line in File.ReadAllLines(Remap))
                    {
                        var Pair = Line.Trim();
                        if (Pair.Length != 3)
                            continue;
                        if (Pair[1] != '=')
                            continue;

                        char Real = Pair.First();
                        char Fake = Pair.Last();

                        EncodeMapping[Fake] = Real;
                        DecodeMapping[Real] = Fake;
                    }
                }


                Wrapper Engine = new Wrapper();

                if (CountMode)
                {
                    Console.WriteLine("Script\tLines");
                    string[] Files;
                    if (FileFound)
                        Files = new string[] { Input };
                    else
                        Files = Directory.GetFiles(Input, "*.*");
                    string Log = "Script\t\tLines\r\n";
                    foreach (string File in Files)
                    {
                        string[] Strings = Engine.Import(File);
                        string Result = string.Format("{0}\t\t{1}", Path.GetFileName(File), Strings.LongCount());
                        Console.WriteLine(Result);
                        Log += Result + "\r\n";
                    }
                    if (Input.EndsWith("\\"))
                        Input = Input.Substring(0, Input.Length - 1); ;
                    string LogPath = Input + "-Count.log";
                    File.WriteAllText(LogPath, Log, Encoding.UTF8);
                    return;
                }

                if (Input == Output)
                    Output += ".new";
                if (Input2 == Output)
                    Output += ".new";
                Console.WriteLine("Processing File: {0}", Path.GetFileName(Input));
                if (DumpMode)
                {
                    string[] Txt = Engine.Import(File.ReadAllBytes(Input), Path.GetExtension(Input));
                    Encode(ref Txt, true);
                    Console.WriteLine("Loaded \"{0}\" Strings, Exporting to \"{1}\"", Txt.Length, Path.GetFileName(Output));
                    File.WriteAllLines(Output, Txt, Encoding.UTF8);
                }
                else
                {
                    string[] Txt = File.ReadAllLines(Input2, Encoding.UTF8);
                    Encode(ref Txt, false);
                    string[] Imp = Engine.Import(File.ReadAllBytes(Input), Path.GetExtension(Input));
                    Console.WriteLine("Original have {0} Lines, New have {1} Lines...", Imp.Length, Txt.Length);
                    if (Txt.Length != Imp.Length)
                    {
                        throw new Exception("Input line count don't match with script string count.");
                    }
                    byte[] OutData = Engine.Export(Txt);
                    File.WriteAllBytes(Output, OutData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to Process: {0}", Path.GetFileName(Input));
                Console.WriteLine("EX: {0}" + ex.ToString());
            }
        }

        private static void Encode(ref string[] Strings, bool Enable)
        {
            for (int i = 0; i < Strings.Length; i++)
                Encode(ref Strings[i], Enable);
        }

        private static void Encode(ref string String, bool Enable)
        {
            if (Enable)
            {
                string Result = string.Empty;
                foreach (char c in String)
                {
                    if (c == '\n')
                        Result += "\\n";
                    else if (c == '\\')
                        Result += "\\\\";
                    else if (c == '\t')
                        Result += "\\t";
                    else if (c == '\r')
                        Result += "\\r";
                    else
                        Result += EncodeMapping.ContainsKey(c) ? EncodeMapping[c] : c;
                }
                String = Result;
            }
            else
            {
                string Result = string.Empty;
                bool Special = false;
                foreach (char c in String)
                {
                    if (c == '\\' & !Special)
                    {
                        Special = true;
                        continue;
                    }
                    if (Special)
                    {
                        switch (c.ToString().ToLower()[0])
                        {
                            case '\\':
                                Result += '\\';
                                break;
                            case 'n':
                                Result += '\n';
                                break;
                            case 't':
                                Result += '\t';
                                break;
                            case 'r':
                                Result += '\r';
                                break;
                            default:
                                throw new Exception("\\" + c + " Isn't a valid string escape.");
                        }
                        Special = false;
                    }
                    else
                        Result += DecodeMapping.ContainsKey(c) ? DecodeMapping[c] : c;
                }
                String = Result;
            }
        }

        private static void CheckArgs(string[] args, ref int ParInd, ref bool FileFound, ref string Input, ref string Input2, ref string Output, ref string Remap, ref bool DumpMode, ref bool CountMode, ref bool SupportMode)
        {
            bool InRemap = false;
            CountMode = false;
            foreach (string Arg in args)
            {
                if (Arg.StartsWith("-") || Arg.StartsWith("\\") || Arg.StartsWith("//"))
                {
                    switch (Arg.ToLower().Trim(' ', '\\', '/', '-'))
                    {
                        case "remap":
                            InRemap = true;
                            break;
                        case "dump":
                            DumpMode = true;
                            break;
                        case "insert":
                            DumpMode = false;
                            break;
                        case "count":
                            CountMode = true;
                            break;
                        case "support":
                            SupportMode = true;
                            return;
                        default:
                            throw new Exception("\"{0}\" Isn't a valid paramter.");
                    }
                    continue;
                }

                if (InRemap)
                {
                    InRemap = false;
                    Remap = Arg;
                    continue;
                }

                string ArgFN = Arg, ArgFNWE = ArgFN, ArgDir = Arg;
                if (ArgFN.Contains(":"))
                    ArgFN = ArgFNWE = Path.GetFileName(ArgFN);
                if (ArgFNWE.Contains("."))
                {
                    ArgFNWE = Path.GetFileNameWithoutExtension("C:\\" + ArgFN);
                }
                ArgDir = (!ArgDir.Contains(":") ? AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') : Path.GetDirectoryName(ArgDir));


                if (FileFound)
                {
                    switch (ParInd++)
                    {
                        case 1:
                            Output = Arg;
                            break;
                        case 2:
                            Input2 = Output;
                            Output = Arg;
                            break;
                    }
                }
                else if (File.Exists(Arg))
                {
                    FileFound = true;
                    if (Arg.Trim().ToLower().EndsWith(".txt"))
                    {
                        string[] Files = Directory.GetFiles(ArgDir, ArgFNWE + ".*");

                        if (Files.Length <= 1 && ArgFNWE.ToLower().EndsWith("-dump"))
                        {
                            ArgFNWE = ArgFNWE.Substring(0, ArgFNWE.Length - "-dump".Length);
                            Files = Directory.GetFiles(ArgDir, ArgFNWE + ".*");
                        }


                        foreach (string f in Files)
                        {
                            if (Path.GetFileName(f).ToLower() == Path.GetFileName(Arg).ToLower())
                                continue;

                            string ext = ".";
                            if (f.Contains("."))
                                ext = Path.GetExtension(f);

                            DumpMode = false;
                            Input = f;
                            Input2 = Arg;
                            Output = ArgDir + "\\" + ArgFNWE + "_New" + ext;
                        }

                        if (DumpMode)
                        {
                            Output = ArgDir + "\\" + ArgFNWE + "-Dump.txt";
                        }
                    }
                    else
                    {
                        Input = ArgDir + "\\" + ArgFN;
                        Output = ArgDir + "\\" + ArgFNWE + ".txt";
                    }

                    ParInd++;
                    if (DumpMode || CountMode)
                        Input = Arg;
                }
                else if (Directory.Exists(Arg))
                {
                    FileFound = false;
                    Input = Arg;
                }
            }
        }
    }
}