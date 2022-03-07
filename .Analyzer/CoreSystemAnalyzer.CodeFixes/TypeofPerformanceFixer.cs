using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
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
    // https://stackoverflow.com/questions/68705176/c-sharp-roslyn-api-insert-instructions-methods-between-each-nodes-members

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TypeofPerformanceFixer)), Shared]
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
            TypeOfExpressionSyntax targetNode = root.FindNode(context.Span)
                .FirstAncestorOrSelf<TypeOfExpressionSyntax>();

            //// TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            Diagnostic diagnostic = context.Diagnostics.First();
            TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

            //// Find the type declaration identified by the diagnostic.
            TypeOfExpressionSyntax typeOfNode = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeOfExpressionSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.TypeOfTitle,
                    createChangedDocument: c => ChangeSyntaxAsync(context.Document, typeOfNode, c),
                    equivalenceKey: nameof(CodeFixResources.TypeOfTitle)),
                context.Diagnostics[0]);
        }

        private static async Task<Document> ChangeSyntaxAsync(Document document,
            TypeOfExpressionSyntax localDeclaration,
            CancellationToken cancellationToken)
        {
            CompilationUnitSyntax oldRoot = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken);
            //NameSyntax newSyntax = SyntaxFactory.IdentifierName("TypeHelper");
            //var temp = SyntaxFactory.GenericName(SyntaxFactory.Identifier("TypeOf"), synf localDeclaration.Type);

            var usingSyntax = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Syadeu.Collections"));
            var oldUsingSyntax = oldRoot.FindNode(usingSyntax.Span);
            if (oldUsingSyntax == null)
            {
                oldRoot = oldRoot.AddUsings(usingSyntax);
            }
            else if (oldUsingSyntax.HasLeadingTrivia)
            {
                oldRoot = oldRoot.ReplaceNode(oldUsingSyntax, usingSyntax);
            }

            string newSyntax = $"TypeHelper.TypeOf<{localDeclaration.Type.ToString()}>.Type";


            //SyntaxGenerator generator = SyntaxGenerator.GetGenerator(document);
            ExpressionSyntax memberAccess
                = SyntaxFactory.ParseExpression(newSyntax);

            var newRoot = oldRoot.ReplaceNode(localDeclaration, memberAccess);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
