namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class TryStatement:Statement
    {
        public readonly struct CatchBlock(Expression condition, BlockStatement block)
        {
            public readonly Expression condition = condition;
            public readonly BlockStatement block = block;
        }
        public readonly BlockStatement? tryBlock;
        public readonly List<CatchBlock> catchBlocks=[];
        public readonly BlockStatement? finallyBlock;
        public readonly List<TextRange> group = [];
    }
}
