namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
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
        public TextRange? finallySymbol;
        public BlockStatement? tryBlock;
        public readonly List<CatchBlock> catchBlocks = [];
        public BlockStatement? finallyBlock;
        public readonly List<TextRange> group = [trySymbol];
        protected override void InternalOperator(Action<Expression> action)
        {
            foreach (var catchBlock in catchBlocks)
                if (catchBlock.expression != null) action(catchBlock.expression);
        }
        public override void Operator(Action<Statement> action)
        {
            tryBlock?.Operator(action);
            foreach (var catchBlock in catchBlocks)
                catchBlock.block.Operator(action);
            finallyBlock?.Operator(action);
            action(this);
        }
        protected override bool InternalOperator(TextPosition position, ExpressionOperator action)
        {
            foreach (var catchBlock in catchBlocks)
                if (catchBlock.expression != null && catchBlock.expression.range.Contain(position)) return action(catchBlock.expression);
            return false;
        }
        public override bool Operator(TextPosition position, StatementOperator action)
        {
            if (tryBlock != null && tryBlock.range.Contain(position)) return tryBlock.Operator(position, action);
            foreach (var catchBlock in catchBlocks)
                if (catchBlock.block.range.Contain(position))
                    return catchBlock.block.Operator(position, action);
            if (finallyBlock != null && finallyBlock.range.Contain(position)) return finallyBlock.Operator(position, action);
            return action(this);
        }

        protected override bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos)
        {
            if (trySymbol.Contain(position))
            {
                InfoUtility.HighlightGroup(group, infos);
                return true;
            }
            if (finallySymbol != null && finallySymbol.Value.Contain(position))
            {
                InfoUtility.HighlightGroup(group, infos);
                return true;
            }
            foreach (var catchBlock in catchBlocks)
            {
                if (catchBlock.catchSymbol.Contain(position))
                {
                    InfoUtility.HighlightGroup(group, infos);
                    return true;
                }
            }
            return false;
        }
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.KeywordCtrl, trySymbol);
            if (finallySymbol != null) collector.Add(DetailTokenType.KeywordCtrl, finallySymbol.Value);
            foreach (var catchBlock in catchBlocks)
                collector.Add(DetailTokenType.KeywordCtrl, catchBlock.catchSymbol);
        }
    }
}
