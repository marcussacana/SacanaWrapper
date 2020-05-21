#IMPORT netstandard.dll
#IMPORT %CD%\Plugins\KrKrSceneManager.dll
using KrKrSceneManager;

public class KrKrSceneManagerNoMDF {
    PSBAnalyzer Editor;
    public KrKrSceneManagerNoMDF(byte[] Script) {
        Editor = new PSBAnalyzer(Script);
        Editor.CompressPackage = false;
    }

    public string[] Import() {
        return Editor.Import();
    }

    public byte[] Export(string[] Strings) {
        return Editor.Export(Strings);
     }
}