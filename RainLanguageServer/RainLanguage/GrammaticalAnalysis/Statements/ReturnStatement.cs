namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
{
    internal class ReturnStatement : Statement
    {
        public readonly TextRange symbol;
        public readonly Expression result;
        public readonly List<TextRange> group;
        public ReturnStatement(TextRange symbol, Expression result, List<TextRange> group)
        {
            range = symbol & result.range;
            this.symbol = symbol;
            this.result = result;
            this.group = group;
            group.Add(symbol);
        }

        protected override void InternalOperator(Action<Expression> action) => action(result);
        public override void Operator(Action<Statement> action) => action(this);
        protected override bool InternalOperator(TextPosition position, ExpressionOperator action) => result.range.Contain(position) && action(result);
        public override bool Operator(TextPosition position, StatementOperator action) => action(this);

        protected override bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos)
        {
            if (symbol.Contain(position))
            {
                InfoUtility.HighlightGroup(group, infos);
                return true;
            }
            return false;
        }
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.KeywordCtrl, symbol);
    }
}
