namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
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
        public override void Read(StatementParameter parameter) => result.Read(parameter);
    }
}
