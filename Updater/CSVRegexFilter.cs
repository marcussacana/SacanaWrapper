#IMPORT System.Collections.Generic.dll
#IMPORT System.Linq.dll
#IMPORT System.Text.dll
#IMPORT %CD%\Plugins\System.Web.dll
#IMPORT System.Core.dll

using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class RegexFilter
{
    Encoding Encoding = Encoding.UTF8;
    string Script = string.Empty;

    string[] Regexs = new string[] {

        //Stanley The Parable: UD
        "^(?:[^,]*),(?:[^,]*),(?:[^,]*),(\"(?:(?:(?:.|\\s)*?)\"(?!\"))|(?:[^,]+)),(?:\"(?:(?:(?:.|\\s)*?)\"(?!\"))|(?:[^,]*)),(?:\"(?:(?:(?:.|\\s)*?)\"(?!\"))|(?:[^,]*)),(?:\"(?:(?:(?:.|\\s)*?)\"(?!\"))|(?:[^,]*)),(?:\"(?:(?:(?:.|\\s)*?)\"(?!\"))|(?:[^,]*)),(?:\"(?:(?:(?:.|\\s)*?)\"(?!\"))|(?:[^,]*))$"
    };
    public RegexFilter(byte[] Script) {
        this.Script = Encoding.GetString(Script);
    }

    public string[] Import()
    {
        List<string> Lines = new List<string>();
        foreach (var Exp in Regexs)
        {
            Regex Regex = new Regex(Exp, RegexOptions.Multiline);
            var Results = Regex.Matches(Script);
            foreach (var Result in Results.Cast<Match>())
            {
                foreach (var Group in Result.Groups.Cast<Group>().Skip(1))
                {
                    var Line = Unescape(Group.Value);
                    if (!string.IsNullOrWhiteSpace(Line)){
						Lines.Add(Line);
						Entries.Add(new Entry() { Offset = Group.Index, Length = Group.Length });
					}
                }
            }
        }
        return Lines.ToArray();
    }
	
	
	public string Unescape(string Str){
		if (!Str.StartsWith("\"") || !Str.EndsWith("\""))
			return Str;
		
		Str = Str.Substring(1, Str.Length - 2);
		
		return Str = Str.Replace("\"\"", "\"");
	}
	
	public string Escape(string Str){
		if (!Str.Contains("\"") && !Str.Contains(",") && !Str.Contains("\n"))
			return Str;
		Str = Str.Replace("\"", "\"\"");
		return $"\"{Str}\"";
	}

    List<Entry> Entries = new List<Entry>();

    struct Entry
    {
        public int Offset;
        public int Length;
        public string Content;
    }

    public byte[] Export(string[] Lines)
    {
        StringBuilder Builder = new StringBuilder(Script);

        var SortedEntries = Entries.Select((e, n) => { 
            e.Content = Lines[n];
            return e;
        }).OrderByDescending(x => x.Offset);

        foreach (var Entry in SortedEntries)
        {
            Builder.Remove(Entry.Offset, Entry.Length);
            Builder.Insert(Entry.Offset, Escape(Entry.Content));
        }

        return Encoding.GetBytes(Builder.ToString());
    }
}
