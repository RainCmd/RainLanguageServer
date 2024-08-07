namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class InvalidExpression : Expression
    {
        public readonly IList<Expression> expressions;
        public override bool Valid => false;
        public InvalidExpression(params Expression[] expressions) : this((IList<Expression>)expressions) { }
        public InvalidExpression(IList<Expression> expressions) : base(expressions[0].range & expressions[^1].range, Tuple.Empty)
        {
            this.expressions = expressions;
            attribute = ExpressionAttribute.Invalid;
        }
    }
}
