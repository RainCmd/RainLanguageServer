namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
{
    internal class LoopStatement : Statement
    {
        public readonly TextRange symbol;
        public TextRange? elseSymbol;
        public readonly Expression? condition;
        public BlockStatement? loopBlock, elseBlock;
        public readonly List<TextRange> group = [];

        public LoopStatement(TextRange symbol, Expression? condition)
        {
            this.symbol = symbol;
            this.condition = condition;
            group.Add(symbol);
        }
        protected override void InternalOperator(Action<Expression> action)
        {
            if (condition != null) action(condition);
        }
        public override void Operator(Action<Statement> action)
        {
            loopBlock?.Operator(action);
            elseBlock?.Operator(action);
            action(this);
        }
        protected override bool InternalOperator(TextPosition position, ExpressionOperator action) => condition != null && condition.range.Contain(position) && action(condition);
        public override bool Operator(TextPosition position, StatementOperator action)
        {
            if (loopBlock != null && loopBlock.range.Contain(position)) return loopBlock.Operator(position, action);
            if (elseBlock != null && elseBlock.range.Contain(position)) return elseBlock.Operator(position, action);
            return action(this);
        }
        protected override void InternalOperator(TextRange range, Action<Expression> action)
        {
            if (condition != null && condition.range.Overlap(range)) action(condition);
        }
        public override void Operator(TextRange range, Action<Statement> action)
        {
            if (loopBlock != null && loopBlock.range.Overlap(range)) loopBlock.Operator(range, action);
            if (elseBlock != null && elseBlock.range.Overlap(range)) elseBlock.Operator(range, action);
            action(this);
        }

        protected override bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos)
        {
            if (symbol.Contain(position) || (elseSymbol != null && elseSymbol.Value.Contain(position)))
            {
                InfoUtility.HighlightGroup(group, infos);
                return true;
            }
            return false;
        }
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.KeywordCtrl, symbol);
            if (elseSymbol != null) collector.Add(DetailTokenType.KeywordCtrl, elseSymbol.Value);
        }
    }
    internal class WhileStatement(TextRange symbol, Expression? condition) : LoopStatement(symbol, condition)
    {
        protected override void InternalCollectInlayHint(Manager manager, List<InlayHintInfo> infos)
        {
            if (condition == null)
                infos.Add(new InlayHintInfo($" {KeyWords.TRUE}", symbol.end, InlayHintInfo.Kind.Paramter));
        }
    }
    internal class ForStatement(TextRange symbol, Expression? condition, TextRange? separator1, TextRange? separator2, Expression? front, Expression? back) : LoopStatement(symbol, condition)
    {
        public readonly TextRange? separator1 = separator1, separator2 = separator2;//两个分隔符 ;
        public readonly Expression? front = front, back = back;
        protected override void InternalOperator(Action<Expression> action)
        {
            base.InternalOperator(action);
            if (front != null) action(front);
            if (back != null) action(back);
        }
        protected override bool InternalOperator(TextPosition position, ExpressionOperator action)
        {
            if (front != null && front.range.Contain(position)) return action(front);
            if (back != null && back.range.Contain(position)) return action(back);
            return base.InternalOperator(position, action);
        }
        protected override void InternalOperator(TextRange range, Action<Expression> action)
        {
            if (front != null && front.range.Overlap(range)) action(front);
            if (back != null && back.range.Overlap(range)) action(back);
            base.InternalOperator(range, action);
        }
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            base.InternalCollectSemanticToken(manager, collector);
            if (separator1 != null) collector.Add(DetailTokenType.Operator, separator1.Value);
            if (separator2 != null) collector.Add(DetailTokenType.Operator, separator2.Value);
        }
    }
}
