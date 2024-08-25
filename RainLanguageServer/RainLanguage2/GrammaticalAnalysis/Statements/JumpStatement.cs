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
    }
    internal class BreakStatement(TextRange symbol, LoopStatement? loop, Expression? condition) : JumpStatement(symbol, loop, condition) { }
    internal class ContinueStatement(TextRange symbol, LoopStatement? loop, Expression? condition) : JumpStatement(symbol, loop, condition) { }
}
