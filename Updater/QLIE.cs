#IMPORT System.Linq.dll
#IMPORT System.Core.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLIE {
    public class S {
        string[] Script;
		Encoding Eco = Encoding.GetEncoding(932);
        public S(byte[] Script) {
            this.Script = Eco.GetString(Script).Replace("\r\n", "\n").Split('\n');
			
        }

        public string[] Import() {
			List<string> Lines = new List<string>();
			string Str = string.Empty;
			for (int i = 1; i < Script.Length; i++){
				string line = Script[i].Trim();
				if (line.StartsWith("^") || line.StartsWith("@") || line.StartsWith("\\") || string.IsNullOrEmpty(line)){
					if (Str != string.Empty){
						Lines.Add(Str.Substring(1));
						Str = string.Empty;
					}
					if (line.StartsWith("^select")){
						var Choices = Split(line).Skip(1).ToArray();
						Lines.AddRange(Choices);
					}
					continue;
				}
				string[] Parts = Split(line);
				if (Parts.Length == 3){
					Lines.Add(Parts[1]);
					Str += "\n" + Parts[2];
					continue;
				}
				Str += "\n" + line;
			}
			if (Str != string.Empty){
				Lines.Add(Str);
			}
			
            return Lines.ToArray();
        }		
		
		private string[] Split(string Line){
			string File = string.Empty;
			string Name = string.Empty;
			string Text = string.Empty;
			List<string> Other = new List<string>();
			int Part = 0;
			bool InTag = false;
			foreach (char c in Line){
				if (c == '[')
					InTag = true;
				if (c == ']')
					InTag = false;
				if (c == ',' && !InTag){
					Part++;
					continue;
				}
				
				switch (Part){
					case 0:
						File += c;
						break;
					case 1:
						Name += c;
						break;
					case 2:
						Text += c;
						break;
					default:
						Other[Part-3] += c;
						break;
				}
			}
			
			switch (Part){
				case 0:
					return new string[] {File};
				case 1:
					return new string[] {File, Name};
				case 2:
					return new string[] {File, Name, Text};
				default:
					return new string[] {File, Name, Text}.Concat(Other).ToArray();
			}
		}
		
        public byte[] Export(string[] Text) {
			System.Diagnostics.Debugger.Launch();
			StringBuilder SB = new StringBuilder();
			for (int i = 0, x = 0; i < Script.Length; i++){
				string line = Script[i].Trim();
				if (line.StartsWith("^") || line.StartsWith("@") || line.StartsWith("\\") || string.IsNullOrEmpty(line)){
					if (line.StartsWith("^select")){
						var SelParts = Split(line);
						if (SelParts.Length > 1) {
							for (int ind = 1; ind < SelParts.Length; ind++){
								SelParts[ind] = Text[x++].Replace(" ", "　");
							}
							Console.WriteLine("X: " + x + " I:" + i);
							SB.AppendLine(string.Join(",", SelParts));
							continue;
						}
					}
					SB.AppendLine(Script[i]);
					continue;
				}
				string[] Parts = Split(line);
				if (Parts.Length == 3){
					SB.AppendLine(string.Format("{0},{1},{2}", Parts[0], Text[x++].Replace(",", "､"), Text[x++].Replace("\n", "\r\n").Replace(",", "､")));
				} else if (Parts.Length != 1){
					SB.AppendLine(line);
				} else {
					SB.AppendLine(Text[x++].Replace("\n", "\r\n").Replace(",", "､"));
				}				
			}
			
			return Eco.GetBytes(SB.ToString());
        }

    }
}

