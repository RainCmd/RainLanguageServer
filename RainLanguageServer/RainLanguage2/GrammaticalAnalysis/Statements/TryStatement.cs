namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class TryStatement:Statement
    {
        public readonly struct CatchBlock(Expression condition, BlockStatement block)
        {
            public readonly TextRange catchSymbol;
            public readonly Expression condition = condition;
            public readonly BlockStatement block = block;
        }
        public readonly TextRange trySymbol;
        public readonly TextRange? finallySymbol;
        public BlockStatement? tryBlock;
        public readonly List<CatchBlock> catchBlocks=[];
        public BlockStatement? finallyBlock;
        public readonly List<TextRange> group = [];
    }
}
