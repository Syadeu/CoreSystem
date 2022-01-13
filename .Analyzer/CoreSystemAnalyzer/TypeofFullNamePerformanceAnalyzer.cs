using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CoreSystemAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TypeofFullNamePerformanceAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor sRule = new DiagnosticDescriptor(
            DiagnosticId.TypeFullNameUsage.ToDiagnosticId(),
            "Type.FullName not allowed in code block",
            "{0} has runtime performance implications",
            Categories.kPerformance,
            DiagnosticSeverity.Error,
            true,
            "Please use TypeHelper.TypeOf<T>.FullName instead as it caches the result of the Type.FullName operation",
            URIs.kConventionsAndStandardsUri);

        public static DiagnosticDescriptor Rule
            => sRule;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(sRule);

        public override void Initialize(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleMemberAccessExpression);

        public static bool Condition(SyntaxNode node)
            => TypeofHelper.Condition(node, "FullName");

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (Condition(context.Node))
            {
                AnalyzerHelper.RegisterDiagnostic<MemberAccessExpressionSyntax>(context, sRule);
            }
        }
    }
}
