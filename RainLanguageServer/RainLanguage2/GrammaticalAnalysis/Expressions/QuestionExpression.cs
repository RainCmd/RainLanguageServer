namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class QuestionExpression : Expression
    {
        public readonly TextRange questionSymbol;
        public readonly TextRange? elseSymbol;
        public readonly Expression condition;
        public readonly Expression left;
        public readonly Expression? right;
        public override bool Valid => left.Valid;

        public QuestionExpression(TextRange range, TextRange questionSymbol, TextRange? elseSymbol, Expression condition, Expression left, Expression? right) : base(range, left.tuple)
        {
            this.questionSymbol = questionSymbol;
            this.elseSymbol = elseSymbol;
            this.condition = condition;
            this.left = left;
            this.right = right;
            attribute = left.attribute & ~ExpressionAttribute.Assignable;
        }
    }
}
