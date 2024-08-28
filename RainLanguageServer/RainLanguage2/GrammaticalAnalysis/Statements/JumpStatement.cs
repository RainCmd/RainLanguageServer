
namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class JumpStatement : Statement
    {
        public readonly TextRange symbol;
        public readonly LoopStatement? loop;
        public readonly Expression? condition;

        public JumpStatement(TextRange symbol, LoopStatement? loop, Expression? condition)
        {
            range = condition == null ? symbol : symbol & condition.range;
            this.symbol = symbol;
            this.loop = loop;
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
    }
    internal class BreakStatement(TextRange symbol, LoopStatement? loop, Expression? condition) : JumpStatement(symbol, loop, condition) { }
    internal class ContinueStatement(TextRange symbol, LoopStatement? loop, Expression? condition) : JumpStatement(symbol, loop, condition) { }
}
