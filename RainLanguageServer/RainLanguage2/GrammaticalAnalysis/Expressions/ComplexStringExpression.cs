namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class ComplexStringExpression : Expression
    {
        public readonly List<Expression> expressions;
        public override bool Valid => true;
        public ComplexStringExpression(TextRange range, List<Expression> expressions, Manager.KernelManager manager) : base(range, manager.STRING)
        {
            this.expressions = expressions;
            attribute = ExpressionAttribute.Value | manager.STRING.GetAttribute(manager);
        }
        public override void Read(ExpressionParameter parameter)
        {
            foreach (var expression in expressions) expression.Read(parameter);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            foreach (var expression in expressions)
                if (expression.range.Contain(position))
                    return expression.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            foreach (var expression in expressions)
                if (expression.range.Contain(position))
                    return expression.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            foreach (var expression in expressions)
                if (expression.range.Contain(position))
                    return expression.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            foreach (var expression in expressions)
                if (expression.range.Contain(position))
                    return expression.FindReferences(manager, position, references);
            return false;
        }
    }
}
