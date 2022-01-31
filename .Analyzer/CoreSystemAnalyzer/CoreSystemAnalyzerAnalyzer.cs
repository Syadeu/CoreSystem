using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace CoreSystemAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CoreSystemAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CoreSystem";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UnusedUsingAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.UnusedUsingMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.UnusedUsingDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            //context.RegisterSyntaxTreeAction(AnalyzeSyntax);
            //context.RegisterSemanticModelAction()
            //context.RegisterSemanticModelAction(AnalyzeSyntax);
        }

        private static void AnalyzeSyntax(SemanticModelAnalysisContext ctx)
        {
            CompilationUnitSyntax root = ctx.SemanticModel.SyntaxTree.GetCompilationUnitRoot();
            UnusedUsingDirectiveCollector collector 
                = new UnusedUsingDirectiveCollector(ctx.SemanticModel, root.Usings, Rule);
            collector.Visit(root);


            for (int i = 0; i < collector.Diagnostics.Count; i++)
            {
                ctx.ReportDiagnostic(collector.Diagnostics.ElementAt(i));
            }

            //Console.WriteLine($"found {collector.UsingDirectives.Count} of un used usings.");
        }
        //private static void AnalyzeSyntax(SyntaxTreeAnalysisContext ctx)
        //{
        //    CompilationUnitSyntax root = ctx.Tree.GetCompilationUnitRoot();
        //    UnusedUsingDirectiveCollector collector = new UnusedUsingDirectiveCollector(s root.Usings, Rule);
        //    collector.Visit(root);


        //    for (int i = 0; i < collector.Diagnostics.Count; i++)
        //    {
        //        ctx.ReportDiagnostic(collector.Diagnostics.ElementAt(i));
        //    }

        //    //Console.WriteLine($"found {collector.UsingDirectives.Count} of un used usings.");
        //}
        

        //private static void AnalyzeSymbol(SymbolAnalysisContext context)
        //{
        //    // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
        //    var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        //    // Find just those named type symbols with names containing lowercase letters.
        //    if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
        //    {
        //        // For all such symbols, produce a diagnostic.
        //        var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

        //        context.ReportDiagnostic(diagnostic);
        //    }
        //}
    }

    public sealed class UnusedUsingDirectiveCollector : CSharpSyntaxWalker
    {
        private SemanticModel SemanticModel { get; }
        //private List<INamespaceSymbol> UsingDirectives { get; }
        private List<UsingDirectiveSyntax> UsingDirectives { get; }
        private DiagnosticDescriptor Descriptor { get; }
        public ICollection<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

        public UnusedUsingDirectiveCollector(
            SemanticModel semanticModel,
            SyntaxList<UsingDirectiveSyntax> usingDirectives, DiagnosticDescriptor desc)
        {
            SemanticModel = semanticModel;
            //UsingDirectives = new List<INamespaceSymbol>();
            UsingDirectives = usingDirectives.ToList();
            Descriptor = desc;

            //for (int i = 0; i < usingDirectives.Count; i++)
            //{
            //    UsingDirectives.Add((INamespaceSymbol)SemanticModel.GetSymbolInfo(usingDirectives[i].Name).Symbol);
            //}
        }
        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            INamespaceSymbol ctxNamespace = SemanticModel.GetTypeInfo(node.Type).Type?.ContainingNamespace;

            //List<UsingDirectiveSyntax> directiveSyntaxes = new List<UsingDirectiveSyntax>();

            //node.Type.



            //SyntaxTree tree = node.Type.SyntaxTree;
            //CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            //for (int i = 0; i < root.Usings.Count; i++)
            //{
            //    if (!UsingDirectives.Contains(root.Usings[i]))
            //    {
            //        var diag = Diagnostic.Create(Descriptor, root.Usings[i].GetLocation(), root.Usings[i].GetText());
            //        Diagnostics.Add(diag);

            //        UsingDirectives.RemoveAt(i);
            //        i--;
            //        continue;
            //    }

            //    UsingDirectives.Remove(root.Usings[i]);
            //}
        }
        //public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        //{
        //    if (node.Declaration.)

        //    base.VisitFieldDeclaration(node);
        //}
        //public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        //{
        //    base.VisitPropertyDeclaration(node);
        //}
        //public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        //{
        //    base.VisitMethodDeclaration(node);
        //}
    }
}
