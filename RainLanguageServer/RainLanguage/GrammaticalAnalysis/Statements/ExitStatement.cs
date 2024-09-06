namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
{
    internal class ExitStatement : Statement
    {
        public readonly TextRange symbol;
        public readonly Expression expression;
        public readonly List<TextRange> group;

        public ExitStatement(TextRange symbol, Expression expression, List<TextRange> group)
        {
            range = symbol & expression.range;
            this.symbol = symbol;
            this.expression = expression;
            this.group = group;
            group.Add(symbol);
        }
        protected override void InternalOperator(Action<Expression> action) => action(expression);
        public override void Operator(Action<Statement> action) => action(this);
        protected override bool InternalOperator(TextPosition position, ExpressionOperator action) => expression.range.Contain(position) && action(expression);
        public override bool Operator(TextPosition position, StatementOperator action) => action(this);

        protected override bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos)
        {
            if (symbol.Contain(position))
            {
                InfoUtility.HighlightGroup(group, infos);
                return true;
            }
            return true;
        }
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.KeywordCtrl, symbol);
    }
}
