#IMPORT System.Linq.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuiNani {
    public class Plain {
        string[] Script;
		Encoding Eco = Encoding.GetEncoding(932);		
		
		const string NamePrefix = "$62,1,39,";
		
		List<uint> Encoded;
		
        public Plain(byte[] Script) {
            this.Script = Eco.GetString(Script).Replace("\r\n", "\n").Split('\n');
        }

        public string[] Import() {
			List<string> Lines = new List<string>();
			Encoded = new List<uint>();
			bool LastIsString = false;
			for (uint i = 0; i < Script.Length; i++) {
				string Line = Script[i];
				if (string.IsNullOrEmpty(Line) || Line.StartsWith("$") && !Line.StartsWith(NamePrefix)){
					LastIsString = false;
					continue;
				}		
				
				if (Line.StartsWith(NamePrefix)){
					Line = Line.Substring(NamePrefix.Length);
					if (Line.Contains("%") && (!Line.Contains("%s") && !Line.Contains("%n") && !Line.Contains("%r"))){
						Encoded.Add(i);
						Line = Line.Replace("%", "");
					}
					Lines.Add(Line);
					LastIsString = false;
					continue;
				}
				bool HasEncoded = false;
				if (Line.Contains("%") && (!Line.Contains("%s") && !Line.Contains("%n") && !Line.Contains("%r"))){
					Encoded.Add(i);
					HasEncoded = true;
					Line = Line.Replace("%", "");
				}
				if (LastIsString){
					if (HasEncoded){
						Encoded.Add(i-1);
					}
					Lines[Lines.Count - 1] += "\n" + Line;
					continue;
				}
				LastIsString = true;
				Lines.Add(Line);
			}
            return Lines.ToArray();
        }
		
		public void AppendArray<T>(ref T[] Array, T Value){
			T[] Output = new T[Array.Length+1];
			Array.CopyTo(Output, 0);
			Output[Array.Length] = Value;
			Array = Output;
		}

        public byte[] Export(string[] Text) {
            StringBuilder Compiler = new StringBuilder();
			bool LastIsString = false;
            for (uint i = 0, x = 0; i < Script.Length; i++){
				string Line = Script[i];					
				if (string.IsNullOrEmpty(Line) || (Line.StartsWith("$") && !Line.StartsWith(NamePrefix))){
					Compiler.AppendLine(Line);
					LastIsString = false;
					continue;
				}			
				
				bool Encoded = this.Encoded.Contains(i);
				if (Line.StartsWith(NamePrefix)){
					Line = Text[x++];
					if (Encoded)
						Line = Encode(Line);
					Compiler.AppendLine(NamePrefix + Line);
					LastIsString = false;
					continue;
				}
				if (LastIsString)
					continue;
				Line = Text[x++];
				if (Encoded)
					Line = Encode(Line);
				Compiler.AppendLine(Line.Replace("\n", "\r\n"));
				LastIsString = true;
			}
			
			return Eco.GetBytes(Compiler.ToString());
        }
		
		private string Encode(string Str){
			return Str;
			List<char> Escapable = new List<char>(new char[] { '<', '>', '?', '!', '(', ')', '*' });
			string Rst = string.Empty;
			foreach (char c in Str){
				if (Escapable.Contains(c))
					Rst += '%';
				Rst += c;
			}
			
			return Rst;
		}

    }
}
