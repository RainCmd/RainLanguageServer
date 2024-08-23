namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class BranchStatement : Statement
    {
        public TextRange ifSymbol;
        public TextRange? elseSymbol;
        public readonly Expression condition;
        public BlockStatement? trueBranch, falseBranch;
        public readonly List<TextRange> group;
    }
}
