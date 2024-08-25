namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class LoopStatement : Statement
    {
        public readonly TextRange symbol;
        public TextRange? elseSymbol;
        public readonly Expression? condition;
        public BlockStatement? loopBlock, elseBlock;
        public readonly List<TextRange> group;

        public LoopStatement(TextRange symbol, Expression condition, List<TextRange> group)
        {
            this.symbol = symbol;
            this.condition = condition;
            this.group = group;
            group.Add(symbol);
        }
    }
    internal class WhileStatement(TextRange symbol, Expression? condition) : LoopStatement(symbol, condition, []) { }
    internal class ForStatement : LoopStatement
    {
        public readonly TextRange? separator1, separator2;//两个分隔符 ;
        public readonly Expression? front, back;

        public ForStatement(TextRange symbol, Expression? condition, TextRange? separator1, TextRange? separator2, Expression? front, Expression? back) : base(symbol, condition, [])
        {
            this.separator1 = separator1;
            this.separator2 = separator2;
            this.front = front;
            this.back = back;
        }
    }
}
