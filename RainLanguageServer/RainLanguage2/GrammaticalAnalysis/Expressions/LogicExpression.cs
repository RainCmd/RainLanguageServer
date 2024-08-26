namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class LogicExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression left, right;
        public override bool Valid => true;

        public LogicExpression(TextRange range, TextRange symbol, Expression left, Expression right, Manager.KernelManager manager) : base(range, manager.BOOL)
        {
            this.symbol = symbol;
            this.left = left;
            this.right = right;
            attribute = ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            left.Read(parameter);
            right.Read(parameter);
        }
    }
}
