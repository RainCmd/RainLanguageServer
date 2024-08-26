namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class QuestionNullExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression left;
        public readonly Expression right;
        public override bool Valid => left.Valid && right.Valid;

        public QuestionNullExpression(TextRange symbol, Expression left, Expression right) : base(left.range & right.range, left.tuple)
        {
            this.symbol = symbol;
            this.left = left;
            this.right = right;
            attribute = left.attribute & ~ExpressionAttribute.Assignable;
        }
        public override void Read(ExpressionParameter parameter)
        {
            left.Read(parameter);
            right.Read(parameter);
        }
    }
}
