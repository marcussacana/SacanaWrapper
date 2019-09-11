#IMPORT %CD%\Plugins\KrKrSceneManager.dll
using KrKrSceneManager;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class KrKrSceneManagerNoMDF {
    PSBAnalyzer Editor;
    public KrKrSceneManagerNoMDF(byte[] Script) {
        Editor = new PSBAnalyzer(Script);
        Editor.CompressPackget = false;
    }

    public string[] Import() {
        return Editor.Import();
    }

    public byte[] Export(string[] Strings) {
        return Editor.Export(Strings);
     }
}