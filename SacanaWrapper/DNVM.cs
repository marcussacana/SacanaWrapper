using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Reflection;

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

    private void DllInitialize(string Dll) {
        Assembly = Assembly.LoadFrom(Dll);
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
            return System.IO.Path.GetDirectoryName(DLL) + "\\" + System.IO.Path.GetFileNameWithoutExtension(DLL) + ".pdb";
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
    private Assembly InitializeEngine(string[] lines, string FileName, string SourcePath)
    {
        var References = new List<MetadataReference>();

        var CreateReferenceFromAssembly = typeof(MetadataReference).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.GetParameters().Length == 1 && x.Name == "CreateFromAssembly").Single();

        var SystemAsm = (from x in AppDomain.CurrentDomain.GetAssemblies() where x.FullName.StartsWith("System,") select x).Single();
        var MscorlibAsm = (from x in AppDomain.CurrentDomain.GetAssemblies() where x.FullName.StartsWith("mscorlib,") select x).Single();

        if (SystemAsm == null || MscorlibAsm == null)
            throw new NullReferenceException("Basic Reference Assembly Not Found");

        References.Add((MetadataReference)CreateReferenceFromAssembly.Invoke(null, new object[] { SystemAsm }));
        References.Add((MetadataReference)CreateReferenceFromAssembly.Invoke(null, new object[] { MscorlibAsm }));

        string SourceCode = string.Empty;
        int Imports = 0;
        foreach (string line in lines)
        {
            if (line.ToLower().StartsWith(ImportFlag) || line.ToLower().StartsWith("//" + ImportFlag))
            {
                int Skip = 0;
                if (line.StartsWith("//"))
                    Skip = 2;

                string ReferenceName = line.Substring(Skip + ImportFlag.Length, line.Length - (Skip + ImportFlag.Length)).Trim();
                if (ReferenceName.Contains("//"))
                    ReferenceName = ReferenceName.Substring(0, ReferenceName.IndexOf("//")).Trim();
                ReferenceName = ReferenceName.Replace("%CD%", AppDomain.CurrentDomain.BaseDirectory);



                if (System.IO.File.Exists(ReferenceName))
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
                            ReferenceName = System.IO.Path.GetFileNameWithoutExtension(ReferenceName);
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


                SourceCode += "//" + line + "\r\n";
                continue;
            }
          
            SourceCode += line + "\r\n";
        }

        var SyntaxTree = CSharpSyntaxTree.ParseText(SourceCode, new CSharpParseOptions(LanguageVersion.Preview), SourcePath, System.Text.Encoding.UTF8);

        var Compilation = CSharpCompilation.Create($"{System.IO.Path.GetFileNameWithoutExtension(FileName)}.dll", new[] { SyntaxTree }, References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, usings: new[] { "System" } )); ;
        
        using (var Stream = new System.IO.MemoryStream())
        using (var PDBStream = new System.IO.MemoryStream())
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
                System.IO.File.WriteAllBytes(FileName, Stream.ToArray());
                System.IO.File.WriteAllBytes(System.IO.Path.GetFileNameWithoutExtension(FileName) + ".pdb", Stream.ToArray());
            }

            return Assembly.Load(Stream.ToArray(), PDBStream.ToArray());
        }

    }

    public static string AssemblyDirectory {
        get {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return System.IO.Path.GetDirectoryName(path).TrimEnd('\\', '/');
        }
    }
}