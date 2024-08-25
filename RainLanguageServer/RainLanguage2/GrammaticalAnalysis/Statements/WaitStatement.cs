namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
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
    }
}
