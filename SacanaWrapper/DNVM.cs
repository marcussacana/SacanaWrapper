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

        Assembly = InitializeEngine(Lines, FileName);
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
    private Assembly InitializeEngine(string[] lines, string FileName)
    {
        var References = new List<MetadataReference>();

        var CreateReferenceFromAssembly = typeof(MetadataReference).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.GetParameters().Length == 1 && x.Name == "CreateFromAssembly").Single();

        var SystemAsm = (from x in AppDomain.CurrentDomain.GetAssemblies() where x.FullName.StartsWith("System,") select x);
        var MscorlibAsm = (from x in AppDomain.CurrentDomain.GetAssemblies() where x.FullName.StartsWith("mscorlib,") select x);

        References.Add((MetadataReference)CreateReferenceFromAssembly.Invoke(null, new object[] { SystemAsm.Single() }));
        References.Add((MetadataReference)CreateReferenceFromAssembly.Invoke(null, new object[] { MscorlibAsm.Single() }));

        string SourceCode = string.Empty;
        int Imports = 0;
        bool SystemAdded = false;
        foreach (string line in lines)
        {
            if (line.ToLower().StartsWith(ImportFlag))
            {
                string ReferenceName = line.Substring(ImportFlag.Length, line.Length - ImportFlag.Length).Trim();
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
                            var Assemblies = (from x in AppDomain.CurrentDomain.GetAssemblies() where x.FullName.StartsWith(ReferenceName+",") select x);

                            foreach (var Assemby in Assemblies)
                                References.Add((MetadataReference)CreateReferenceFromAssembly.Invoke(null, new object[] { Assembly }));

                        }
                    }
                }


                Imports++;
                continue;
            }
            if (line.StartsWith("using ") || line.StartsWith("public class ") || line.StartsWith("namespace  ") || line.StartsWith("class ")) {
                if (!SystemAdded) {
                    SystemAdded = true;
                    Imports++;
                    SourceCode += "using System;\r\n";
                }
            }
            SourceCode += line + "\r\n";
        }

        var SyntaxTree = CSharpSyntaxTree.ParseText(SourceCode);

        var Compilation = CSharpCompilation.Create($"{System.IO.Path.GetFileNameWithoutExtension(FileName)}.dll", new[] { SyntaxTree }, References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        

        using (var Stream = new System.IO.MemoryStream())
        {
            EmitResult result = Compilation.Emit(Stream);

            if (!result.Success)
            {
                var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                List<Exception> Errors = new List<Exception>();
                foreach (Diagnostic diagnostic in failures.OrderBy(o => o.Location.GetLineSpan().StartLinePosition.Line))
                    Errors.Add(new Exception($"({diagnostic.Location.GetLineSpan().StartLinePosition.Line+Imports}) {diagnostic.Id}: {diagnostic.GetMessage()}"));

                throw new AggregateException(Errors.ToArray());
            }

            if (FileName != null)
                System.IO.File.WriteAllBytes(FileName, Stream.ToArray());

            return Assembly.Load(Stream.ToArray());
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