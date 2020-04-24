#IMPORT System.Core.dll
#IMPORT System.Linq.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTML {
    public class Plain {
        string Script;
        List<TextInfo> Infos;
        Encoding Eco = Encoding.UTF8;
		bool BOOM = false;
        public Plain(byte[] Script) {
			if (Script[0] == 0xFF && Script[1] == 0xFE)
			{
				BOOM = true;
				byte[] narr = new byte[Script.Length-2];
				for (int i = 2; i < Script.Length; i++)
					narr[i-2] = Script[i];
				this.Script = Eco.GetString(narr).Replace("\r\n", "\n");
				return;
			}
            this.Script = Eco.GetString(Script).Replace("\r\n", "\n");
        }

        private readonly string[] SkipTags = new string[] { "em", "b", "i", "br" };
        public string[] Import() {
			Infos = new List<TextInfo>();
            int Status = 0;
            TextInfo TagInfo = new TextInfo();
            TextInfo Current = new TextInfo();
            for (int i = 0; i < Script.Length; i++) {
                char c = Script[i];
                switch (Status) {
                    default:
                        if (c == '<') {
                            Status = 1;
                            TagInfo.Begin = i;
                            break;
                        }
                        break;
                    case 1:
                        if (c == '>') {
                            TagInfo.End = i;
                            string Tag = GetText(TagInfo).ToLower().Trim(' ', '\n', '\r', '<', '>', '\\', '/');
                            if ((from x in SkipTags where Tag == x select x).Any()) {
                                Current = Infos.Last();
                                Infos.Remove(Current);
                                Status = 2;
                                break;
                            }

                            Status = 2;
                            Current = new TextInfo();
                            Current.Begin = i + 1;
                            continue;
                        }
                        if (char.IsWhiteSpace(c) && i == Current.Begin)
                            Current.Begin = i;
                        break;
                    case 2:
                        if (c == '<') {
                            Current.End = i;
                            if (Current.Length > 2) {
                                while (char.IsWhiteSpace(Script[Current.End - 1]))
                                    Current.End--;
                                if (Current.Length > 2)
                                    Infos.Add(Current);
                            }
                            TagInfo.Begin = i;
                            Status = 1;
                            break;
                        }
                        break;
                }
            }
            return (from x in Infos select GetText(x)).ToArray();
        }

        public byte[] Export(string[] Text) {
			StringBuilder SB = new StringBuilder(Script);
            for (int i = Infos.Count - 1; i >= 0; i--)
                SetText(Infos[i], SB, Text[i]);

            return Eco.GetBytes(SB.ToString());
        }

        struct TextInfo {
            public int Begin;
            public int End;

            public int Length
            {
                get {
                    return End - Begin;
                }
                set {
                    End = Begin  + value;
                }
            }
        }

        private string GetText(TextInfo Info) {
            return Script.Substring(Info.Begin, Info.End - Info.Begin);
        }

        private void SetText(TextInfo Info, StringBuilder Builder, string Content) {
            Builder.Remove(Info.Begin, Info.Length); 
            Builder.Insert(Info.Begin, Content);
        }
    }
}
