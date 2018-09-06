﻿#IMPORT System.Linq.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TXT {
    public class Plain {
        string[] Script;
		Encoding Eco = Encoding.UTF8;		
		bool BOOM = false;
		
		List<uint> Ignore;
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
			Ignore = new List<uint>();
			for (uint i = 0; i < Script.Length; i += 2){
				if (string.IsNullOrEmpty(Script[i]))
					continue;
				
				string Str = Script[i + 1].Replace("::BREAKLINE::", "\n");
				if (Lines.Contains(Str)){
					Ignore.Add(i + 1);
				}
				Lines.Add(Str);				
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
            for (uint i = 0; i < Script.Length; i+=2){	
				if (Ignore.Contains(i) || string.IsNullOrEmpty(Script[i]))
					continue;
				
                Compiler.AppendLine(Script[i]);
				Compiler.AppendLine(Text[i/2].Replace("\n", "::BREAKLINE::"));
			}
			
            byte[] barr = Eco.GetBytes(Compiler.ToString());
			if (BOOM){
				byte[] Out = new byte[barr.Length+2];
				Out[0] = 0xFF;
				Out[1] = 0xFE;
				barr.CopyTo(Out, 0);
				barr = Out;
			}
			return barr;
        }

    }
}