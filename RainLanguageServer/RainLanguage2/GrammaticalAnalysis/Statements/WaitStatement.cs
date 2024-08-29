
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
        public override void Operator(Action<Expression> action)
        {
            if (expression != null) action(expression);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (expression != null && expression.range.Contain(position)) return action(expression);
            return false;
        }
    }
}
