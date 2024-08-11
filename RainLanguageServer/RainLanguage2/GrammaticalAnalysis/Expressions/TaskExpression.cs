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
}
