namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
{
    internal class JumpStatement : Statement
    {
        public readonly TextRange symbol;
        public readonly Expression? condition;
        public List<TextRange>? group;

        public JumpStatement(TextRange symbol, Expression? condition)
        {
            range = condition == null ? symbol : symbol & condition.range;
            this.symbol = symbol;
            this.condition = condition;
        }

        protected override void InternalOperator(Action<Expression> action)
        {
            if (condition != null) action(condition);
        }
        public override void Operator(Action<Statement> action) => action(this);
        protected override bool InternalOperator(TextPosition position, ExpressionOperator action) => condition != null && condition.range.Contain(position) && action(condition);
        public override bool Operator(TextPosition position, StatementOperator action) => action(this);
        protected override void InternalOperator(TextRange range, Action<Expression> action)
        {
            if (condition != null && condition.range.Overlap(range))
                action(condition);
        }
        public override void Operator(TextRange range, Action<Statement> action) => action(this);

        protected override bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos)
        {
            if (group != null && symbol.Contain(position))
            {
                InfoUtility.HighlightGroup(group, infos);
                return true;
            }
            return false;
        }
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.KeywordCtrl, symbol);
        protected override void InternalCollectInlayHint(Manager manager, List<InlayHintInfo> infos)
        {
            if (condition == null)
                infos.Add(new InlayHintInfo($" {KeyWords.TRUE}", symbol.end, InlayHintInfo.Kind.Paramter));
        }
    }
    internal class BreakStatement(TextRange symbol, Expression? condition) : JumpStatement(symbol, condition) { }
    internal class ContinueStatement(TextRange symbol, Expression? condition) : JumpStatement(symbol, condition) { }
}
