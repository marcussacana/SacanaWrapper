#IMPORT System.Linq.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NScript {
    public class Filter {
        string[] Script;
		Dictionary<uint, string> Prefix;
		Dictionary<uint, string> Sufix;
		Dictionary<uint, string> Masks;
		
		Dictionary<uint, uint> MapHelp;
		
		Encoding Eco = Encoding.GetEncoding(932);

        public Filter(byte[] Script) {
            this.Script = Eco.GetString(Decrypt(Script)).Replace("\r\n", "\n").Split('\n');
        }
		
		public byte[] Decrypt(byte[] Script){
			//First Try detect if the script is already decrypted...
			List<byte[]> Arrays = new List<byte[]>();
			Arrays.Add(new byte[] { 0x67, 0x6F, 0x74, 0x6F });
			Arrays.Add(new byte[] { 0x2E, 0x62, 0x6D, 0x70});
			Arrays.Add(new byte[] { 0x67, 0x6F, 0x73, 0x75, 0x62 });
			Arrays.Add(new byte[] { 0x6E, 0x61, 0x6D, 0x65 });
			
			int Matchs = 0;
			foreach (byte[] Arr in Arrays)
				for (uint i = 0; i < Script.LongLength; i++){
					//First byte check to optimization
					if (Script[i] == Arr[0] && EqualsAt(Script, Arr, i)){
						Matchs++;
						break;
					}
				}
			
			
			if (Matchs > 2)
				return Script;
			
			
			//Decrypt
			XOR(ref Script);
			
			return Script;
		}
		
		public void XOR(ref byte[] Data){
			for (uint i = 0; i < Data.LongLength; i++)
				Data[i] ^= 0x84;			
		}
		
		public bool EqualsAt(byte[] Data, byte[] DataToCompare, uint Pos){
			if (DataToCompare.LongLength + Pos >= Data.LongLength)
				return false;
			if (Pos >= Data.LongLength)
				return false;
			
			for (uint i = 0; i < DataToCompare.LongLength; i++){
				if (DataToCompare[i] != Data[i+Pos])
					return false;
			}	
			return true;
		}

        public string[] Import() {
			Prefix = new Dictionary<uint, string>();
			Sufix = new Dictionary<uint, string>();
			Masks = new Dictionary<uint, string>();
			
			MapHelp = new Dictionary<uint, uint>();
			
			List<string> Lines = new List<string>();
			for (uint i = 0; i < Script.Length; i++){
				string Line = Script[i];
				if (!IsDialogue(Line))
					continue;
				
				string[] Dialogues = GetDialogue(i, Line);
				MapHelp[i] = (uint)Dialogues.LongLength;
				foreach (string str in Dialogues)
					Lines.Add(str);
			}
			
            return Lines.ToArray();
        }
		
		public string SetDialogue(uint ID, string Line, string[] Strings){
			if (Eco.GetBytes(Line.Trim()[0].ToString()).Length == 2 || Line.Trim().StartsWith(">")){
				return Prefix[ID] + Strings[0].Replace("\n", "@ >") + Sufix[ID];
			} else if (Line.Trim().Split('"').Length >= 2 && !((Line.Contains("\\") && Line.Contains(".")) || Line.Contains("_"))){
				return string.Format(Masks[ID], (object[])Strings);
			}
			
			throw new Exception("Unk Dialogue");
		}
		
		public string[] GetDialogue(uint ID, string Line){
			if (Eco.GetBytes(Line.Trim()[0].ToString()).Length == 2 || Line.Trim().StartsWith(">")){
				string Rst = Line.TrimStart('\t',' ', '@', '>', '\\');
				Prefix[ID] = Line.Substring(0, Line.Length - Rst.Length);
				
				Rst = Line.TrimEnd('\t',' ', '@', '>', '\\');
				Sufix[ID] = Line.Substring(Rst.Length, Line.Length - Rst.Length);
				
				Rst = Line.Trim('\t',' ', '@', '>', '\\');
				return new string[] { Rst.Replace("@ >", "\n") };
			} else if (Line.Trim().Split('"').Length >= 2 && !((Line.Contains("\\") && Line.Contains(".")) || Line.Contains("_"))){
				string Base = Line.Replace("{", "{{").Replace("}", "}}");
				string Rst = string.Empty;
				
				List<string> Strs = new List<string>();
				string Working = string.Empty;
				bool InStr = false;
				bool Escape = false;
				foreach (char c in Base){
					if (c == '\\' && InStr){
						Escape = true;
						Working += c;
						continue;
					}
					if (Escape && InStr){
						Escape = false;
						Working += c;
						continue;
					}
					if (c == '"'){
						InStr = !InStr;
						if (!InStr){
							Rst += string.Format("\"{{{0}}}\"", Strs.Count);
							Strs.Add(Working);
							Working = string.Empty;
						}
						continue;
					}
					
					if (!InStr)
						Rst += c;
					else
						Working += c;
				}
				
				Masks[ID] = Rst;
				return Strs.ToArray();
			}
			
			throw new Exception("Unk Dialogue");
		}
		
		
		string[] NoTextCmds = new string[] { "sean_change", "dwave", "if", "mov", "lsp", "add" };
		public bool IsDialogue(string Line){
			for (int i = 0; i < NoTextCmds.Length; i++)
				if (Line.ToLower().Trim(' ', '\t').StartsWith(NoTextCmds[i].ToLower()))
					return false;
			
			if (string.IsNullOrEmpty(Line.Trim('\t', ' ', '@', '>', '\\')))
				return false;
			
			if (Line.Contains("#"))
				return false;
			
			if (Line.Trim().StartsWith(">"))
				return true;
			
			if (Line.Trim().Length == 0)
				return false;
			
			if (Line.Trim().Split('"').Length >= 2 && !((Line.Contains("\\") && Line.Contains(".")) || Line.Contains("_")))
				return true;
			
			if (Eco.GetBytes(Line.Trim()[0].ToString()).Length == 2)
				return true;
		
			return false;			
		}
		
		public void AppendArray<T>(ref T[] Array, T Value){
			T[] Output = new T[Array.Length+1];
			Array.CopyTo(Output, 0);
			Output[Array.Length] = Value;
			Array = Output;
		}

        public byte[] Export(string[] Text) {
            StringBuilder Compiler = new StringBuilder();
			
            for (uint i = 0, x = 0; i < Script.Length; i++){
				string Line = Script[i];
				if (!IsDialogue(Line)){
					Compiler.AppendLine(Line);
					continue;
				}
				
				uint Cnt = MapHelp[i];
				string[] Arr = new string[Cnt];
				for (uint b = 0; b < Cnt; b++)
					Arr[b] = Text[x+b];
				x += Cnt;
				
				Compiler.AppendLine(SetDialogue(i, Line, Arr));
			}
			
            byte[] barr = Eco.GetBytes(Compiler.ToString());
			
			XOR(ref barr);
			
			return barr;
        }

    }
}
