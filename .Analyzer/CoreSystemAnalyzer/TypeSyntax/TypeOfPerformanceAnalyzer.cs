using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CoreSystemAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TypeOfPerformanceAnalyzer : Analyzer<AnalyzerStrings.TypeOfAnalyzerStrings>
    {
        protected override SyntaxKind SyntaxKind => SyntaxKind.TypeOfExpression;

        public static bool Condition(SyntaxNode node)
        {
            var typeOfNode = node as TypeOfExpressionSyntax;
            if (typeOfNode == null) return false;

            //Ignore typeof expressions that are not part of a code block (ie. as attribute params)
            var block = typeOfNode.FirstAncestorOrSelf<BlockSyntax>();
            if (block == null) return false;

            //Ignore if this is part of a member access expression
            var parentExpression = typeOfNode.FirstAncestorOrSelf<MemberAccessExpressionSyntax>();
            if (parentExpression != null) return false;

            //Special case: ignore value types that are handled differently
            var isValue = TypeOfHelper.IsValueType(typeOfNode.Type);
            if (isValue) return false;

            return true;
        }

        protected override void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (Condition(context.Node))
            {
                ReportDignostic<TypeOfExpressionSyntax>(context);
            }
        }
    }
}
