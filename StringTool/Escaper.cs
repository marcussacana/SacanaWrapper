using System;
using System.Collections.Generic;

namespace StringTool
{
    class Escaper
    {
        public static Dictionary<char, char> EncodeMapping = new Dictionary<char, char>();
        public static Dictionary<char, char> DecodeMapping = new Dictionary<char, char>();

        public static void Escape(string[] Strings) {
            Encode(Strings, true);
        }

        public static void Unescape(string[] Strings) {
            Encode(Strings, false);
        }

        static void Encode(string[] Strings, bool Enable)
        {
            for (int i = 0; i < Strings.Length; i++)
                Strings[i] = Encode(Strings[i], Enable);
        }

        static string Encode(string String, bool Enable)
        {
            if (Enable)
            {
                string Result = string.Empty;
                foreach (char c in String)
                {
                    if (c == '\n')
                        Result += "\\n";
                    else if (c == '\\')
                        Result += "\\\\";
                    else if (c == '\t')
                        Result += "\\t";
                    else if (c == '\r')
                        Result += "\\r";
                    else
                        Result += EncodeMapping.ContainsKey(c) ? EncodeMapping[c] : c;
                }
                String = Result;
            }
            else
            {
                string Result = string.Empty;
                bool Special = false;
                foreach (char c in String)
                {
                    if (c == '\\' & !Special)
                    {
                        Special = true;
                        continue;
                    }
                    if (Special)
                    {
                        switch (c.ToString().ToLower()[0])
                        {
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
                            default:
                                throw new Exception("\\" + c + " Isn't a valid string escape.");
                        }
                        Special = false;
                    }
                    else
                        Result += DecodeMapping.ContainsKey(c) ? DecodeMapping[c] : c;
                }
                String = Result;
            }

            return String;
        }
    }
}
