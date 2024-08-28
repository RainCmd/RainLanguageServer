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
        public virtual bool OnHover(Manager manager, TextPosition position, out HoverInfo info) { info = default; return false; }//todo hover
        public virtual bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) { return false; };//todo highlight
        public virtual bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition) { definition = default; return false; }//todo definition
        public virtual bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) { return false; }//todo references
    }
}
