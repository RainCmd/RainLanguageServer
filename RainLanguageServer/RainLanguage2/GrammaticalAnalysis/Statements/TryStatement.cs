namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class TryStatement(TextRange trySymbol) : Statement
    {
        public readonly struct CatchBlock(TextRange catchSymbol, Expression? expression, BlockStatement block)
        {
            public readonly TextRange catchSymbol = catchSymbol;
            public readonly Expression? expression = expression;
            public readonly BlockStatement block = block;
        }
        public readonly TextRange trySymbol = trySymbol;
        public readonly TextRange? finallySymbol;
        public BlockStatement? tryBlock;
        public readonly List<CatchBlock> catchBlocks = [];
        public BlockStatement? finallyBlock;
        public readonly List<TextRange> group = [];
        public override void Read(StatementParameter parameter)
        {
            tryBlock?.Read(parameter);
            foreach (var catchBlock in catchBlocks)
            {
                catchBlock.expression?.Read(parameter);
                catchBlock.block.Read(parameter);
            }
            finallyBlock?.Read(parameter);
        }
    }
}
