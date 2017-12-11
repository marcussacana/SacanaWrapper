#IMPORT System.Linq.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TXT {
    public class Plain {
        string[] Script;
		Encoding Eco = Encoding.UTF8;
		bool BOOM = false;
        public Plain(byte[] Script) {
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
			for (int i = 0; i < Script.Length; i++){
				string line = Script[i];
				
				//if (line.StartsWith("[") && Str != string.Empty){
					Lines.Add(line);
				continue;
					Str = string.Empty;
				//}
				
				if (line == string.Empty || line.StartsWith("["))
					continue;
				
				if (Str == string.Empty)
					Str = line;
				else
					Str += "\n" + line;
			}
			if (Str != string.Empty)
				Lines.Add(Str);
            return Lines.ToArray();
        }
		
		public void AppendArray<T>(ref T[] Array, T Value){
			T[] Output = new T[Array.Length+1];
			Array.CopyTo(Output, 0);
			Output[Array.Length] = Value;
			Array = Output;
		}

        public byte[] Export(string[] Text) {
			StringBuilder SB = new StringBuilder();
			foreach (string Line in Text)
				SB.AppendLine(Line);
			
			return Eco.GetBytes(SB.ToString());
        }

    }
}
