namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class InvokerExpression : Expression
    {
        public readonly Expression parameters;
        public override bool Valid => true;

        public InvokerExpression(TextRange range, Tuple tuple, Expression parameters, Manager manager) : base(range, tuple)
        {
            this.parameters = parameters;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
        }
    }
}
