#IMPORT System.Linq.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersonaLib {
    public class TXT {
        string[] Script;
		Encoding Eco = Encoding.UTF8;
		bool BOOM = false;
        public TXT(byte[] Script) {
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
		//e100_001.ptp\t0\t0\t<EMPTY>\t> Estaciいn terminal, de noche...
        public string[] Import() {
			List<string> Lines = new List<string>();
			for (int i = 0; i < Script.Length; i++){
				var Blocks = Script[i].Split('\t');

				if (Blocks.Length < 5)
					continue;

				if (Blocks[3] != "<EMPTY>")
					Lines.Add(Blocks[3].Replace("{0A}", "\\n"));
				
				Lines.Add(Blocks[4].Replace("{0A}", "\\n"));
			}
            return Lines.ToArray();
        }

        public byte[] Export(string[] Strings) {
			StringBuilder SB = new StringBuilder();
			for (int i = 0, x = 0; i < Script.Length; i++){
				var Blocks = Script[i].Split('\t');

				if (Blocks.Length < 5){
					SB.AppendLine(Script[i]);
					continue;
				}

				if (Blocks[3] != "<EMPTY>")
					Blocks[3] = Strings[x++].Replace("\\n", "{0A}");
				
				Blocks[4] = Strings[x++].Replace("\\n", "{0A}");
				SB.AppendLine(string.Join("\t", Blocks));
			}
			
			return Eco.GetBytes(SB.ToString());
        }

    }
}
