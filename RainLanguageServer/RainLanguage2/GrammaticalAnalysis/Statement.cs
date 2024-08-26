namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis
{
    internal readonly struct StatementParameter(Manager manager, MessageCollector collector)
    {
        public readonly Manager manager = manager;
        public readonly MessageCollector collector = collector;
        public static implicit operator ExpressionParameter(StatementParameter parameter) => new(parameter.manager, parameter.collector);
    }
    internal class Statement
    {
        public TextRange range;
        public virtual void Read(StatementParameter parameter) { }
    }
}
