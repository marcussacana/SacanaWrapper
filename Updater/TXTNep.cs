#IMPORT System.Linq.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TXT {
    public class PlainNep {
        string[] Script;
		Encoding Eco = Encoding.GetEncoding(932); //Encoding.UTF8;
        public PlainNep(byte[] Script) {
            this.Script = Eco.GetString(Script).Replace("\r\n", "\n").Split('\n');
			
			if (!this.Script[0].StartsWith("――――――――――――――――――――――――――――――――――――――――"))
				throw new Exception();
        }

        public string[] Import() {
			List<string> Lines = new List<string>();
			string Str = string.Empty;
			for (int i = 1; i < Script.Length; i++){
				string line = Script[i];
				if (line.StartsWith("――――――――――――――――――――――――――――――――――――――――")){
					Lines.Add(Str.Length > 0 ? Str.Substring(0, Str.Length-1) : Str);
					Str = string.Empty;
					continue;
				}
				Str += line + "\n";
			}
            return Lines.ToArray();
        }		

        public byte[] Export(string[] Text) {
			StringBuilder SB = new StringBuilder();
			bool StrEnd = false;
			for (int i = 0, x = 0; i < Script.Length; i++){
				string line = Script[i];
				if (line.StartsWith("――――――――――――――――――――――――――――――――――――――――")){
					StrEnd = false;
					SB.AppendLine(line);
					if (line.EndsWith("EOF"))
						break;
					continue;
				} if (StrEnd)
					continue;
					
				SB.AppendLine(Text[x++].Replace("\n", "\r\n"));
				StrEnd = true;
			}
			
			return Eco.GetBytes(SB.ToString());
        }

    }
}

