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
        public override void Read(StatementParameter parameter)
        {
            condition?.Read(parameter);
            loopBlock?.Read(parameter);
            elseBlock?.Read(parameter);
        }
    }
    internal class WhileStatement(TextRange symbol, Expression? condition) : LoopStatement(symbol, condition, []) { }
    internal class ForStatement(TextRange symbol, Expression? condition, TextRange? separator1, TextRange? separator2, Expression? front, Expression? back) : LoopStatement(symbol, condition, [])
    {
        public readonly TextRange? separator1 = separator1, separator2 = separator2;//两个分隔符 ;
        public readonly Expression? front = front, back = back;

        public override void Read(StatementParameter parameter)
        {
            base.Read(parameter);
            front?.Read(parameter);
            back?.Read(parameter);
        }
    }
}
