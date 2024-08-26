namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class AssignmentExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression left;
        public readonly Expression right;
        public override bool Valid => true;
        public AssignmentExpression(TextRange range, TextRange symbol, Expression left, Expression right) : base(range, left.tuple)
        {
            this.symbol = symbol;
            this.left = left;
            this.right = right;
            attribute = left.attribute & ~ExpressionAttribute.Assignable;
        }
        public override void Read(ExpressionParameter parameter)
        {
            if (left.attribute.ContainAny(ExpressionAttribute.Assignable)) left.Write(parameter);
            right.Read(parameter);
        }
    }
}
