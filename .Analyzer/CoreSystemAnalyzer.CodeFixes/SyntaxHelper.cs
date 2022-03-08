using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreSystemAnalyzer
{
    public static class SyntaxHelper
    {
        // https://stackoverflow.com/questions/53338372/function-which-deletes-all-code-comments
        class CommentResolver : CSharpSyntaxRewriter
        {
            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                switch (trivia.Kind())
                {
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                        return SyntaxFactory.CarriageReturn;
                    default:
                        return trivia;
                }
                return base.VisitTrivia(trivia);
            }
        }
        private static CommentResolver CommentRemover = new CommentResolver();

        public static UsingDirectiveSyntax ToUsingSyntax(in string namespaceName)
        {
            return SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(namespaceName));
        }

        public static async Task<CompilationUnitSyntax> GetSyntaxRootAsync(Document document, CancellationToken token = default)
        {
            CompilationUnitSyntax root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(token);

            return root;
        }

        /// <summary>
        /// <paramref name="span"/> 의 텍스트와 동일한 using directive 가 존재하는지 반환합니다.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="span"></param>
        /// <returns></returns>
        public static bool HasUsingSyntax(this CompilationUnitSyntax t, TextSpan span)
        {
            foreach (var item in t.Usings)
            {
                if (item.Span.Equals(span)) return true;
            }

            return false;
        }

        public static CompilationUnitSyntax ConcreteUsingSyntax(this CompilationUnitSyntax t, UsingDirectiveSyntax syntax)
        {
            CompilationUnitSyntax root = t;

            //bool found = false;
            // https://elbruno.com/2015/08/17/vs2015-delete-all-comments-in-a-file-with-roslyn/
            for (int i = root.Usings.Count - 1; i >= 0; i--)
            {
                UsingDirectiveSyntax newUsing = (UsingDirectiveSyntax)CommentRemover.Visit(root.Usings[i]);
                if (root.Usings[i].Equals(newUsing))
                {
                    continue;
                }



                //UsingDirectiveSyntax newDirective;
                //SyntaxList<UsingDirectiveSyntax> newUsings;
                //SyntaxTriviaList leadingTrivia = root.Usings[i].GetLeadingTrivia();
                //for (int j = leadingTrivia.Count - 1; j >= 0; j--)
                //{
                //    if (!leadingTrivia[j].IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                //        !leadingTrivia[j].IsKind(SyntaxKind.MultiLineCommentTrivia))
                //    {
                //        continue;
                //    }

                //    if (leadingTrivia[j].Span.Contains(syntax.Span))
                //    {
                //        leadingTrivia = leadingTrivia.RemoveAt(j);
                //        newDirective = root.Usings[i].WithLeadingTrivia(leadingTrivia);
                //        newUsings = root.Usings.Replace(root.Usings[i], newDirective);

                //        newUsings = newUsings.Insert(i, syntax);
                //        root = root.WithUsings(newUsings);

                //        found = true;
                //        break;
                //    }
                //}

                //if (found) break;
            }

            //if (!found)
            //{
            //    root = root.AddUsings(syntax);
            //}

            return root;
        }
        
        //static SyntaxTrivia EmptyTriviaNodes(SyntaxTrivia arg1, SyntaxTrivia arg2)
        //{
        //    if (arg1.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
        //        arg1.IsKind(SyntaxKind.MultiLineCommentTrivia))
        //    {
        //        if (arg1.Span.Contains()
        //        arg2 = SyntaxFactory.CarriageReturn;
        //    }
        //    else arg2 = arg1;

        //    return arg2;
        //}
    }
}
