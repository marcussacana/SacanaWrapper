#IMPORT System.Core.dll
#IMPORT System.Linq.dll
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public class Filter {
    string[] Lines;
    Encoding Encoder = Encoding.UTF8;
    public Filter(byte[] Script) {
        string Text = Encoder.GetString(Script);
        Text = Text.Replace("\r\n", "\n");
        Lines = Text.Split('\n');
    }

    Dictionary<int, int> IndexMap;
    Dictionary<int, int> CountMap;
    Dictionary<int, string> MaskMap;
    public string[] Import()
    {
        CountMap = new Dictionary<int, int>();
        IndexMap = new Dictionary<int, int>();
        MaskMap = new Dictionary<int, string>();
        List<string> Output = new List<string>();
        for (int i = 0; i < Lines.Length; i++) {
            string Line = Lines[i].Trim(' ', '\t');

            if (!Line.Contains("\"") /*|| Line.StartsWith("$") */|| Line.StartsWith("#"))
                continue;

            IndexMap.Add(Output.Count, i);
            Output.AddRange(Dump(Output.Count, Lines[i]));
        }
        return Output.ToArray();
    }

    public byte[] Export(string[] Strings) {
        for (int i = 0; i < Strings.Length; ) {
            int Count = CountMap[i];

            List<object> Strs = new List<object>();
            foreach (string Str in Strings.Skip(i).Take(Count)) {
                string Tmp = Str;
                Encode(ref Tmp, true);
                Strs.Add(Tmp);
            }
            Lines[IndexMap[i]] = string.Format(MaskMap[i], Strs.ToArray());
            i += Count;
        }

        StringBuilder Builder = new StringBuilder();
        foreach (string Line in Lines)
            Builder.AppendLine(Line);

        return Encoder.GetBytes(Builder.ToString());
    }

    private string[] Dump(int Index, string Line) {
        string Mask = "";
        List<string> Texts = new List<string>();
        bool InStr = false;
        bool InEsc = false;
        int ID = 0;
        foreach (char c in Line)
        {
            if (!InStr) {
                if (c == '{' || c == '}')
                    Mask += c;
                Mask += c;
                if (c == '"') {
                    InStr = true;
                    Texts.Add(string.Empty);
                }
                continue;
            }
            if (!InEsc) {
                if (c == '"') {
                    InStr = false;
                    Mask += "{" + ID + "}\"";
                    ID++;
                    continue;
                }
                if (c != '\\')
                    Texts[Texts.Count - 1] += c;
                else
                    InEsc = true;
                continue;
            }

            InEsc = false;
            string Append = string.Empty;
            switch (c.ToString().ToLower().First()){
                case 'n':
                    Append = "\\n"; 
                    break;
                case 'r':
                    Append = "\\r";
                    break;
                case 't':
                    Append = "\\t";
                    break;
                case '\\':
                    Append = "\\\\";
                    break;
                case '\'':
                    Append = "\\'";
                    break;
                case '"':
                    Append = "\\\"";
                    break;
				case '%':
					Append = "\\%";
					break;
                default:
                    throw new Exception("\\" + c + " Isn't a valid string escape.");
                    break; 
            }
            Texts[Texts.Count - 1] += Append;
        }
        this.CountMap[Index] = Texts.Count;
        Console.WriteLine("DBG: " + Mask);
        this.MaskMap[Index] = Mask;

        for (int i = 0; i < Texts.Count; i++) {
            string tmp = Texts[i];
            Encode(ref tmp, false);
            Texts[i] = tmp;
        }

        return Texts.ToArray();
    }

    private void Encode(ref string String, bool Enable) {
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
                //else if (c == '\'')
                //    Result += "\\'";
                else if (c == '"')
                    Result += "\\\"";
                else if (c == '%')
					Result += "\\%";
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
                        case '\'':
                            Result += '\'';
                            break;
                        case '"':
                            Result += '"';
                            break;
						case '%':
							Result += '%';
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
}