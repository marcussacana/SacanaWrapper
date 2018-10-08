#IMPORT System.Linq.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Debonosu {
    public class LUA {
        byte[] Script;
		Encoding Eco = Encoding.GetEncoding(932);	
		
		
        public LUA(byte[] Script) {
			this.Script = Script;
        }

        public string[] Import() {
			List<string> Lines = new List<string>();
			for (uint i = 0; i < Script.Length; i++) {
				try{
					if (Script[i] != 0x04 || Script[i+4] != 0x00)
						continue;
					byte[] DW = new byte[] { Script[i+1], Script[i+2], Script[i+3], Script[i+4]};
					int Len = BitConverter.ToInt32(DW, 0);
					if (Script[i + 4 + Len] != 0x00 || Len < 3)
						continue;
					List<byte> Buffer = new List<byte>();
					while (Script[i + 5 + Buffer.Count] != 0x00)
						Buffer.Add(Script[i + 5 + Buffer.Count]);
					if (Buffer.Count != Len - 1)
						continue;
					Lines.Add(Eco.GetString(Buffer.ToArray()));
					i += 4 + (uint)Len;
				} catch {
					
				}
			}
            return Lines.ToArray();
        }     

		private bool IsSJIS(byte Prefix) {
            if ((Prefix >= 0x81 && Prefix <= 0x83) || (Prefix >= 0x88 && Prefix <= 0xEE))
                return true;
            return false;
        }
		

        public byte[] Export(string[] Text) {			
			return new byte[0];
        }		

    }
}
