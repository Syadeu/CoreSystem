using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CoreSystemAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TypeofPerformanceAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor Rule
            => AnalyzerStrings.TypeOfAnalyzerStrings.Rule;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.TypeOfExpression);
        }

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
            var isValue = TypeofHelper.IsValueType(typeOfNode.Type);
            if (isValue) return false;

            return true;
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (Condition(context.Node))
            {
                AnalyzerHelper.RegisterDiagnostic<TypeOfExpressionSyntax>(context, Rule);
            }
        }
    }
}
