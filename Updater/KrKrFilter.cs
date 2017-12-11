#IMPORT System.Linq.dll
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KrKrFilter {
    public class KSFilter {
		/*
		KiriKiri Kag Script Filter - By Marcussacana
		This will try filter all text from a ks script, but keep in mind
		the KS have a dynamic format, this filter don't work to any game.
		*/
		
		//Change the Encoding Here, Eco = Read, Eco2 = Write
		//Sample Encoding:
		//Encoding.UTF8;
		//Encoding.GetEncoding(932); // SJIS
		//Encoding.Unicode; //UTF16
		//Encoding.Default; //Operating System Default Encoding
        private Encoding Eco = Encoding.Unicode;
        private Encoding Eco2 = Encoding.Unicode;
        
		private string[] Lines = new string[0];
        private Dictionary<uint, string> Prefix = new Dictionary<uint, string>();
        private Dictionary<uint, string> Sufix = new Dictionary<uint, string>();
        private Dictionary<uint, string> Prefix2 = new Dictionary<uint, string>();
        private Dictionary<uint, string> Sufix2 = new Dictionary<uint, string>();

        public KSFilter(byte[] Script) {
            string Decoded = Eco.GetString(Script);
            Decoded = Decoded.Replace("\r\n", "\n");
            Lines = Decoded.Split('\n');
        }

        public string[] Import() {
            uint ID = 0, ID2 = 0;
            List<string> Dialogues = new List<string>();
            bool InScript = false;
            foreach (string Line in Lines) {
                if (Line.Trim() == "[iscript]" || Line.Trim() == "@iscript")
                    InScript = true;
                if (Line.Trim() == "[endscript]" || Line.Trim() == "@endscript")
                    InScript = false;
                if (InScript)
                    continue;

                if (IsString(Line)) {
                    Dialogues.Add(LineWork(true, ID++, Line).Replace("[r]", "\n"));
                }
                if (ContainsTextOnTag(Line)) {
                    Dialogues.Add(TextOnTagWork(true, ID2++, Line));
                }
            }
            return Dialogues.ToArray();
        }

        public byte[] Export(string[] Content) {
            string Result = "";
            bool InScript = false;
            for (uint i = 0, x = 0, y = 0, z = 0; i < Lines.Length; i++) {
                string Line = Lines[i];
                if (Line.Trim() == "[iscript]")
                    InScript = true;
                if (Line.Trim() == "[endscript]")
                    InScript = false;

                if (IsString(Line) && !InScript) {
                    string Input = LineWork(false, z++, Content[x++].Replace("\n", "[r]")).Replace("\n", "\r\n");

                    if (ContainsTextOnTag(Lines[i])) {
                        TextOnTagWork(true, y, Input);
                        Input = TextOnTagWork(false, y++, Content[x++]);
                    }

                    Result += Input;
                } else if (ContainsTextOnTag(Lines[i]) && !InScript) {
                    Result += TextOnTagWork(false, y++, Content[x++]);
                } else {
                    Result += Lines[i];
                }
                Result += "\r\n";
            }
            return Eco2.GetBytes(Result);
        }

        string[] TTag = new string[] { " text=\"", " t=\"", " char=\"", " actor=\"", " txt=\"" };
        private bool ContainsTextOnTag(string Line) {
			if (Line == string.Empty)
				return false;
			/*if (Line[0] == '@')
				return false;*/
            foreach (string T in TTag) {
                if (Line.Contains(T)) {
                    return true;
                }
            }
            return false;
        }

        private string TextOnTagWork(bool Mode, uint ID, string Line) {
            if (Mode) {
                foreach (string T in TTag) {
                    if (Line.Contains(T)) {
                        int BI = Line.IndexOf(T) + T.Length;
                        int Len = Line.IndexOf("\"", BI) - BI;
                        Prefix2[ID] = Line.Substring(0, BI);
                        Sufix2[ID] = Line.Substring(BI + Len, Line.Length - (BI + Len));
                        return Line.Substring(BI, Len);
                    }
                }
            } else {
                foreach (string T in TTag) {
                    if (Prefix2[ID].Contains(T)) {
                        return Prefix2[ID] + Line + Sufix2[ID];
                    }

                }
            }
            return Line;
        }


        private string LineWork(bool Mode, uint ID, string Line, bool While = false) {
            string[] Tags = new string[]
            { ";", " ", "[cm]", "[hr]", "[wt]", "[line1]", "[line2]", "[line3]", "[line4]", "[line5]", "[line6]",
            "[line7]", "[line8]", "[line9]", "[r]", "[l]" , "\\", "[SYSTEM_MENU_ON_OPENING]",
            "[SYSTEM_MENU_ON]", "[FUNC_LOAD_PLUGIN]", "[plc]", "[style pitch=-1]", "[resetstyle]",
            "―", "　", "[endlink]", "[np]" };

            string ResultLine = Line;
            if (Mode) {
				if (ResultLine.Contains("[") && ResultLine.Contains("]")){
					string New = string.Empty;
					string Tag = string.Empty;
					bool InTag = false;
					bool IsFurigana = false;
					for (int i = 0; i < ResultLine.Length; i++){
						char c = ResultLine[i];
						if (c == '[')
							InTag = true;
						if (c == ']'){
							Tag += ']';
							if (IsFurigana)
								Tag = Tag.Trim('[', ']').Split('\'')[0];//0 = kanji, 1 = furigana
							InTag = false;
							IsFurigana = false;
							New += Tag;
							Tag = string.Empty;
							continue;
						}
						if (c == '\'' && InTag)
							IsFurigana = true;
						
						if (InTag)
							Tag += c;
						else
							New += c;
					}
					ResultLine = New;
				}
				
                if (ResultLine.Trim().StartsWith("[charaname")) {
                    if (!Prefix.ContainsKey(ID))
                        Prefix[ID] = string.Empty;

                    int Bef = ResultLine.IndexOf("]") + 1;
                    Prefix[ID] += ResultLine.Substring(0, Bef);
                    ResultLine = ResultLine.Substring(Bef, ResultLine.Length - Bef);
                }
			    if (ResultLine.Trim().StartsWith("[link")) {
                    if (!Prefix.ContainsKey(ID))
                        Prefix[ID] = string.Empty;

                    int Bef = ResultLine.IndexOf("]") + 1;
                    Prefix[ID] += ResultLine.Substring(0, Bef);
                    ResultLine = ResultLine.Substring(Bef, ResultLine.Length - Bef);
                }

                for (uint i = 0; i < Tags.Length; i++) {
                    if (ResultLine.StartsWith(Tags[i])) {
                        if (!Prefix.ContainsKey(ID))
                            Prefix[ID] = string.Empty;
                        Prefix[ID] += Tags[i];
                        ResultLine = ResultLine.Substring(Tags[i].Length, ResultLine.Length - Tags[i].Length);
                    }
                    if (ResultLine.EndsWith(Tags[i])) {
                        if (!Sufix.ContainsKey(ID))
                            Sufix[ID] = string.Empty;
                        Sufix[ID] = Tags[i] + Sufix[ID];
                        ResultLine = ResultLine.Substring(0, ResultLine.Length - Tags[i].Length);
                    }
                }
				
				while (ResultLine.EndsWith("]") && !ResultLine.EndsWith("\\]")){
					if (!Sufix.ContainsKey(ID))
                        Sufix[ID] = string.Empty;
					Sufix[ID] = Sufix[ID] + ResultLine.Substring(ResultLine.LastIndexOf("["), ResultLine.Length - ResultLine.LastIndexOf("["));
					ResultLine = ResultLine.Substring(0, ResultLine.LastIndexOf("["));
				}
				
				while (ResultLine.StartsWith("[") && !ResultLine.EndsWith("\\[")){
                    if (!Prefix.ContainsKey(ID))
                        Prefix[ID] = string.Empty;
					Prefix[ID] += ResultLine.Substring(0, ResultLine.IndexOf("]") + 1);
					ResultLine = ResultLine.Substring(ResultLine.IndexOf("]") + 1, (ResultLine.Length - ResultLine.IndexOf("]")) - 1);
				}			
				
            } else {
                if (Prefix.ContainsKey(ID))
                    ResultLine = Prefix[ID] + ResultLine;
                if (Sufix.ContainsKey(ID))
                    ResultLine = ResultLine + Sufix[ID];
            }
            while (!While && Mode) {
                string L = LineWork(true, ID, ResultLine, true);
                if (ResultLine == L) {
                    ResultLine = L;
                    break;
                } else {
                    ResultLine = L;
                }
            }
            return ResultLine;
        }

		private string TrimTags(string Line){
			while (Line.EndsWith("]") && !Line.EndsWith("\\]")){
				Line = Line.Substring(0, Line.LastIndexOf("["));
			}
				
			while (Line.StartsWith("[") && !Line.EndsWith("\\[")){
				Line = Line.Substring(Line.IndexOf("]") + 1, (Line.Length - Line.IndexOf("]")) - 1);
			}
			
			return Line;
		}
		
        private bool IsString(string Line) {
            Line = Line.Trim(' ', '/', '\\', '\n', '\r', '.', '\t');
            if (string.IsNullOrEmpty(Line) || Line.Length < 3)
                return false;
            char Start = Line[0];
            bool TagTest = false;
            if (Line.StartsWith("[")) {
                for (int i = 0, c = 0; i < Line.Length; i++) {
                    char l = Line[i];
                    if (l == ']')
                        c--;
                    if (l == '[')
                        c++;
                    if (c == 0 && i + 1 == Line.Length) {
                        TagTest = true;
                        break;
                    } else if (c == 0 && i + 1 < Line.Length){
						if (Line[i+1] == '['){
							c = 0;
							continue;
						}
                        break;
					}
                }
            }

            return !(Start == '@' || Start == '*' || Start == ';' || Start == 0xFEFF || Start == '【' || TagTest /*|| (TrimTags(Line).Contains("[") && TrimTags(Line).Contains("="))*/); //It's Right?
        }

    }
}