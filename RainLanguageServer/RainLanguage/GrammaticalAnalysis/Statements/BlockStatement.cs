namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
{
    internal class BlockStatement : Statement
    {
        public readonly List<Statement> statements = [];
        public BlockStatement(TextRange range)
        {
            base.range = range;
        }

        public override void Operator(Action<Statement> action)
        {
            foreach (var statement in statements) statement.Operator(action);
            action(this);
        }
        public override bool Operator(TextPosition position, StatementOperator action)
        {
            foreach (var statement in statements)
                if (statement.range.Contain(position))
                    return statement.Operator(position, action);
            return action(this);
        }
        public override void Operator(TextRange range, Action<Statement> action)
        {
            foreach (var statement in statements)
                if (statement.range.Overlap(range))
                    statement.Operator(range, action);
            action(this);
        }
    }
}
