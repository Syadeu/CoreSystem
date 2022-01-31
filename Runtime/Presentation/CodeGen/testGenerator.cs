//using Unity.CompilationPipeline.Common.Diagnostics;
//using Unity.CompilationPipeline.Common.ILPostProcessing;

using Microsoft.CodeAnalysis;
using Syadeu.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Il2Cpp;

namespace Syadeu.Presentation.CodeGen
{
    // https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview
    [Generator]
    public class testGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // Find the main method
            var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);

            // Build up the source code
            string source = $@" // Auto-generated code
using System;

namespace {mainMethod.ContainingNamespace.ToDisplayString()}
{{
    public static partial class {mainMethod.ContainingType.Name}
    {{
        static partial void HelloFrom(string name) =>
            Console.WriteLine($""Generator says: Hi from '{{name}}'"");
    }}
}}
";
            var typeName = mainMethod.ContainingType.Name;

            // Add the source code to the compilation
            context.AddSource($"{typeName}.g.cs", source);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }

    public partial class SourceGenerator
    {
        static partial void Test(string name);
    }
}
