namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class OperationExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly AbstractCallable callable;
        public readonly Expression parameters;
        public override bool Valid => true;

        public OperationExpression(TextRange range, TextRange symbol, AbstractCallable callable, Expression parameters, Manager.KernelManager manager) : base(range, callable.returns)
        {
            this.symbol = symbol;
            this.callable = callable;
            this.parameters = parameters;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
        }
    }
}
