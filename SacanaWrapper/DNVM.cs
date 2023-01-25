using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.IO;

class DotNetVM {
    public enum Language {
        CSharp, VisualBasic
    }

    internal DotNetVM(byte[] Data) {
        Assembly = AppDomain.CurrentDomain.Load(Data);
    }
    internal DotNetVM(string Content, string FileName) {
        if (System.IO.File.Exists(Content)) {
            DllInitialize(Content);
            return;
        }

        System.IO.StringReader Sr = new System.IO.StringReader(Content);
        string[] Lines = new string[0];
        while (Sr.Peek() != -1) {
            string[] tmp = new string[Lines.Length + 1];
            Lines.CopyTo(tmp, 0);
            tmp[Lines.Length] = Sr.ReadLine();
            Lines = tmp;
        }

        Assembly = InitializeEngine(Lines, FileName, null);
    }
    internal DotNetVM(string[] SourceFiles, string FileName)
    {
        List<string> Lines = new List<string>();
        foreach (var File in SourceFiles)
        {
            var Sr = System.IO.File.OpenText(File);
            while (Sr.Peek() != -1)
            {
                Lines.Add(Sr.ReadLine());
            }
        }

        Assembly = InitializeEngine(Lines.ToArray(), FileName, SourceFiles.First());
    }

    private void DllInitialize(string DLL) {
        this.DLL = DLL;

        if (!File.Exists(AssemblyDebugSymbols))
            Assembly = Assembly.LoadFrom(AssemblyPath);
        else
            Assembly = Assembly.Load(File.ReadAllBytes(AssemblyPath), File.ReadAllBytes(AssemblyDebugSymbols));
    }

    public Assembly Assembly { get; private set; }


    string DLL;
    public string AssemblyPath {
        get {
            return DLL;
        }
    }

    public string AssemblyDebugSymbols {
        get {
            return Path.Combine(Path.GetDirectoryName(DLL),  Path.GetFileNameWithoutExtension(DLL) + ".pdb");
        }
    }

    internal static void Crash() {
        Crash();
    }

    /// <summary>
    /// Call a Function with arguments and return the result
    /// </summary>
    /// <param name="ClassName">Class o the target function</param>
    /// <param name="FunctionName">Target function name</param>
    /// <param name="Arguments">Function parameters</param>
    /// <returns></returns>
    internal dynamic Call(string ClassName, string FunctionName, params object[] Arguments) {
        return exec(Arguments, ClassName, FunctionName, Assembly);
    }
    internal void StartInstance(string Class, params object[] Arguments) {
        Type fooType = Assembly.GetType(Class);
        Instance = Activator.CreateInstance(fooType, Arguments);
        LastClass = Class;
    }

    private string LastClass;
    public object Instance = null;
    private object exec(object[] Args, string Class, string Function, Assembly assembly) {
        if (LastClass != Class)
            Instance = null;
        LastClass = Class;

        Type fooType = assembly.GetType(Class);

        MethodInfo[] Methods = fooType.GetMethods().Where(x => x.Name == Function).Select(x => x).ToArray();

        foreach (MethodInfo Method in Methods) {
            if (Method.GetParameters().Length == Args.Length) {
                try {
                    if (Instance == null && !Method.IsStatic)
                        Instance = assembly.CreateInstance(Class);

                    return Method?.Invoke(Instance, BindingFlags.InvokeMethod, null, Args, CultureInfo.CurrentCulture);
                } catch (Exception ex) {
                    if (Method == Methods.Last())
                        throw;
                }
            }
        }

        throw new Exception("Failed to find the method...");
    }

    const string ImportFlag = "#import ";
    private Assembly InitializeEngine(string[] Lines, string FileName, string SourcePath)
    {
        var References = new List<MetadataReference>();

        var CreateReferenceFromAssembly = typeof(MetadataReference).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.GetParameters().Length == 1 && x.Name == "CreateFromAssembly").Single();

