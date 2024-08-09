namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
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
