namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
{
    internal class ExpressionStatement : Statement
    {
        public readonly Expression expression;

        public ExpressionStatement(Expression expression)
        {
            range = expression.range;
            this.expression = expression;
        }

        protected override void InternalOperator(Action<Expression> action) => action(expression);
        public override void Operator(Action<Statement> action) => action(this);
        protected override bool InternalOperator(TextPosition position, ExpressionOperator action) => expression.range.Contain(position) && action(expression);
        public override bool Operator(TextPosition position, StatementOperator action) => action(this);
        protected override void InternalOperator(TextRange range, Action<Expression> action)
        {
            if (expression.range.Overlap(range))
                action(expression);
        }
        public override void Operator(TextRange range, Action<Statement> action) => action(this);
    }
}
