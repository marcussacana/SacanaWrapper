#IMPORT System.Linq.dll
#IMPORT System.Core.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TXT {
    public class PIXSTUDIO {
        string[] Script;
		Encoding Eco = Encoding.GetEncoding(932);
		bool BOOM = false;
		const string HackyDialogPrefix = "<font></font>";
		
        public PIXSTUDIO(byte[] Script) {
			if (Script[0] == 0xFF && Script[1] == 0xFE)
			{
				BOOM = true;
				byte[] narr = new byte[Script.Length-2];
				for (int i = 2; i < Script.Length; i++)
					narr[i-2] = Script[i];
				this.Script = Eco.GetString(narr).Replace("\r\n", "\n").Split('\n');
				return;
			}
            this.Script = Eco.GetString(Script).Replace("\r\n", "\n").Split('\n');
        }

        public string[] Import() {
			List<string> Lines = new List<string>();
			string Str = string.Empty;
			bool InDiag = false;
			for (int i = 0; i < Script.Length; i++){
				string line = Script[i];
				if (IsDialog(line)){
					InDiag = true;
					
					if (line.StartsWith(HackyDialogPrefix))
						line = line.Substring(HackyDialogPrefix.Length);
					
					Str += line + "\\n";
					continue;
				}
				if (InDiag){
					InDiag = false;
					Lines.Add(Str.Substring(0, Str.Length-2));
					Str = string.Empty;
				}
				
				if (IsName(line))
					Lines.Add(GetName(line));
			}
			if (Str != string.Empty)
				Lines.Add(Str);
            return Lines.ToArray();
        }
		
		public bool IsDialog(string Line){
			if (Line.Contains("\t") || string.IsNullOrEmpty(Line))
				return false;
			
			var NearDot = CharsAfter(Line, '.');
			if (NearDot.Where(x => x == ' ').Count() != NearDot.Length)
				return false;
			
			if (Line.Contains("="))
				return false;
			
			var Commands = new string[] { "//", "＠", "screenfilterremove", "global(", "if ",
										  "gosub", "goto", "label", "return", "SceneTitle",
										  "next", "sceneend", "keywait", "layerrelease", "reset",
										  "wait", "}" };
			foreach (var Command in Commands)
				if (Line.StartsWith(Command))
					return false;
			
			return true;
		}
		
		public bool IsName(string Line){
			return Line.StartsWith("＠");
		}
		
		public string GetName(string Line){
			return Line.Substring(1).Split(',').First();
		}
		
		public string SetName(string Name, string Line){
			int SufixBegin = Line.IndexOf(",");
			string Sufix = string.Empty;
			if (SufixBegin >= 0)
				Sufix = Line.Substring(SufixBegin);
			return "＠" + Name + Sufix;
		}
	
		public char[] CharsAfter(string Line, char Char){
			List<char> Chars = new List<char>();
			bool CatchNext = false;
			foreach (char c in Line){
				if (CatchNext && c != Char){//...
					Chars.Add(c);
					CatchNext = false;
					continue;
				}
				if (c == Char)
					CatchNext = true;
			}
			return Chars.ToArray();
		}
		
		public void AppendArray<T>(ref T[] Array, T Value){
			T[] Output = new T[Array.Length+1];
			Array.CopyTo(Output, 0);
			Output[Array.Length] = Value;
			Array = Output;
		}

        public byte[] Export(string[] Text) {
			StringBuilder SB = new StringBuilder();
			int ID = 0;
			bool InDiag = false;
			foreach (string Line in Script){
				if (IsDialog(Line)){
					InDiag = true;
					continue;
				}
				if (InDiag){
					InDiag = false;
					SB.AppendLine(ParseDialog(Text[ID++]));
					SB.AppendLine(Line);
				} else if (IsName(Line))
					SB.AppendLine(SetName(Text[ID++], Line));
				else
					SB.AppendLine(Line);
			}
			return Eco.GetBytes(SB.ToString());
        }
		
		public string ParseDialog(string Dialog){
			Dialog = Dialog.Replace("\\n", "\r\n");
			
			var Begin = Eco.GetBytes(Dialog);
			if (Begin.Length > 1 && Begin.First() < 0x80)
				Dialog = HackyDialogPrefix + Dialog;
			
			return Dialog;
		}

    }
}
