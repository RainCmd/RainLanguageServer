
namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class BracketExpression : Expression
    {
        public readonly TextRange left, right;
        public readonly Expression expression;
        public override bool Valid => expression.Valid;
        public BracketExpression(TextRange left, TextRange right, Expression expression) : base(left & right, expression.tuple)
        {
            this.left = left;
            this.right = right;
            this.expression = expression;
            attribute = expression.attribute;
        }
        public override bool TryEvaluateIndices(List<long> indices)
        {
            return expression.TryEvaluateIndices(indices);
        }
    }
}
