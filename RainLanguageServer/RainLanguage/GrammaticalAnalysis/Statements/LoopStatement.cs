
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
        public override void Operator(Action<Expression> action)
        {
            if (condition != null) action(condition);
            loopBlock?.Operator(action);
            elseBlock?.Operator(action);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (condition != null && condition.range.Contain(position)) return action(condition);
            if (loopBlock != null && loopBlock.range.Contain(position)) return loopBlock.Operator(position, action);
            if (elseBlock != null && elseBlock.range.Contain(position)) return elseBlock.Operator(position, action);
            return false;
        }
        public override bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos)
        {
            if (symbol.Contain(position) || (elseSymbol != null && elseSymbol.Value.Contain(position)))
            {
                InfoUtility.HighlightGroup(group, infos);
                return true;
            }
            if (loopBlock != null && loopBlock.range.Contain(position)) return loopBlock.TryHighlightGroup(position, infos);
            if (elseBlock != null && elseBlock.range.Contain(position)) return elseBlock.TryHighlightGroup(position, infos);
            return false;
        }
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.KeywordCtrl, symbol);
            if (elseSymbol != null) collector.Add(DetailTokenType.KeywordCtrl, elseSymbol.Value);
            condition?.CollectSemanticToken(manager, collector);
            loopBlock?.CollectSemanticToken(manager, collector);
            elseBlock?.CollectSemanticToken(manager, collector);
        }
    }
    internal class WhileStatement(TextRange symbol, Expression? condition) : LoopStatement(symbol, condition) { }
    internal class ForStatement(TextRange symbol, Expression? condition, TextRange? separator1, TextRange? separator2, Expression? front, Expression? back) : LoopStatement(symbol, condition)
    {
        public readonly TextRange? separator1 = separator1, separator2 = separator2;//两个分隔符 ;
        public readonly Expression? front = front, back = back;
        public override void Operator(Action<Expression> action)
        {
            base.Operator(action);
            if (front != null) action(front);
            if (back != null) action(back);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (front != null && front.range.Contain(position)) return action(front);
            if (back != null && back.range.Contain(position)) return action(back);
            return base.Operator(position, action);
        }
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            base.CollectSemanticToken(manager, collector);
            if (separator1 != null) collector.Add(DetailTokenType.Operator, separator1.Value);
            if (separator2 != null) collector.Add(DetailTokenType.Operator, separator2.Value);
            front?.CollectSemanticToken(manager, collector);
            back?.CollectSemanticToken(manager, collector);
        }
    }
}
