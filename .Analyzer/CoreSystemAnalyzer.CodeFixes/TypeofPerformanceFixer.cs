using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreSystemAnalyzer
{
    //[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TypeofPerformanceFixer)), Shared]
    public class TypeofPerformanceFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create<string>(AnalyzerStrings.TypeOfAnalyzerStrings.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            Diagnostic diagnostic = context.Diagnostics.First();
            TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            TypeOfExpressionSyntax typeOfNode = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeOfExpressionSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.TypeOfTitle,
                    createChangedDocument: c => ChangeSyntaxAsync(context.Document, typeOfNode, c),
                    equivalenceKey: nameof(CodeFixResources.TypeOfTitle)),
                diagnostic);
        }

        private static async Task<Document> ChangeSyntaxAsync(Document document,
            TypeOfExpressionSyntax localDeclaration,
            CancellationToken cancellationToken)
        {
            SourceText original = localDeclaration.GetText();
            string newSyntax = $"TypeHelper.TypeOf<{localDeclaration.Type.ToString()}>.Type";

            // Get the symbol representing the type to be renamed.
            SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            ISymbol typeSymbol = semanticModel.GetDeclaredSymbol(localDeclaration, cancellationToken);

            Solution originalSolution = document.Project.Solution;
            OptionSet optionSet = originalSolution.Workspace.Options;

            Solution newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newSyntax, optionSet, cancellationToken).ConfigureAwait(false);

            return newSolution.GetDocument(document.Id);
        }
    }
}
