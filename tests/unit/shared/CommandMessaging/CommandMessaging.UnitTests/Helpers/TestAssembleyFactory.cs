using Cqrs.Messaging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Events;
using System.Reflection;

namespace Cqrs.UnitTests.Helpers;
public static class TestAssemblyFactory
{
    public static Assembly CompileAssemblyFile(string resourceName, string assemblyName)
    {
        // var assembly = Assembly.GetExecutingAssembly();
        // var fullResourceName = $"Cqrs.UnitTests.TestAssemblies.{resourceName}";
        
        // using var stream = assembly.GetManifestResourceStream(fullResourceName);
        // using var reader = new StreamReader(stream ?? throw new Exception($"Resource {fullResourceName} not found."));
        // string sourceCode = reader.ReadToEnd();

        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var projectRoot = Path.Combine(basePath, "..", "..", ".."); 
        var filePath = Path.Combine(projectRoot, "TestAssemblies", resourceName);
        var sourceCode = File.ReadAllText(filePath);

        return Create(sourceCode, assemblyName);
    }

    private static Assembly Create(string code, string assemblyName)
    {

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();
        // var references = new[]
        // {
        //     MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        //     MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
        //     MetadataReference.CreateFromFile(typeof(ICommand).Assembly.Location),
        //     MetadataReference.CreateFromFile(typeof(IDomainEvent).Assembly.Location)
        // };

        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var failures = string.Join("\n", result.Diagnostics.Select(d => d.ToString()));
            throw new Exception($"Compilation failed: {failures}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(ms.ToArray());
    }
}