namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class JumpStatement : Statement
    {
        public readonly LoopStatement? loop;
        public readonly Expression? condition;
    }
    internal class BreakStatement : JumpStatement { }
    internal class ContinueStatement : JumpStatement { }
}
