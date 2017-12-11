#IMPORT System.Linq.dll
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public class Filter {
	string[] Lines;
	Encoding Encoder = Encoding.UTF8;
	public Filter(byte[] Script){
		string Text = Encoder.GetString(Script);
		Text = Text.Replace("\r\n", "\n");
		Lines = Text.Split('\n');
	}
	
	Dictionary<int, int> IndexMap;
	Dictionary<int, string> Prefix;
	Dictionary<int, string> Sufix;
	public string[] Import(){
		IndexMap = new Dictionary<int, int>();
		Prefix = new Dictionary<int, string>();
		Sufix = new Dictionary<int, string>();
		List<string> Output = new List<string>();
		for (int i = 0; i < Lines.Length; i++){
			string Line = Lines[i].Trim(' ', '\t');
			if (!Line.Contains("\"") || Line.StartsWith("$"))
				continue;
			IndexMap.Add(Output.Count, i);
			Output.Add(Dump(Output.Count, Lines[i]));
		}
		return Output.ToArray();
	}
	
	public byte[] Export(string[] Strings){
		for (int i = 0; i < Strings.Length; i++){
			string Ready = Strings[i];
			Encode(ref Ready, true);
			Lines[IndexMap[i]] = Prefix[i] + Ready + Sufix[i];
		}
		
		StringBuilder Builder = new StringBuilder();
		foreach (string Line in Lines)
			Builder.AppendLine(Line);
		
		return Encoder.GetBytes(Builder.ToString());
	}
	
	private string Dump(int Index, string Line){
		string Prefix = "", Sufix = "", Text = "";
		int Ind = 0;
		bool Escape = false;
		foreach (char c in Line){
			if (c == '"' && !Escape && Ind == 1){
				Ind++;
				Sufix += '"';
				continue;
			} 
			switch (Ind){
				case 0:
				   Prefix += c;
				   break;
				case 1:
					Text += c;
					break;
				case 2:
					Sufix += c;
					break;
				default:
					break;
			}
			if (c == '"' && !Escape){
				Ind++;
			} else if (c == '\\' && !Escape)
				Escape = true;
			else if (c == '"' || c == 'r' || c == 'n' || c == '\\' || c == 't')
				Escape = false;
		}
		this.Prefix.Add(Index, Prefix);
		this.Sufix.Add(Index, Sufix);
		Encode(ref Text, false);
		return Text;
	}
	
	private void Encode(ref string String, bool Enable) {
            if (Enable) {
                string Result = string.Empty;
                foreach (char c in String) {
                    if (c == '\n')
                        Result += "\\n";
                    else if (c == '\\')
                        Result += "\\\\";
                    else if (c == '\t')
                        Result += "\\t";
                    else if (c == '\r')
                        Result += "\\r";
                    else if (c == '"')
						Result += "\\\"";
					else
                        Result += c;
                }
                String = Result;
            } else {
                string Result = string.Empty;
                bool Special = false;
                foreach (char c in String) {
                    if (c == '\\' & !Special) {
                        Special = true;
                        continue;
                    }
                    if (Special) {
                        switch (c.ToString().ToLower()[0]) {
                            case '\\':
                                Result += '\\';
                                break;
                            case 'n':
                                Result += '\n';
                                break;
                            case 't':
                                Result += '\t';
                                break;
                            case 'r':
                                Result += '\r';
                                break;
							case '"':
								Result += '"';
								break;
                            default:
                                throw new Exception("\\" + c + " Isn't a valid string escape.");
                        }
                        Special = false;
                    } else
                        Result += c;
                }
                String = Result;
            }
        }
}