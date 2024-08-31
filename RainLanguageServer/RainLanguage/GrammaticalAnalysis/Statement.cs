namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis
{
    internal abstract class Statement
    {
        public delegate bool ExpressionOperator(Expression expression);
        public TextRange range;
        public abstract void Operator(Action<Expression> action);
        public void Read(ExpressionParameter parameter) => Operator(value => value.Read(parameter));
        public abstract bool Operator(TextPosition position, ExpressionOperator action);

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
        public abstract bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos);
        public bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (Operator(position, value => value.OnHighlight(manager, position, infos))) return true;
            return TryHighlightGroup(position, infos);
        }

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
        public abstract void CollectSemanticToken(Manager manager, SemanticTokenCollector collector);
    }
}
