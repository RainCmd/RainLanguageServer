namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class BlockStatement : Statement
    {
        public readonly List<Statement> statements = [];
        public BlockStatement(TextRange range)
        {
            base.range = range;
        }
        public override void Read(StatementParameter parameter)
        {
            foreach(var statement in statements) statement.Read(parameter);
        }
    }
}
