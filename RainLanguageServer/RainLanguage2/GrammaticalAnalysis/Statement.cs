namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis
{
    internal readonly struct StatementParameter(Manager manager, MessageCollector collector)
    {
        public readonly Manager manager = manager;
        public readonly MessageCollector collector = collector;
    }
    internal class Statement
    {
        public TextRange range;
        public virtual void Read(StatementParameter parameter) { }
    }
}
