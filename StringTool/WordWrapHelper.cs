using System;
using System.Drawing;
using System.Text;

namespace StringTool {

    [FieldParmaters(Name = "WordWrap")]
    internal struct WordwrapSettings
    {
        [FieldParmaters(DefaultValue = false, Name = "FakeBreakLine")]
        public bool FakeBreakLine;
        [FieldParmaters(DefaultValue = false, Name = "Monospaced")]
        public bool Monospaced;
        [FieldParmaters(DefaultValue = false, Name = "Bold")]
        public bool Bold;
        [FieldParmaters(DefaultValue = 0, Name = "MaxWidth")]
        public int MaxWidth;
        [FieldParmaters(DefaultValue = 0, Name = "MaxLines")]
        public int MaxLines;
        [FieldParmaters(DefaultValue = "Consolas", Name = "FaceName")]
        public string FontName;
        [FieldParmaters(DefaultValue = 12.0f, Name = "FontSize")]
        public float FontSize;
        [FieldParmaters(DefaultValue = "\\n", Name = "LineBreaker")]
        public string LineBreaker;
    }

    public static class WordWrapHelper {
        public static string GameLineBreaker {
            get {
                if (string.IsNullOrEmpty(Program.WordwrapSettings.LineBreaker))
                    return "\n";


                return Escaper.Unescape(Program.WordwrapSettings.LineBreaker);
            }
        }
        /// <summary>
        /// Wordwrap a string
        /// </summary>
        /// <param name="Input">The string to wordwrap</param>
        /// <returns>The Result String</returns>        
        public static string WordWrap(this string Input, int? Width = null) {
            if (Program.WordwrapSettings.FakeBreakLine) {
                while (Input.Contains(@"  "))
                    Input = Input.Replace(@"  ", @" ");
            }

            if (Program.WordwrapSettings.Monospaced) {
                return MonospacedWordWrap(MergeLines(Input), Width);
            } else {
                return MultispacedWordWrap(MergeLines(Input), Width);
            }
        }

        /// <summary>
        /// Remove Break Lines
        /// </summary>
        /// <param name="String">The string to remove the breakline</param>
        /// <returns>The Result</returns>
        public static string MergeLines(this string String) {
            string Rst = String.Replace(" " + GameLineBreaker + " ", "  ");
            Rst = Rst.Replace(GameLineBreaker + " ", " ");
            Rst = Rst.Replace(" " + GameLineBreaker, " ");
            Rst = Rst.Replace(GameLineBreaker, " ");
            return Rst;
        }

        #region WordWrap
        private static string MultispacedWordWrap(string String, int? Width = null) {
            Font Font = Program.WordwrapFont;
            if (Width == null)
                Width = Program.WordwrapSettings.MaxWidth;


            StringBuilder sb = new StringBuilder();
            if (Width == 0)
                return String;
            string[] Words = String.Split(' ');
            string Line = string.Empty;
            foreach (string Word in Words) {
                if (GetTextWidth(Font, Line + Word) > Width) {
                    if (Line == string.Empty) {
                        string Overload = string.Empty;
                        int Cnt = 0;
                        while (GetTextWidth(Font, Word.Substring(0, Word.Length - Cnt)) > Width)
                            Cnt++;
                        sb.AppendLine(Word.Substring(0, Word.Length - Cnt));
                        Line = Word.Substring(Word.Length - Cnt, Cnt);
                    } else {
                        sb.AppendLine(Line);
                        Line = Word;
                    }
                } else
                    Line += (Line == string.Empty) ? Word : " " + Word;
            }
            if (Line != string.Empty)
                sb.AppendLine(Line);

            string rst = sb.ToString().Replace("\r\n", "\n");

            rst = rst.Replace("\n", GameLineBreaker);

            if (rst.EndsWith(GameLineBreaker))
                rst = rst.Substring(0, rst.Length - GameLineBreaker.Length);

            if (Program.WordwrapSettings.FakeBreakLine) {
                string[] Splited = rst.Replace(GameLineBreaker, "\n").Split('\n');
                string NewRst = string.Empty;
                for (int i = 0; i < Splited.Length; i++) {
                    string tmp = Splited[i];
                    bool Last = i + 1 >= Splited.Length;
                    if (!Last)
                        while (GetTextWidth(Font, tmp) < Width)
                            tmp += ' ';

                    NewRst += tmp;
                }
                rst = NewRst;
            }

            return rst;
        }

        internal static int GetTextWidth(Font Font, string Text) {
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
                return (int)g.MeasureString(Text, Font).Width;

            //return System.Windows.Forms.TextRenderer.MeasureText(Text, Font).Width;
        }

        internal static string MonospacedWordWrap(string String, int? Width = null) {
            if (Width == null)
                Width = Program.WordwrapSettings.MaxWidth;

            int pos, next;
            StringBuilder sb = new StringBuilder();
            if (Width < 1)
                return String;
            for (pos = 0; pos < String.Length; pos = next) {
                int eol = String.IndexOf(GameLineBreaker, pos);
                if (eol == -1)
                    next = eol = String.Length;
                else
                    next = eol + GameLineBreaker.Length;
                if (eol > pos) {
                    do {
                        int len = eol - pos;
                        if (len > Width)
                            len = BreakLine(String, pos, Width.Value);
                        sb.Append(String, pos, len);
                        sb.Append(GameLineBreaker);
                        pos += len;
                        while (pos < eol && char.IsWhiteSpace(String[pos]))
                            pos++;
                    } while (eol > pos);
                } else sb.Append(GameLineBreaker);
            }
            string rst = sb.ToString();


            if (rst.EndsWith(GameLineBreaker))
                rst = rst.Substring(0, rst.Length - GameLineBreaker.Length);

            if (Program.WordwrapSettings.FakeBreakLine) {
                string[] Splited = rst.Replace(GameLineBreaker, "\n").Split('\n');
                string NewRst = string.Empty;
                for (int i = 0; i < Splited.Length; i++) {
                    string tmp = Splited[i];
                    bool Last = i + 1 >= Splited.Length;
                    if (!Last)
                        while (tmp.Length < Width)
                            tmp += ' ';
                    NewRst += tmp;
                }
                rst = NewRst;
            }

            return rst;
        }
        public static string[] SplitLines(this string Text) {
            return Text.Replace(GameLineBreaker, "\n").Split('\n');
        }
        private static int BreakLine(string text, int pos, int max) {
            int i = max - 1;
            while (i >= 0 && !char.IsWhiteSpace(text[pos + i]))
                i--;
            if (i < 0)
                return max;
            while (i >= 0 && char.IsWhiteSpace(text[pos + i]))
                i--;
            return i + 1;
        }
        #endregion
    }

}
