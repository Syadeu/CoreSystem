using System;
using System.Linq;
//using Unity.CompilationPipeline.Common.Diagnostics;
//using Unity.CompilationPipeline.Common.ILPostProcessing;

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Syadeu.Presentation.CodeGen
{
    // https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class testAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CoreSystem";

        public static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resource1.AnalyzerTitle), Resource1.ResourceManager, typeof(Resource1));
        public static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resource1.AnalyzerMessageFormat), Resource1.ResourceManager, typeof(Resource1));
        public static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resource1.AnalyerDescription), Resource1.ResourceManager, typeof(Resource1));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException();

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }
        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
