namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
{
    internal class BranchStatement(TextRange ifSymbol, Expression condition, List<TextRange> group) : Statement
    {
        public readonly TextRange ifSymbol = ifSymbol;
        public TextRange? elseSymbol;
        public readonly Expression condition = condition;
        public BlockStatement? trueBranch, falseBranch;
        public readonly List<TextRange> group = group;

        protected override void InternalOperator(Action<Expression> action) => action(condition);
        public override void Operator(Action<Statement> action)
        {
            trueBranch?.Operator(action);
            falseBranch?.Operator(action);
            action(this);
        }
        protected override bool InternalOperator(TextPosition position, ExpressionOperator action)
        {
            if (condition.range.Contain(position)) return action(condition);
            return false;
        }
        public override bool Operator(TextPosition position, StatementOperator action)
        {
            if (trueBranch != null && trueBranch.range.Contain(position)) return trueBranch.Operator(position, action);
            if (falseBranch != null && falseBranch.range.Contain(position)) return falseBranch.Operator(position, action);
            return action(this);
        }
        protected override void InternalOperator(TextRange range, Action<Expression> action)
        {
            if (condition.range.Overlap(range)) action(condition);
        }
        public override void Operator(TextRange range, Action<Statement> action)
        {
            if (trueBranch != null && trueBranch.range.Overlap(range)) trueBranch.Operator(range, action);
            if (falseBranch != null && falseBranch.range.Overlap(range)) falseBranch.Operator(range, action);
            action(this);
        }

        protected override bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos)
        {
            if (ifSymbol.Contain(position) || (elseSymbol != null && elseSymbol.Value.Contain(position)))
            {
                InfoUtility.HighlightGroup(group, infos);
                return true;
            }
            return false;
        }
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.KeywordCtrl, ifSymbol);
            if (elseSymbol != null) collector.Add(DetailTokenType.KeywordCtrl, elseSymbol.Value);
        }
    }
}
