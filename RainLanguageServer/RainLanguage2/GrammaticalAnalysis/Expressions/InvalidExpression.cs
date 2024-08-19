namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class InvalidExpression : Expression
    {
        public readonly IList<Expression> expressions;
        public override bool Valid => false;
        public InvalidExpression(TextRange range) : base(range, Tuple.Empty)
        {
            expressions = [];
            attribute = ExpressionAttribute.Invalid;
        }
        public InvalidExpression(params Expression[] expressions) : this((IList<Expression>)expressions) { }
        public InvalidExpression(IList<Expression> expressions) : base(expressions[0].range & expressions[^1].range, Tuple.Empty)
        {
            this.expressions = expressions;
            attribute = ExpressionAttribute.Invalid;
        }
        public InvalidExpression(Expression expression, Tuple tuple) : base(expression.range, tuple)
        {
            expressions = [expression];
            attribute = ExpressionAttribute.Invalid;
        }
    }
    internal class InvalidKeyworldExpression(TextRange range) : InvalidExpression(range)
    {

    }
    internal class InvalidOperationExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression? parameters;
        public override bool Valid => false;

        public InvalidOperationExpression(TextRange range, TextRange symbol, Expression? parameters = null) : base(range, Tuple.Empty)
        {
            this.symbol = symbol;
            this.parameters = parameters;
            attribute = ExpressionAttribute.Invalid;
        }
    }
}
