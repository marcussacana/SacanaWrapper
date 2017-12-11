#IMPORT System.Linq.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SSA {
    public class Subtitle {
        string[] Script;
        Dictionary<int, string> Prefix;
        Dictionary<int, string> Sufix;
        int StartIndex;
        public Subtitle(byte[] Script) {
            this.Script = Encoding.UTF8.GetString(Script).Replace("\r\n", "\n").Split('\n');
        }

        public string[] Import() {
            List<string> Subtitle = new List<string>();
            Prefix = new Dictionary<int, string>();
            Sufix = new Dictionary<int, string>();
            StartIndex = 0;
            while (!Script[++StartIndex].ToLower().TrimStart().StartsWith("dialogue:"))
                continue;
            for (int i = StartIndex; i < Script.Length; i++) {
                //Dialogue: 10,0:00:05.77,0:00:09.36,Default,Subaru,0,0,0,,I'm in deep shit. Seriously deep shit!

                try {
                    if (string.IsNullOrWhiteSpace(Script[i]))
                        continue;
                    string Line = GetLine(i);
                    if (Line.StartsWith("{"))
                        GetPrefix(i, ref Line);
                    if (Line.EndsWith("}"))
                        GetSufix(i, ref Line);
					Clear(ref Line);
                    Subtitle.Add(Line.Replace("\\N", "\n"));
                }
                catch { System.Diagnostics.Debugger.Break(); }
            }

            return Subtitle.ToArray();
        }
		
		private void Clear(ref string Text){
			string New = string.Empty;
			bool InTag = false;
			foreach (char c in Text){
				if (c == '{')
					InTag = true;
				if (c == '}'){
					InTag = false;
					continue;
				}
				if (InTag)
					continue;
				New += c;
			}
			Text = New;
		}

        private string GetLine(int ID) {
            string[] Splited = Script[ID].Split(',');
            int len = 0;
            for (int i = 0; i < 9; i++)
                len += Splited[i].Length + 1;
            return Script[ID].Substring(len, Script[ID].Length - len);
        }

        public byte[] Export(string[] Subtitle) {
            for (int i = StartIndex; i < Script.Length; i++) {
                int ID = i - StartIndex;
                if (ID >= Subtitle.Length)
                    break;
                string NewLine = Subtitle[ID];
                FixTags(ref NewLine);
                if (Prefix.ContainsKey(i))
                    NewLine = Prefix[i] + NewLine;
                if (Sufix.ContainsKey(i))
                    NewLine = NewLine + Sufix[i];
                string OriLine = GetLine(i);
				if (string.IsNullOrWhiteSpace(OriLine))
					continue;
                Script[i] = Script[i].Replace(OriLine, NewLine.Replace("\n", "\\N"));
            }
            StringBuilder Compiler = new StringBuilder();
            foreach (string line in Script)
                Compiler.AppendLine(line);
            return Encoding.UTF8.GetBytes(Compiler.ToString());
        }

        private void FixTags(ref string line) {
            string Out = string.Empty;
            bool intag = false;
            foreach (char c in line) {
                if (c == '{')
                    intag = true;
                if (c == '}')
                    intag = false;
                if (c == ' ' && intag)
                    continue;
                Out += c;
            }
            line = Out;
        }

        private void GetPrefix(int ID, ref string Line) {
            if (!Prefix.ContainsKey(ID))
                Prefix.Add(ID, string.Empty);
            string PrefixStr = string.Empty;
            foreach (char c in Line) {
                PrefixStr += c;
                if (c == '}')
                    break;
            }
            Prefix[ID] += PrefixStr;
            Line = Line.Substring(PrefixStr.Length, Line.Length - PrefixStr.Length);
            if (Line.StartsWith("{"))
                GetPrefix(ID, ref Line);
        }
        private void GetSufix(int ID, ref string Line) {
            if (!Sufix.ContainsKey(ID))
                Sufix.Add(ID, string.Empty);
            string SufixStr = string.Empty;
            for (int i = Line.Length - 1; i >= 0 && Line[i] != '{'; i--)
                SufixStr = Line[i] + SufixStr;
            SufixStr = '{' + SufixStr;
            Sufix[ID] = SufixStr + Sufix[ID];
            Line = Line.Substring(0, Line.Length - SufixStr.Length);
            if (Line.EndsWith("}"))
                GetSufix(ID, ref Line);
        }
    }
}
