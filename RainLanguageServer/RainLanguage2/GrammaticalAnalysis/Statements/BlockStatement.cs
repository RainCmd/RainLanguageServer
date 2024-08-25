namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class BlockStatement : Statement
    {
        public int indent = -1;
        public readonly List<Statement> statements = [];
    }
}
