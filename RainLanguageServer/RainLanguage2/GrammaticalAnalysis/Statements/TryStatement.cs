
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
        public override void Operator(Action<Expression> action)
        {
            tryBlock?.Operator(action);
            foreach (var catchBlock in catchBlocks)
            {
                if (catchBlock.expression != null) action(catchBlock.expression);
                catchBlock.block.Operator(action);
            }
            finallyBlock?.Operator(action);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (tryBlock != null && tryBlock.range.Contain(position)) return tryBlock.Operator(position, action);
            foreach (var catchBlock in catchBlocks)
            {
                if (catchBlock.expression != null && catchBlock.expression.range.Contain(position)) return action(catchBlock.expression);
                if (catchBlock.block.range.Contain(position)) return catchBlock.block.Operator(position, action);
            }
            if (finallyBlock != null && finallyBlock.range.Contain(position)) return finallyBlock.Operator(position, action);
            return false;
        }
    }
}
