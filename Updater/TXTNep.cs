#IMPORT System.Linq.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TXT {
    public class PlainNep {
        string[] Script;
		Encoding Eco = Encoding.GetEncoding(932);
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
			for (int i = 0; i < Script.Length; i++){
				string line = Script[i];
				if (line.StartsWith("――――――――――――――――――――――――――――――――――――――――")){
					SB.AppendLine(line);
					continue;
				}
				SB.AppendLine(Text[i]);
			}
			
			return Eco.GetBytes(SB.ToString());
        }

    }
}