        var SystemAsm = (from x in AppDomain.CurrentDomain.GetAssemblies() where x.FullName.StartsWith("System,") select x).Single();
        var MscorlibAsm = (from x in AppDomain.CurrentDomain.GetAssemblies() where x.FullName.StartsWith("mscorlib,") select x).Single();

        if (SystemAsm == null || MscorlibAsm == null)
            throw new NullReferenceException("Basic Reference Assembly Not Found");

        References.Add((MetadataReference)CreateReferenceFromAssembly.Invoke(null, new object[] { MscorlibAsm }));
        References.Add((MetadataReference)CreateReferenceFromAssembly.Invoke(null, new object[] { SystemAsm }));

        string SourceCode = string.Empty;
        int Imports = 0;
        foreach (string Line in Lines)
        {
            if (Line.ToLower().StartsWith(ImportFlag) || Line.ToLower().StartsWith("//" + ImportFlag))
            {
                int Skip = 0;
                if (Line.StartsWith("//"))
                    Skip = 2;

                string ReferenceName = Line.Substring(Skip + ImportFlag.Length, Line.Length - (Skip + ImportFlag.Length)).Trim();
                if (ReferenceName.Contains("//"))
                    ReferenceName = ReferenceName.Substring(0, ReferenceName.IndexOf("//")).Trim();
                ReferenceName = ReferenceName.Replace("%CD%", AppDomain.CurrentDomain.BaseDirectory);



                if (File.Exists(ReferenceName))
                    References.Add(MetadataReference.CreateFromFile(ReferenceName));
                else
                {
                    try
                    {
                        References.Add((MetadataReference)CreateReferenceFromAssembly.Invoke(null, new object[] { Assembly.Load(ReferenceName) }));
                    }
                    catch
                    {
                        try
                        {
                            ReferenceName = Path.GetFileNameWithoutExtension(ReferenceName);
                            References.Add((MetadataReference)CreateReferenceFromAssembly.Invoke(null, new object[] { Assembly.Load(ReferenceName) }));
                        }
                        catch
                        {
                            var Assemblies = (from x in AppDomain.CurrentDomain.GetAssemblies() where x.FullName.StartsWith(ReferenceName+",") && !string.IsNullOrWhiteSpace(x.Location) select x).ToArray();


                            foreach (var Assembly in Assemblies)
                                References.Add((MetadataReference)CreateReferenceFromAssembly.Invoke(null, new object[] { Assembly }));

                        }
                    }
                }


                SourceCode += "//" + Line + "\r\n";
                continue;
            }
          
            SourceCode += Line + "\r\n";
        }

        DLL = Path.Combine(Path.GetDirectoryName(FileName), $"{Path.GetFileNameWithoutExtension(FileName)}.dll");

        var SyntaxTree = CSharpSyntaxTree.ParseText(SourceCode, new CSharpParseOptions(LanguageVersion.Preview), SourcePath, System.Text.Encoding.UTF8);

        var Compilation = CSharpCompilation.Create(Path.GetFileName(AssemblyPath), new[] { SyntaxTree }, References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, usings: new[] { "System" } )); ;
        
        using (var Stream = new MemoryStream())
        using (var PDBStream = new MemoryStream())
        {
            EmitResult result = Compilation.Emit(Stream, PDBStream);

            if (!result.Success)
            {
                var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                List<Exception> Errors = new List<Exception>();
                foreach (Diagnostic diagnostic in failures.OrderBy(o => o.Location.GetLineSpan().StartLinePosition.Line))
                    Errors.Add(new Exception($"({diagnostic.Location.GetLineSpan().StartLinePosition.Line + Imports}) {diagnostic.Id}: {diagnostic.GetMessage()}"));

                throw new AggregateException(Errors.ToArray());
            }

            if (FileName != null)
            {
                File.WriteAllBytes(AssemblyPath, Stream.ToArray());
                File.WriteAllBytes(AssemblyDebugSymbols, Stream.ToArray());
            }

            return Assembly.Load(Stream.ToArray(), PDBStream.ToArray());
        }

    }

    public static string AssemblyDirectory {
        get {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path).TrimEnd('\\', '/');
        }
    }
}