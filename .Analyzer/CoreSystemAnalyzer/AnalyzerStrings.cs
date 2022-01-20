using Microsoft.CodeAnalysis;

namespace CoreSystemAnalyzer
{
    public static class AnalyzerStrings
    {
        public class TypeOfAnalyzerStrings : AnalyzerRuleProvider
        {
            public static readonly string DiagnosticId = CoreSystemAnalyzer.DiagnosticId.TypeofUsage.ToDiagnosticId();
            public static readonly LocalizableString
                Title = AnalyzerHelper.GetString(nameof(Resources.TypeofDescription)),
                MessageFormat = AnalyzerHelper.GetString(nameof(Resources.TypeofMessageFormat)),
                Description = AnalyzerHelper.GetString(nameof(Resources.TypeofDescription));

            public override DiagnosticDescriptor Rule => new DiagnosticDescriptor(
                DiagnosticId, Title, MessageFormat, 

                Categories.kPerformance, 
                DiagnosticSeverity.Error,

                isEnabledByDefault: true,
                description: Description,
                helpLinkUri: URIs.kConventionsAndStandardsUri);
        }
    }
    public abstract class AnalyzerRuleProvider
    {
        public abstract DiagnosticDescriptor Rule { get; }
    }
}
