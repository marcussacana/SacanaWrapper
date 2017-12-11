#IMPORT System.Linq.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace JSON {
    public class Plain {
        StreamReader Reader;
        List<uint> ValLines = new List<uint>();
        Encoding Eco = Encoding.UTF8;

        public Plain(byte[] Script) {
            Reader = new StreamReader(new MemoryStream(Script), Eco);
        }

        public string[] Import() {
            List<string> Lines = new List<string>();
            ValLines = new List<uint>();	
			bool TBegin = false;
            string L1 = Reader.ReadLine();
            if (Reader.Peek() < 0)
                return new string[0];
            string L2 = Reader.ReadLine();
            if (Reader.Peek() < 0)
                return new string[0];
			string L3 = Reader.ReadLine();
            if (Reader.Peek() < 0)
                return new string[0];
			
			
			int Block = 0;
            for (uint i = 0; true; i++) {
				L1 = L2;
				L2 = L3;
				L3 = Reader.ReadLine();
				if (Reader.Peek() < 0)
					break;
				
                string TL1 = L1.Trim(' ', '\t', ',');
                string TL2 = L2.Trim(' ', '\t', ',');
                string TL3 = L3.Trim(' ', '\t', ',');
				
				TBegin |= TL1 == "\"texts\" : [";

				if (!TBegin)
					continue;					
				
				if (Block == 0){
					Block = GetPadding(L3);
				}
				
				if (TL3.StartsWith("\"text\" : ")){
					TL3 = TL3.Substring("\"text\" : ".Length, TL3.Length - "\"text\" : ".Length);
                    ValLines.Add(i + 3);
                    Lines.Add(ParseStr(TL3));
					continue;
				}
				
				if (GetPadding(L3) != Block || GetPadding(L2) != Block)
					continue;
				
                if (!(TL1.ToLower() == "null" || TL2.ToLower() == "null") && !(IsString(TL1) && IsString(TL2)))
                    continue;
				
				if (!IsString(TL3))
					continue;
				
				if (!IsString(TL1) && TL1.ToLower() != "null")
					continue;
				
                if (TL2 == TL3) {
					continue;
					
                    if (!ValLines.Contains(i + 2)) {
                        ValLines.Add(i + 2);
                        Lines.Add(ParseStr(TL2));
                    }
                }

                if (!ValLines.Contains(i + 3)) {
                    ValLines.Add(i + 3);
                    Lines.Add(ParseStr(TL3));
                }
            }
			
            Reader.BaseStream.Seek(0, 0);
            Reader.BaseStream.Flush();
            return Lines.ToArray();
        }

		private int GetPadding(string Line){			
			string TMP = Line.TrimStart(' ', '\t');
			return Line.Length - TMP.Length;
		}
		
		private bool IsString(string Line){
			if (Line.Contains("\" : \""))
				return false;
			if (!Line.StartsWith("\"") || !Line.EndsWith("\""))
				return false;
			if ("{}[]".Contains(Line))
				return false;
			string[] BlackList = new string[] { "update", "v11", "delay" };
			foreach (string Deny in BlackList){
				if (Line.ToLower().Contains(Deny))
					return false;
			}
			
			
			return true;			
		}
		
        private string ParseStr(string Str) {
            string Rst = Str.Trim('"');
            Rst = Rst.Replace("\\\\n", "\\n");
            Rst = Rst.Replace("\\\"", "\"");

            return Rst;
        }

        private string GetStr(string Str) {
            string Rst = Str;
            Rst = Rst.Replace("\\n", "\\\\n");
            Rst = Rst.Replace("\n", "\\n");
            Rst = Rst.Replace("\"", "\\\"");

            return '"' + Rst + '"';
        }

        public void AppendArray<T>(ref T[] Array, T Value) {
            T[] Output = new T[Array.Length + 1];
            Array.CopyTo(Output, 0);
            Output[Array.Length] = Value;
            Array = Output;
        }

        public byte[] Export(string[] Text) {
            StringBuilder SB = new StringBuilder();
            for (uint i = 0, x = 0; true; i++) {
                string Line = Reader.ReadLine();
                if (Reader.Peek() < 0)
                    break;

                if (!ValLines.Contains(i)) {
                    SB.AppendLine(Line);
                    continue;
                }
				string Trimed = Line.TrimStart(' ', '\t');
                int SWidth = Line.Length - Trimed.Length;
				bool ArrCnt = Line.TrimEnd(' ', '\t').EndsWith(",");
				
				if (Trimed.StartsWith("\"text\" : ")){
					SWidth += "\"text\" : ".Length;
				}
				
                string Prefix = Line.Substring(0, SWidth);

                Line = Prefix + GetStr(Text[x++]);
				if (ArrCnt)
					Line += ',';

                SB.AppendLine(Line);
            }

            Reader.BaseStream.Seek(0, 0);
            Reader.BaseStream.Flush();

            return Eco.GetBytes(SB.ToString());
        }

    }
}
