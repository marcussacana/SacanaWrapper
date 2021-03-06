﻿using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

class Ini {

    internal static string GetConfig(string Key, string Name, string Path, bool Required = true) {
        byte[] CFG = File.ReadAllBytes(Path);
        return GetConfig(Key, Name, CFG, Required);
    }
    internal static string GetConfig(string Key, string Name, byte[] File, bool Required = true) {
		VerifyHeader(ref File);

        string[] Lines = Encoding.UTF8.GetString(File).Replace("\r\n", "\n").Split('\n');
        string AtualKey = string.Empty;
        foreach (string Line in Lines) {
            if (Line.StartsWith("[") && Line.EndsWith("]"))
                AtualKey = Line.Substring(1, Line.Length - 2);
            if (Line.StartsWith("!") || string.IsNullOrWhiteSpace(Line) || !Line.Contains("=") || Line.StartsWith("#") || Line.StartsWith(";"))
                continue;
            string[] Splited = Line.Split('=');
            string AtualName = Splited[0].Trim();
            string Value = Splited[1];
            for (int i = 2; i < Splited.Length; i++)
                Value += '=' + Splited[i];
            if (Name.Split(';').Any(N => N.Trim() == AtualName.Trim()) && AtualKey == Key)
                return Value;
        }
        if (!Required)
            return string.Empty;
        throw new Exception(string.Format("Config Error:\n[{0}]\n{1}=...", Key, Name));
    }
	
	internal static void VerifyHeader(ref byte[] Content){
		if (EqualsAt(Content, new byte[] { 0xEF, 0xBB, 0xBF }, 0)){
			byte[] Tmp = new byte[Content.Length - 3];
			for (uint i = 3; i < Content.LongLength; i++)
				Tmp[i - 3] = Content[i];

            Content = Tmp;
		}
	}
	
	internal static bool EqualsAt(byte[] Arr, byte[] Arr2, uint At){
		if (Arr2.LongLength + At >= Arr.LongLength)
			return false;
		
		for (uint i = 0; i < Arr2.LongLength; i++){
			if (Arr2[i] != Arr[i+At])
				return false;
		}
		
		return true;
	}


    internal static void SetConfig(string Key, string Name, string Value, string CfgPath) {
        if (GetConfigStatus(Key, Name, CfgPath) == ConfigStatus.NoFile) {
            File.WriteAllText(CfgPath, $"[{Key}]\r\n{Name}={Value}", Encoding.UTF8);
        } else {
            File.WriteAllBytes(CfgPath, SetConfig(Key, Name, Value, File.ReadAllBytes(CfgPath)));
        }
    }
    internal static byte[] SetConfig(string Key, string Name, string Value, byte[] Data) {
        VerifyHeader(ref Data);
       

        ConfigStatus cfg = GetConfigStatus(Key, Name, Data);
        string[] Lines = Encoding.UTF8.GetString(Data).Replace("\r\n", "\n").Split('\n');
        string AtualKey = string.Empty;
        if (cfg == ConfigStatus.Ok) {
            for (int i = 0; i < Lines.Length; i++) {
                string Line = Lines[i];
                if (Line.StartsWith("[") && Line.EndsWith("]"))
                    AtualKey = Line.Substring(1, Line.Length - 2);
                if (Line.StartsWith("!") || string.IsNullOrWhiteSpace(Line) || !Line.Contains("=") || Line.StartsWith("#") || Line.StartsWith(";"))
                    continue;
                string AtualName = Line.Split('=')[0].Trim();
                if (Name.Split(';').Any(N => N.Trim() == AtualName.Trim()) && AtualKey == Key) { 
                    Lines[i] = string.Format("{0}={1}", Name, Value);
                    break;
                }
            }
        }
        if (cfg == ConfigStatus.NoName) {
            List<string> Cfgs = new List<string>();
            int KeyPos = 0;
            for (int i = 0; i < Lines.Length; i++) {
                if (string.Format("[{0}]", Key) == Lines[i])
                    KeyPos = i;
                Cfgs.Add(Lines[i]);
            }
            Cfgs.Insert(KeyPos + 1, string.Format("{0}={1}", Name, Value));
            Lines = Cfgs.ToArray();
        }
        if (cfg == ConfigStatus.NoKey) {
            string[] NewLines = new string[Lines.Length + 3];
            Lines.CopyTo(NewLines, 0);
            NewLines[Lines.Length + 1] = string.Format("[{0}]", Key);
            NewLines[Lines.Length + 2] = string.Format("{0}={1}", Name, Value);
            Lines = NewLines;
        }

        StringBuilder SB = new StringBuilder();
        foreach (string str in Lines)
            SB.AppendLine(str);

        return Encoding.UTF8.GetBytes(SB.ToString());
    }

    internal enum ConfigStatus {
        NoFile, NoKey, NoName, Ok
    }
	
    internal static ConfigStatus GetConfigStatus(string Key, string Name, string CfgPath) {
        if (!File.Exists(CfgPath))
            return ConfigStatus.NoFile;
        return GetConfigStatus(Key, Name, File.ReadAllBytes(CfgPath));
    }
    internal static ConfigStatus GetConfigStatus(string Key, string Name, byte[] Data) {
        VerifyHeader(ref Data);

        string[] Lines = Encoding.UTF8.GetString(Data).Replace("\r\n", "\n").Split('\n');
        bool KeyFound = false;
        string AtualKey = string.Empty;
        foreach (string Line in Lines) {
            if (Line.StartsWith("[") && Line.EndsWith("]"))
                AtualKey = Line.Substring(1, Line.Length - 2);
            if (AtualKey == Key)
                KeyFound = true;
            if (Line.StartsWith("!") || string.IsNullOrWhiteSpace(Line) || !Line.Contains("=") || Line.StartsWith("#") || Line.StartsWith(";"))
                continue;

            string AtualName = Line.Split('=')[0].Trim();
            if (Name.Split(';').Any(N => N.Trim() == AtualName.Trim()) && AtualKey == Key)
                return ConfigStatus.Ok;
        }
        return KeyFound ? ConfigStatus.NoName : ConfigStatus.NoKey;
    }
}