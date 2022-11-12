using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;

namespace DotNetTypeGenerator;

public class AssemblyGenerator
{
    private readonly IList<Assembly> _assemblies = new List<Assembly>();
    private readonly IList<MetadataReference> _references = new List<MetadataReference>();
    private readonly string _workingFolder;
    private readonly bool _persist;
    private readonly AssemblyLoadContext _assemblyLoadContext;

    public AssemblyLoadContext LoadContext => _assemblyLoadContext;

    public string? AssemblyName { get; set; }

    public static Func<AssemblyLoadContext> DefaultAssemblyLoadContextFactory { get; set; } 
        = () => new CustomAssemblyLoadContext(null);

    public AssemblyGenerator(
        bool persist, 
        string? workingFolder, 
        List<Assembly>? assemblies, 
        Func<AssemblyLoadContext?> assemblyLoadContextFactory)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var name = entryAssembly?.GetName().Name ?? Guid.NewGuid().ToString();
        var version = entryAssembly?.GetName()?.Version?.ToString() ?? "1.0.0";

        if (string.IsNullOrEmpty(workingFolder)) 
            workingFolder = Path.Combine(Path.GetTempPath(), "DotNetTypeGenerator", name, version);

        _workingFolder = workingFolder;
        _assemblyLoadContext = assemblyLoadContextFactory();

        if (_assemblyLoadContext == null) _assemblyLoadContext = DefaultAssemblyLoadContextFactory();

        _persist = persist;

        if (assemblies?.Any() == true)
            foreach (var assembly in assemblies)
                ReferenceAssembly(assembly);
    }

    public AssemblyGenerator(
        bool persist, 
        string? workingFolder, 
        List<Assembly>? assemblies, 
        AssemblyLoadContext? assemblyLoadContext) : 
        this(persist, workingFolder, assemblies, () => assemblyLoadContext) { }

    public AssemblyGenerator(
        bool persist = true, 
        string? workingFolder = default, 
        List<Assembly>? assemblies = null) : 
        this(persist, workingFolder, assemblies, () => null) { }

    public void ReferenceAssembly(Assembly? assembly)
    {
        if (assembly == null || _assemblies.Contains(assembly)) return;

        _assemblies.Add(assembly);

        try
        {
            var referencePath = CreateAssemblyReference(assembly);

            if (_references.Any(x => x.Display == referencePath)) return;

            var metadataReference = MetadataReference.CreateFromFile(referencePath);
            _references.Add(metadataReference);

            foreach (var referecedAssembly in assembly.GetReferencedAssemblies())
            {
                if (_assemblies.Any(x => x.GetName() == referecedAssembly)) continue;

                var loadedAssembly = _assemblyLoadContext.LoadFromAssemblyName(referecedAssembly);

                ReferenceAssembly(loadedAssembly);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Couldn't make an assembly reference to {assembly.FullName}. Try to continue without the assembly.", ex);
        }
    }

    private static string? CreateAssemblyReference(Assembly assembly)
    {
        return assembly.IsDynamic ? null : assembly.Location;
    }

    public void ReferenceAssemblyContainingType<T>() => ReferenceAssembly(typeof(T).GetTypeInfo().Assembly);

    public Assembly GenerateAssembly(string code)
    {
        var assemblyName = AssemblyName ?? Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
        var text = CSharpSyntaxTree.ParseText(code);
        var array = _references.ToArray();
        var syntaxTreeArray = new SyntaxTree[1] { text };

        if (!Directory.Exists(_workingFolder)) Directory.CreateDirectory(_workingFolder);

        var compilation = CSharpCompilation
            .Create(assemblyName, syntaxTreeArray, array,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, false, null,
                    null, null, null, OptimizationLevel.Debug, false,
                    false, null, null, new ImmutableArray<byte>(), new bool?()));

        var fullPath = Path.Combine(_workingFolder, assemblyName);
        var assemblies = _assemblies
            .Where(x => !string.IsNullOrWhiteSpace(x.Location)).Select(x => x).ToList();

        if (_assemblyLoadContext is CustomAssemblyLoadContext customAssemblyLoadContext) customAssemblyLoadContext.SetAssemblies(assemblies);

        if (!_persist)
        {
            using (var memoryStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(memoryStream);

                if (!emitResult.Success) ThrowError(code, emitResult);

                memoryStream.Seek(0L, SeekOrigin.Begin);
                var assembly = LoadContext.LoadFromStream(memoryStream);

                return assembly;
            }
        }

        using (var dllStream = new MemoryStream())
        using (var pdbStream = new MemoryStream())
        using (var win32resStream = compilation.CreateDefaultWin32Resources(true, false, null, null))
        {
            var emitResult = compilation.Emit(dllStream, pdbStream, win32Resources: win32resStream);

            if (!emitResult.Success) ThrowError(code, emitResult);

            File.WriteAllBytes(fullPath, dllStream.ToArray());

            var assembly = _assemblyLoadContext.LoadFromAssemblyPath(fullPath);

            return assembly;
        }
    }

    private static void ThrowError(string code, EmitResult emitResult)
    {
        var errors = emitResult.Diagnostics
            .Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

        var errorMsg = string.Join("\n", errors.Select(x => $"{x.Id}: {x.GetMessage()}"));

        var errorMsgWithCode = $"Compilation failures! " +
            $"{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"{errorMsg}" +
            $"{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"Code:" +
            $"{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"{code}";

        throw new InvalidOperationException(errorMsgWithCode);
    }
}
