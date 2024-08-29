
namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class BracketExpression : Expression
    {
        public readonly TextRange left, right;
        public readonly Expression expression;
        public override bool Valid => expression.Valid;
        public BracketExpression(TextRange left, TextRange right, Expression expression) : base(left & right, expression.tuple)
        {
            this.left = left;
            this.right = right;
            this.expression = expression;
            attribute = expression.attribute;
        }
        public override bool TryEvaluateIndices(List<long> indices)
        {
            return expression.TryEvaluateIndices(indices);
        }
        public override void Read(ExpressionParameter parameter) => expression.Read(parameter);

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (expression.range.Contain(position)) return expression.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => expression.range.Contain(position) && expression.OnHighlight(manager, position, infos);

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (expression.range.Contain(position)) return expression.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => expression.range.Contain(position) && expression.FindReferences(manager, position, references);
    }
}
