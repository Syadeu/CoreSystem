using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CoreSystemAnalyzer
{
    /// <summary>
    /// <see cref="DiagnosticAnalyzer"/> 를 쉽게 사용하기 위한 기초 <see langword="abstract"/> 클래스입니다.
    /// </summary>
    /// <typeparam name="TRuleProvider"></typeparam>
    public abstract class Analyzer<TRuleProvider> : DiagnosticAnalyzer
        where TRuleProvider : AnalyzerRuleProvider, new()
    {
        private readonly TRuleProvider m_RuleProvider = new TRuleProvider();

        public override sealed ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(m_RuleProvider.Rule);
        /// <summary>
        /// 이 Analyzer 의 설명입니다.
        /// </summary>
        public DiagnosticDescriptor Rule => m_RuleProvider.Rule;
        /// <summary>
        /// 이 Analyzer 의 타입입니다.
        /// </summary>
        protected abstract SyntaxKind SyntaxKind { get; }

        public override sealed void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind);
        }
        protected abstract void Analyze(SyntaxNodeAnalysisContext context);

        protected void ReportDignostic<TSyntax>(SyntaxNodeAnalysisContext context)
            where TSyntax : SyntaxNode
        {
            var expression = context.Node as TSyntax;
            var diagnostic = Diagnostic.Create(Rule, expression.GetLocation(), expression.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
