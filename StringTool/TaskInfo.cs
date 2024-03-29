﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringTool
{

    enum TaskType
    {
        Insert,
        Dump,
        WordWrap,
        Debug
    }

    struct TaskInfo
    {
        public TaskType Type;

        public string InputFile;
        public string InputText;
        public string OutputFile;

        public TaskInfo(TaskType Type, string InputFile, string InputText, string OutputFile)
        {
            this.Type = Type;
            this.InputFile = InputFile;
            this.InputText = InputText;
            this.OutputFile = OutputFile;
        }

        public TaskInfo(TaskType Type, string InputFile) : this(Type, InputFile, null) { }
        public TaskInfo(TaskType Type, string InputFile, string InputText)
        {
            if (File.Exists(InputFile))
                InputFile = new FileInfo(InputFile).FullName;
            
            if (File.Exists(InputText))
                InputFile = new FileInfo(InputText).FullName;

            switch (Type)
            {
                case TaskType.Insert:
                    InputText = Path.Combine(Path.GetDirectoryName(InputFile), Path.GetFileNameWithoutExtension(InputFile) + ".dump.txt");
                    if (InputFile.EndsWith(".dump.txt"))
                    {
                        InputText = InputFile;
                        var Dir = Path.GetDirectoryName(InputText);
                        var Found = Directory.GetFiles(Dir, Path.GetFileNameWithoutExtension(InputText).Replace(".dump", "") + ".*").Where(x => !x.EndsWith(".dump.txt"));
                        if (Found.Any())
                            InputFile = Found.First();
                        else
                            throw new Exception("No script found for the dump: " + Path.GetFileName(InputFile));
                    }
                    else if (Directory.GetFiles(Path.GetDirectoryName(InputFile), Path.GetFileNameWithoutExtension(InputFile) + ".*").Length == 2) {
                        var Files = Directory.GetFiles(Path.GetDirectoryName(InputFile), Path.GetFileNameWithoutExtension(InputFile) + ".*");
                        InputText = InputFile;
                        InputFile = Files.Where(x => !x.EndsWith(Path.GetFileName(InputFile))).Single();
                    }
                    OutputFile = Path.Combine(Path.GetDirectoryName(InputFile), Path.GetFileNameWithoutExtension(InputFile) + Path.GetExtension(InputFile) + ".new");
                    break;
                case TaskType.Dump:
                    OutputFile = Path.Combine(Path.GetDirectoryName(InputFile), Path.GetFileNameWithoutExtension(InputFile) + ".dump.txt");
                    break;
                case TaskType.WordWrap:
                    if (InputText == null || !File.Exists(InputText)) {
                        InputText = Path.Combine(Path.GetDirectoryName(InputFile), Path.GetFileNameWithoutExtension(InputFile) + ".dump.txt");
                    }
                    OutputFile = Path.Combine(Path.GetDirectoryName(InputFile), Path.GetFileNameWithoutExtension(InputFile).Replace("_New", "").Replace(".new", "") + "_Wordwrap" + Path.GetExtension(InputFile));
                    break;
                case TaskType.Debug:
                    OutputFile = InputFile;
                    break;
                default:
                    throw new Exception("Invalid Task Type");
            }

            this.Type = Type;
            this.InputFile = InputFile;
            this.InputText = InputText;
        }
    }
}
