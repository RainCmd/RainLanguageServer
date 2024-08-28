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
        public delegate bool ExpressionOperator(Expression expression);
        public TextRange range;
        public virtual void Operator(Action<Expression> action) { }
        public void Read(StatementParameter parameter) => Operator(value => value.Read(parameter));
        public virtual bool Operator(TextPosition position, ExpressionOperator action) { return false; }

        public bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            HoverInfo result = default;
            if (Operator(position, value => value.OnHover(manager, position, out result)))
            {
                info = result;
                return true;
            }
            info = default;
            return false;
        }
        public bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => Operator(position, value => value.OnHighlight(manager, position, infos));
        public bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            TextRange result = default;
            if (Operator(position, value => value.TryGetDefinition(manager, position, out result)))
            {
                definition = result;
                return true;
            }
            definition = default;
            return false;
        }
        public bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => Operator(position, value => value.FindReferences(manager, position, references));
    }
}
