namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class CastExpression : Expression
    {
        public readonly TypeExpression type;
        public readonly TextRange symbol;
        public readonly Expression expression;
        public override bool Valid => true;

        public CastExpression(TextRange range, TypeExpression type, TextRange symbol, Expression expression, Manager.KernelManager manager) : base(range, type.type)
        {
            this.type = type;
            this.symbol = symbol;
            this.expression = expression;
            attribute = ExpressionAttribute.Value | type.type.GetAttribute(manager);
        }

    }
    internal class TupleCastExpression : Expression
    {
        public readonly Expression expression;
        public override bool Valid => true;
        public TupleCastExpression(Expression expression, Tuple tuple, Manager.KernelManager manager) : base(expression.range, tuple)
        {
            this.expression = expression;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
            attribute |= expression.attribute & ~ExpressionAttribute.Assignable;
        }
    }
}
