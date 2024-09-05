
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
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
        public BracketExpression Replace(Expression expression)
        {
            if (this.expression == expression) return this;
            return new BracketExpression(left, right, expression);
        }
        public override bool TryEvaluateIndices(List<long> indices)
        {
            return expression.TryEvaluateIndices(indices);
        }
        public override bool Calculability() => expression.Calculability();
        public override void Read(ExpressionParameter parameter) => expression.Read(parameter);
        public override void Write(ExpressionParameter parameter) => expression.Write(parameter);

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

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.Operator, left);
            collector.Add(DetailTokenType.Operator, right);
            expression.CollectSemanticToken(manager, collector);
        }

        public override int GetTupleIndex(TextPosition position)
        {
            if (expression.range.Contain(position)) return expression.GetTupleIndex(position);
            return 0;
        }
        public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            if (expression.range.Contain(position)) return expression.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }
    }
}
