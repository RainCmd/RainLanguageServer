

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

        public override void Operator(Action<Expression> action)
        {
            if (condition != null) action(condition);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (condition != null && condition.range.Contain(position)) return action(condition);
            return false;
        }
        public override bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos)
        {
            if (group != null && symbol.Contain(position))
            {
                InfoUtility.HighlightGroup(group, infos);
                return true;
            }
            return false;
        }
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.KeywordCtrl, symbol);
            condition?.CollectSemanticToken(manager, collector);
        }
    }
    internal class BreakStatement(TextRange symbol, Expression? condition) : JumpStatement(symbol, condition) { }
    internal class ContinueStatement(TextRange symbol, Expression? condition) : JumpStatement(symbol, condition) { }
}
