namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class TaskCreateExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly InvokerExpression invoker;
        public override bool Valid => true;

        public TaskCreateExpression(TextRange range, Type type, TextRange symbol, InvokerExpression invoker, Manager.KernelManager manager) : base(range, type)
        {
            this.symbol = symbol;
            this.invoker = invoker;
            attribute = ExpressionAttribute.Value | type.GetAttribute(manager);
        }
    }
    internal class TaskEvaluationExpression : Expression
    {
        public readonly Expression source;
        public readonly BracketExpression indices;
        public override bool Valid => true;
        public TaskEvaluationExpression(TextRange range, Tuple tuple, Expression source, BracketExpression indices, Manager.KernelManager manager) : base(range, tuple)
        {
            this.source = source;
            this.indices = indices;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
        }
    }
}
