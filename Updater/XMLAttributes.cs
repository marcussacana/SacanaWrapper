#IMPORT System.Core.dll
#IMPORT System.Linq.dll
#IMPORT System.Xml.dll
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml;

public class XMLAttributes {    
    byte[] Script;
    public XMLAttributes(byte[] Script) {
        this.Script = Script;
    }
    
    string[] Attributes = new string[] { "description", "description", "Text",  "Text0", "Text1", "Text2", "Text3", "text", "text0", "text1", "text2", "text3" };
    
    string XPATH {
        get {
           var Rst = "//*[";
            for (int i = 0; i < Attributes.Length; i++){
                Rst += "@" + Attributes[i];
                if (i + 1 < Attributes.Length)
                    Rst += " or ";
            }
            Rst += "]";
            return Rst;
        }
    }
    
    public string[] Import() {
        using (MemoryStream Stream = new MemoryStream(Script)){
            XmlDocument document = new XmlDocument();
            document.Load(Stream);
            List<string> Strs = new List<string>();
            foreach (var node in document.SelectNodes(XPATH).Cast<XmlNode>()){
                foreach (var attribute in node.Attributes.Cast<XmlAttribute>()){
                    if (Attributes.Contains(attribute.Name)){
                        Strs.Add(attribute.Value);
                    }
                }
            }
            return Strs.ToArray();
        }
    }
    
    public byte[] Export(string[] Strings){
        
        using (MemoryStream Stream = new MemoryStream(Script)){
            XmlDocument document = new XmlDocument();
            document.Load(Stream);
            
            int Index = 0;
            foreach (var node in document.SelectNodes(XPATH).Cast<XmlNode>()){
                foreach (var attribute in node.Attributes.Cast<XmlAttribute>()){
                    if (Attributes.Contains(attribute.Name)){
                        attribute.Value = Strings[Index++];
                    }
                }
            }
            
            using (MemoryStream OutStream = new MemoryStream()){
                document.Save(OutStream);
                return OutStream.ToArray();
            }
        }
    }
}