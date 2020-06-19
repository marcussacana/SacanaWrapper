#IMPORT System.Core.dll
#IMPORT System.Linq.dll
#IMPORT System.IO.Compression.dll
#IMPORT %CD%\Plugins\HTML.dll
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using HTML;

public class EPUB {
	
    ZipArchive Zip;
    MemoryStream Data;
    List<byte[]> Pages;
    ZipArchiveEntry[] Entries;
    Plain[] Readers;
    int[] CountMap;
	
    public EPUB(byte[] Data) {        
        this.Data = new MemoryStream();
        this.Data.Write(Data, 0, Data.Length);
        this.Data.Position = 0;
        
        Zip = new ZipArchive(this.Data, ZipArchiveMode.Update, true);
    }
    
    ~EPUB(){
        if (Zip != null)
			Zip.Dispose();
		if (Data != null)
			Data.Dispose();
    }
    
    public string[] Import() {
        Pages = new List<byte[]>();
        Entries = (from x in Zip.Entries where x.FullName.ToLower().EndsWith(".xhtml") || x.FullName.ToLower().EndsWith(".html") select x).ToArray();
        foreach (var Entry in Entries){
            using (Stream EntryData = Entry.Open())
            using (MemoryStream Buffer = new MemoryStream()){
                EntryData.CopyTo(Buffer);
                Pages.Add(Buffer.ToArray());
            }
        }
        List<string> Lines = new List<string>();
        CountMap = new int[Pages.Count];
        Readers = new Plain[Pages.Count];
        for (int i = 0; i < Readers.Length; i++){
			try {
				Readers[i] = new Plain(Pages.ElementAt(i));
				var Strs = Readers[i].Import();
				if (Strs.Length == 0 || (Strs.Length == 1 && Strs.First() == "")){
					Readers[i] = null;
					continue;
				}
				
				CountMap[i] = Strs.Length;
				Lines.AddRange(Strs);
			} catch {
				Readers[i] = null;
			}
        }
        
        return Lines.ToArray();
    }
    
    public byte[] Export(string[] Lines) {
        for (int x = 0, z = 0; x < Readers.Length; x++) {
            var Reader = Readers[x];
			if (Reader == null)
				continue;
            var Count = CountMap[x];
            var Entry = Entries[x];
            var Buff = new string[Count];
            for (int i = 0; i < Buff.Length; i++, z++)
                Buff[i] = Lines[z];
            var NewData = Reader.Export(Buff);
            using (var ZipData = Entry.Open()) {
                ZipData.Position = 0;
                ZipData.SetLength(0);
                ZipData.Write(NewData, 0, NewData.Length);
                ZipData.Flush();
            }
        }
		Zip.Dispose();
        return Data.ToArray();
    }
}
