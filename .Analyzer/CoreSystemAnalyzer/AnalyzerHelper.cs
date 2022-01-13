using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace CoreSystemAnalyzer
{
    internal static class AnalyzerHelper
    {
        public static LocalizableResourceString GetString(string description)
        {
            return new LocalizableResourceString(description, Resources.ResourceManager, typeof(Resources));
        }

        /// <summary>
        /// Helper method for reporting triggered diagnostics
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="rule"></param>
        public static void RegisterDiagnostic<T>(SyntaxNodeAnalysisContext context, DiagnosticDescriptor rule) where T : SyntaxNode
        {
            var expression = context.Node as T;
            var diagnostic = Diagnostic.Create(rule, expression.GetLocation(), expression.ToString());
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Helper method for reporting a triggered diagnostic with a specific location
        /// </summary>
        public static void RegisterDiagnostic<T>(SyntaxNodeAnalysisContext context, Location location, DiagnosticDescriptor rule) where T : SyntaxNode
        {
            var expression = context.Node as T;
            var diagnostic = Diagnostic.Create(rule, location, expression.ToString());
            context.ReportDiagnostic(diagnostic);
        }

        public static string ToDiagnosticId(this DiagnosticId diagnosticId)
            => $"SD{(int)diagnosticId:D4}";
    }

    internal static class TypeofHelper
    {
        private static Dictionary<string, string> mValueTypes = new Dictionary<string, string>()
        {
            {"void",    "kVoidType" },
            {"bool",    "kBoolType" },
            {"int",     "kIntType" },
            {"long",    "kLongType" },
            {"float",   "kFloatType" },
            {"double",  "kDoubleType" },
            {"Real",    "kRealType" },
            {"string",  "kStringType" },
        };

        /// <summary>
        /// Helper for identifying special value types that we want to handle differently
        /// </summary>
        /// <param name="syntax"></param>
        /// <returns></returns>
        internal static bool IsValueType(TypeSyntax syntax)
        {
            var stringType = syntax.ToString();
            return mValueTypes.ContainsKey(stringType);
        }

        /// <summary>
        /// Condition used to capture typeof(T).Name and typeof(T).FullName expressions
        /// This is a bit complicated as we need to ensure the algorithm is not greedy
        /// </summary>
        /// <param name="node"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        internal static bool Condition(SyntaxNode node, string memberName)
        {
            var expressionNode = node as MemberAccessExpressionSyntax;
            if (expressionNode == null) return false;

            // Ensure that we are capturing only one level deep (so avoid going multiple levels deep to capture a typeof expression)
            var typeofExpression = expressionNode.ChildNodes().Where(n => n.IsKind(SyntaxKind.TypeOfExpression));
            if (typeofExpression == null || typeofExpression.Count() == 0) return false;

            // Reject nested access expressions (e.g. typeof(T).Name.Something)
            if (expressionNode.Parent is MemberAccessExpressionSyntax) return false;

            // Capture only member specified by memberName (i.e. Name or FullName)
            var member = expressionNode.DescendantTokens().Where(t => t.ValueText == memberName);

            return member.Count() > 0;
        }

        ///// <summary>
        ///// Helper for replacing captured typeof expression with ReflectionTypeHelpers.GetType
        ///// </summary>
        ///// <param name="document"></param>
        ///// <param name="expression">typeof expression</param>
        ///// <param name="c"></param>
        ///// <param name="replacementMember">ReflectionTypeHelpers.GetType.kType"/></param>
        ///// <returns></returns>
        //internal static async Task<Document> ReplaceTypeof(Document document, object expression, CancellationToken c, string replacementMember)
        //{
        //    return await ReplaceExpression<TypeOfExpressionSyntax>(document, expression, c, replacementMember);
        //}

        ///// <summary>
        ///// Helper for replacing captured typeof member with ReflectionTypeHelpers.GetType member
        ///// </summary>
        ///// <param name="document"></param>
        ///// <param name="expression">typeof expression</param>
        ///// <param name="c"></param>
        ///// <param name="replacementMember">ReflectionTypeHelpers.GetType member"/></param>
        ///// <returns></returns>
        //internal static async Task<Document> ReplaceTypeofMember(Document document, object expression, CancellationToken c, string replacementMember)
        //{
        //    return await ReplaceExpression<MemberAccessExpressionSyntax>(document, expression, c, replacementMember);
        //}

        ///// <summary>
        ///// Helper for replacing a GetHashCode call with kFullNameHashCode
        ///// </summary>
        ///// <param name="document"></param>
        ///// <param name="expression"></param>
        ///// <param name="c"></param>
        ///// <returns></returns>
        //internal static async Task<Document> ReplaceTypeofHashCode(Document document, object expression, CancellationToken c)
        //{
        //    return await ReplaceExpression<InvocationExpressionSyntax>(document, expression, c, "kFullNameHashCode");
        //}

        //internal static async Task<Document> ReplaceTypeofValueType(Document document, object expression, CancellationToken c)
        //{
        //    var invocationExpression = expression as TypeOfExpressionSyntax;
        //    if (invocationExpression == null) return document;

        //    var typeofType = ExtractTypeofType(invocationExpression);
        //    if (typeofType == null) return document;

        //    var typeString = typeofType.ToString();
        //    if (!mValueTypes.ContainsKey(typeString)) return document;

        //    // ReflectionTypeHelpers.k*Type etc
        //    var newExpression = CodeFixHelpers.CreateMemberAccess("ReflectionTypeHelpers", mValueTypes[typeString]);

        //    //Return modified document
        //    var newDoc = await CodeFixHelpers.ReplaceNode(document, invocationExpression, newExpression, c);
        //    return newDoc;
        //}

        ///// <summary>
        ///// Helper for building up the full ReflectionTypeHelper expression
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="document"></param>
        ///// <param name="expression"></param>
        ///// <param name="c"></param>
        ///// <param name="replacementIdentifier"></param>
        ///// <returns></returns>
        //private static async Task<Document> ReplaceExpression<T>(Document document, object expression, CancellationToken c, string replacementIdentifier) where T : SyntaxNode
        //{
        //    var invocationExpression = expression as T;
        //    if (invocationExpression == null) return document;

        //    var typeofType = ExtractTypeofType(invocationExpression);
        //    if (typeofType == null) return document;

        //    // GetType<T>
        //    var getType = CodeFixHelpers.CreateGenericType("GetType", new[] { typeofType });
        //    // ReflectionTypeHelpers.GetType<T>
        //    var innerExpression = CodeFixHelpers.CreateMemberAccess("ReflectionTypeHelpers", getType.ToString());
        //    // ReflectionTypeHelpers.GetType<T>.replacementIdentifier
        //    var outerExpression = CodeFixHelpers.CreateMemberAccess(innerExpression.ToString(), replacementIdentifier);

        //    //Return modified document
        //    var newDoc = await CodeFixHelpers.ReplaceNode(document, invocationExpression, outerExpression, c);
        //    return newDoc;
        //}

        /// <summary>
        /// Helper for extracting the nested TypeSyntax from a TypeOfExpression node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static TypeSyntax ExtractTypeofType(SyntaxNode node)
        {
            try
            {
                var typeofNode = node.DescendantNodesAndSelf()
                                    .Where(n => n.IsKind(SyntaxKind.TypeOfExpression))
                                    .First();

                var typeofExpression = typeofNode as TypeOfExpressionSyntax;
                return typeofExpression.Type;
            }
            catch
            {
                return null;
            }
        }
    }

    //class CodeFixHelpers
    //{
    //    public delegate CompilationUnitSyntax TrasformFunc(CompilationUnitSyntax arg);
    //    /// <summary>
    //    /// Common code used by all code fixes to easily extract the parent of the diagnostic triggered
    //    /// </summary>
    //    /// <typeparam name="T"></typeparam>
    //    /// <param name="context"></param>
    //    /// <param name="diagnostic"></param>
    //    /// <returns></returns>
    //    public static async Task<T> FirstAncestorFromCodeFixContext<T>(CodeFixContext context, Diagnostic diagnostic)
    //        where T : SyntaxNode
    //    {
    //        // Gets the root node of the syntax tree asynchronously.
    //        var root = await context
    //                            .Document.
    //                            GetSyntaxRootAsync(context.CancellationToken).
    //                            ConfigureAwait(false);          // Do not capture context (http://blog.stephencleary.com/2012/02/async-and-await.html#avoiding-context)

    //        var diagnosticSpan = diagnostic
    //                            .Location
    //                            .SourceSpan;                    // Source code location of the triggered diagnostic

    //        var node = root
    //                    .FindToken(diagnosticSpan.Start)        // Finds a descendant token of this node whose span includes the supplied position.   
    //                    .Parent                                 // Grab parent node (the node that contains this token in its children list
    //                    .FirstAncestorOrSelf<T>();              // Get first node of type T

    //        return node;
    //    }

    //    /// <summary>
    //    /// Convenience method for replacing a single node
    //    /// </summary>
    //    /// <param name="document"></param>
    //    /// <param name="oldNode"></param>
    //    /// <param name="newNode"></param>
    //    /// <param name="c"></param>
    //    /// <param name="moveTrivia"></param>
    //    /// <returns></returns>
    //    public static async Task<Document> ReplaceNode(Document document, SyntaxNode oldNode, SyntaxNode newNode, CancellationToken c, bool moveTrivia = false)
    //    {
    //        var action = new List<TrasformFunc>()
    //        {
    //            x => ReplaceNode(x, oldNode, newNode, moveTrivia)
    //        };
    //        return await UpdateDocument(document, action, c);
    //    }

    //    /// <summary>
    //    /// Replace node helper that preserves trivia and enables formatting
    //    /// </summary>
    //    /// <param name="root"></param>
    //    /// <param name="oldNode"></param>
    //    /// <param name="newNode"></param>
    //    /// <param name="moveTrivia"></param>
    //    /// <returns></returns>
    //    public static CompilationUnitSyntax ReplaceNode(CompilationUnitSyntax root, SyntaxNode oldNode, SyntaxNode newNode, bool moveTrivia = false)
    //    {
    //        if (moveTrivia)
    //        {
    //            newNode = newNode.WithLeadingTrivia(
    //                            oldNode.GetLeadingTrivia()
    //                            .AddRange(newNode.GetLeadingTrivia())       // Combine leading trivia from both nodes
    //                      ).WithTrailingTrivia(
    //                            oldNode.GetTrailingTrivia()
    //                            .AddRange(newNode.GetTrailingTrivia()));    // Combine trailing trivia from both nodes
    //        }

    //        // It’s good practice to tag new nodes you create with the “Formatter” annotation, 
    //        // which informs the code fix engine that you want your new node formatted according to the end user’s code style settings
    //        newNode = newNode.WithAdditionalAnnotations(Formatter.Annotation);

    //        var newRoot = root.ReplaceNode(oldNode, newNode);
    //        return newRoot;
    //    }

    //    /// <summary>
    //    /// Add using statement helper
    //    /// </summary>
    //    /// <param name="root"></param>
    //    /// <param name="usingName"></param>
    //    /// <returns></returns>
    //    public static CompilationUnitSyntax AddUsings(CompilationUnitSyntax root, string usingName)
    //    {
    //        var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(usingName))
    //                            .WithAdditionalAnnotations(Formatter.Annotation);
    //        root = root.AddUsings(newUsing);
    //        return root;
    //    }

    //    /// <summary>
    //    /// Apply a list of functors 
    //    /// </summary>
    //    /// <param name="document"></param>
    //    /// <param name="actions"></param>
    //    /// <param name="c"></param>
    //    /// <returns></returns>
    //    public static async Task<Document> UpdateDocument(Document document, IEnumerable<TrasformFunc> actions, CancellationToken c)
    //    {
    //        var rootNode = await document.GetSyntaxRootAsync(c);
    //        var root = rootNode as CompilationUnitSyntax;
    //        var newRoot = root;
    //        foreach (var action in actions)
    //        {
    //            newRoot = action(newRoot);
    //        }
    //        var newDocument = document.WithSyntaxRoot(newRoot);
    //        return newDocument;
    //    }

    //    /// <summary>
    //    /// Helper to create a indentifier<T> expression
    //    /// </summary>
    //    /// <param name="identifier"></param>
    //    /// <param name="arguments"></param>
    //    /// <returns></returns>
    //    internal static TypeSyntax CreateGenericType(string identifier, params TypeSyntax[] arguments)
    //    {
    //        return SyntaxFactory.GenericName(
    //                    SyntaxFactory.Identifier(identifier),
    //                    SyntaxFactory.TypeArgumentList(
    //                        SyntaxFactory.SeparatedList(arguments)
    //                    )
    //                ).WithAdditionalAnnotations(Formatter.Annotation);
    //    }

    //    /// <summary>
    //    /// Helper to create a simple member access expression: objIdentifier.memberIdentofier
    //    /// </summary>
    //    /// <param name="objIdentifier"></param>
    //    /// <param name="memberIdentifier"></param>
    //    /// <returns></returns>
    //    internal static MemberAccessExpressionSyntax CreateMemberAccess(string objIdentifier, string memberIdentifier)
    //    {
    //        return SyntaxFactory.MemberAccessExpression(
    //                    SyntaxKind.SimpleMemberAccessExpression,
    //                    SyntaxFactory.IdentifierName(objIdentifier),
    //                    SyntaxFactory.IdentifierName(memberIdentifier)
    //               ).WithAdditionalAnnotations(Formatter.Annotation);
    //    }
    //}
}
