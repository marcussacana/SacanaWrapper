using SacanaWrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StringTool {
    class Program {
        static void Main(string[] args) {
            Console.Title = "Marcussacana String Tool For Newbie/Lazy - The First and Last.";
            if (args == null || args.Length == 0) {
                Console.WriteLine("Usage:");
                Console.WriteLine("-dump \"C:\\GameScript.bin\" \"C:\\ScriptStr.txt\"");
                Console.WriteLine("-insert \"C:\\GameScript.bin\" \"C:\\ScriptStr.txt\"  \"C:\\NewScript.bin\"");
                Console.WriteLine("Or just drag&drop");
                Console.ReadKey();
                return;
            }
            bool AllExist = true;
            foreach (string f in args)
                if (!File.Exists(f))
                    AllExist = false;
            if (AllExist) {
                foreach (string f in args)
                    Process(new string[] { f });
            } else {
                AllExist = true;
                foreach (string f in args)
                    if (!Directory.Exists(f))
                        AllExist = false;
                if (AllExist) {
                    foreach (string d in args) {
                        foreach (string f in Directory.GetFiles(d, "*.*"))
                            Process(new string[] { f });
                    }
                } else
                    Process(args);
            }
        }

        private static void Process(string[] args) {
            string Input = string.Empty, Input2 = string.Empty, Output = string.Empty;
            try {
                int ParInd = 0;
                bool FileFound = false;
                bool DumpMode = true;
                CheckArgs(args, ref ParInd, ref FileFound, ref Input, ref Input2, ref Output, ref DumpMode);
                if (Input == Output)
                    Output += ".new";
                if (Input2 == Output)
                    Output += ".new";
                Console.WriteLine("Processing File: {0}", Path.GetFileName(Input));
                Wrapper Engine = new Wrapper();
                if (DumpMode) {
                    string[] Txt = Engine.Import(File.ReadAllBytes(Input));
                    Encode(ref Txt, true);
                    Console.WriteLine("Loaded \"{0}\" Strings, Exporting to \"{1}\"", Txt.Length, Path.GetFileName(Output));
                    File.WriteAllLines(Output, Txt, Encoding.UTF8);
                } else {
                    string[] Txt = File.ReadAllLines(Input2, Encoding.UTF8);
                    Encode(ref Txt, false);
                    string[] Imp = Engine.Import(File.ReadAllBytes(Input));
                    Console.WriteLine("Original have {0} Lines, New have {1} Lines...", Imp.Length, Txt.Length);
                    if (Txt.Length != Imp.Length) {
                        throw new Exception("Input line count don't match with script string count.");
                    }
                    File.WriteAllBytes(Output, Engine.Export(Txt));
                }
            }
            catch {
                Console.WriteLine("Failed to Process: {0}", Path.GetFileName(Input));
            }
        }

        private static void Encode(ref string[] Strings, bool Enable) {
            for (int i = 0; i < Strings.Length; i++)
                Encode(ref Strings[i], Enable);
        }

        private static void Encode(ref string String, bool Enable) {
            if (Enable) {
                string Result = string.Empty;
                foreach (char c in String) {
                    if (c == '\n')
                        Result += "\\n";
                    else if (c == '\\')
                        Result += "\\\\";
                    else if (c == '\t')
                        Result += "\\t";
                    else if (c == '\r')
                        Result += "\\r";
                    else
                        Result += c;
                }
                String = Result;
            } else {
                string Result = string.Empty;
                bool Special = false;
                foreach (char c in String) {
                    if (c == '\\' & !Special) {
                        Special = true;
                        continue;
                    }
                    if (Special) {
                        switch (c.ToString().ToLower()[0]) {
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
                    } else
                        Result += c;
                }
                String = Result;
            }
        }

        private static void CheckArgs(string[] args, ref int ParInd, ref bool FileFound, ref string Input, ref string Input2, ref string Output, ref bool DumpMode) {
            foreach (string Arg in args) {
                if (Arg.StartsWith("-") || Arg.StartsWith("\\") || Arg.StartsWith("//")) {
                    switch (Arg.ToLower().Trim()) {
                        case "dump":
                            DumpMode = true;
                            break;
                        case "insert":
                            DumpMode = false;
                            break;
                        default:
                            throw new Exception("\"{0}\" Isn't a valid paramter.");
                    }
                    continue;
                }
                if (FileFound) {
                    switch (ParInd++) {
                        case 1:
                            Output = Arg;
                            break;
                        case 2:
                            Input2 = Output;
                            Output = Arg;
                            break;
                    }
                } else if (System.IO.File.Exists(Arg)) {
                    FileFound = true;
                    if (Arg.Trim().ToLower().EndsWith(".txt")) {
                        DumpMode = false;
                        string[] Files = Directory.GetFiles(Path.GetDirectoryName(Arg), Path.GetFileNameWithoutExtension(Arg) + ".*");
                        foreach (string f in Files) {
                            if (!f.Trim().ToLower().EndsWith(".txt")) {
                                Input = f;
                                Input2 = Arg;
                                Output = Path.GetDirectoryName(Arg) + "\\" + Path.GetFileNameWithoutExtension(Arg) + "_New" + Path.GetExtension(f);
                            }
                        }
                    } else
                        Output = Path.GetDirectoryName(Arg) + "\\" + Path.GetFileNameWithoutExtension(Arg) + ".txt";
                    ParInd++;
                    if (DumpMode)
                        Input = Arg;
                }
            }
        }
    }
}
