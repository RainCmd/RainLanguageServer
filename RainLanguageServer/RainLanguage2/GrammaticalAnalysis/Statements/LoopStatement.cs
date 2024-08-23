namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class LoopStatement : Statement
    {
        public readonly TextRange symbol;
        public readonly TextRange? elseSymbol;
        public readonly Expression? condition;
        public BlockStatement? loopBlock, elseBlock;
        public readonly List<TextRange> group;
    }
    internal class WhileStatement : LoopStatement { }
    internal class ForStatement : LoopStatement
    {
        public readonly TextRange? separator1, separator2;//两个分隔符 ;
        public readonly Expression? front, back;
    }
}
