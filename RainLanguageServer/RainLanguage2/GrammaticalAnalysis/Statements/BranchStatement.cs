
namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class BranchStatement(TextRange ifSymbol, Expression condition, List<TextRange> group) : Statement
    {
        public readonly TextRange ifSymbol = ifSymbol;
        public TextRange? elseSymbol;
        public readonly Expression condition = condition;
        public BlockStatement? trueBranch, falseBranch;
        public readonly List<TextRange> group = group;

        public override void Operator(Action<Expression> action)
        {
            action(condition);
            trueBranch?.Operator(action);
            falseBranch?.Operator(action);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (condition.range.Contain(position)) return action(condition);
            if (trueBranch != null && trueBranch.range.Contain(position)) return trueBranch.Operator(position, action);
            if (falseBranch != null && falseBranch.range.Contain(position)) return falseBranch.Operator(position, action);
            return false;
        }
    }
}
