namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class InvokerExpression : Expression
    {
        public readonly BracketExpression parameters;
        public override bool Valid => true;

        public InvokerExpression(TextRange range, Tuple tuple, BracketExpression parameters, Manager.KernelManager manager) : base(range, tuple)
        {
            this.parameters = parameters;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
        }
    }
}
