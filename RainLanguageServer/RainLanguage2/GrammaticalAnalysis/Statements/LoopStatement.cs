
namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class LoopStatement : Statement
    {
        public readonly TextRange symbol;
        public TextRange? elseSymbol;
        public readonly Expression? condition;
        public BlockStatement? loopBlock, elseBlock;
        public readonly List<TextRange> group;

        public LoopStatement(TextRange symbol, Expression? condition, List<TextRange> group)
        {
            this.symbol = symbol;
            this.condition = condition;
            this.group = group;
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
    }
    internal class WhileStatement(TextRange symbol, Expression? condition) : LoopStatement(symbol, condition, []) { }
    internal class ForStatement(TextRange symbol, Expression? condition, TextRange? separator1, TextRange? separator2, Expression? front, Expression? back) : LoopStatement(symbol, condition, [])
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
    }
}
