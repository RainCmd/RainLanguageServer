namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
{
    internal class WaitStatement : Statement
    {
        public readonly TextRange symbol;
        public readonly Expression? expression;
        public readonly List<TextRange> group;

        public WaitStatement(TextRange symbol, Expression? expression, List<TextRange> group)
        {
            range = expression == null ? symbol : symbol & expression.range;
            this.symbol = symbol;
            this.expression = expression;
            this.group = group;
            group.Add(symbol);
        }
        protected override void InternalOperator(Action<Expression> action)
        {
            if (expression != null) action(expression);
        }
        public override void Operator(Action<Statement> action) => action(this);
        protected override bool InternalOperator(TextPosition position, ExpressionOperator action) => expression != null && expression.range.Contain(position) && action(expression);
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
        protected override void InternalCollectInlayHint(Manager manager, List<InlayHintInfo> infos)
        {
            if (expression == null)
                infos.Add(new InlayHintInfo(" 1", symbol.end));
        }
    }
}
