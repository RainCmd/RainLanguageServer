
namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class CastExpression : Expression
    {
        public readonly TypeExpression type;
        public readonly TextRange symbol;
        public readonly Expression expression;
        public override bool Valid => true;

        public CastExpression(TextRange range, TypeExpression type, TextRange symbol, Expression expression, Manager.KernelManager manager) : base(range, type.type)
        {
            this.type = type;
            this.symbol = symbol;
            this.expression = expression;
            attribute = ExpressionAttribute.Value | type.type.GetAttribute(manager);
        }
        public override void Read(ExpressionParameter parameter)
        {
            type.Read(parameter);
            expression.Read(parameter);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (type.range.Contain(position)) return type.OnHover(manager, position, out info);
            if (expression.range.Contain(position)) return expression.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (type.range.Contain(position)) return type.OnHighlight(manager, position, infos);
            if (expression.range.Contain(position)) return expression.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (type.range.Contain(position)) return type.TryGetDefinition(manager, position, out definition);
            if (expression.range.Contain(position)) return expression.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (type.range.Contain(position)) return type.FindReferences(manager, position, references);
            if (expression.range.Contain(position)) return expression.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            type.CollectSemanticToken(manager, collector);
            collector.Add(DetailTokenType.Operator, symbol);
            expression.CollectSemanticToken(manager, collector);
        }
    }
    internal class TupleCastExpression : Expression
    {
        public readonly Expression expression;
        public override bool Valid => true;
        public TupleCastExpression(Expression expression, Tuple tuple, Manager.KernelManager manager) : base(expression.range, tuple)
        {
            this.expression = expression;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
            attribute |= expression.attribute & ~ExpressionAttribute.Assignable;
        }
        public override void Read(ExpressionParameter parameter) => expression.Read(parameter);

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (expression.range.Contain(position)) return expression.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (expression.range.Contain(position)) return expression.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (expression.range.Contain(position)) return expression.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (expression.range.Contain(position)) return expression.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector) => expression.CollectSemanticToken(manager, collector);
    }
    internal class IsCastExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly TextRange? identifier;
        public readonly Expression source;
        public readonly TypeExpression type;
        public readonly Local? local;
        public override bool Valid => true;

        public IsCastExpression(TextRange range, TextRange symbol, TextRange? identifier, Expression source, TypeExpression type, Local? local, Manager.KernelManager manager) : base(range, manager.BOOL)
        {
            this.symbol = symbol;
            this.identifier = identifier;
            this.source = source;
            this.type = type;
            this.local = local;
            attribute = ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            source.Read(parameter);
            type.Read(parameter);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (source.range.Contain(position)) return source.OnHover(manager, position, out info);
            if (type.range.Contain(position)) return type.OnHover(manager, position, out info);
            if (local != null && local.Value.range.Contain(position))
            {
                info = local.Value.Hover(manager, position);
                return true;
            }
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (source.range.Contain(position)) return source.OnHighlight(manager, position, infos);
            if (type.range.Contain(position)) return type.OnHighlight(manager, position, infos);
            if (local != null && local.Value.range.Contain(position))
            {
                local.Value.OnHighlight(infos);
                return true;
            }
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (source.range.Contain(position)) return source.TryGetDefinition(manager, position, out definition);
            if (type.range.Contain(position)) return type.TryGetDefinition(manager, position, out definition);
            if (local != null && local.Value.range.Contain(position))
            {
                definition = local.Value.range;
                return true;
            }
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (source.range.Contain(position)) return source.FindReferences(manager, position, references);
            if (type.range.Contain(position)) return type.FindReferences(manager, position, references);
            if (local != null && local.Value.range.Contain(position))
            {
                local.Value.FindReferences(references);
                return true;
            }
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.KeywordCtrl, symbol);
            if (identifier != null) collector.Add(DetailTokenType.Local, identifier.Value);
            source.CollectSemanticToken(manager, collector);
            type.CollectSemanticToken(manager, collector);
        }
    }
    internal class AsCastExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression source;
        public readonly TypeExpression type;
        public override bool Valid => true;

        public AsCastExpression(TextRange range, TextRange symbol, Expression source, TypeExpression type) : base(range, type.type)
        {
            this.symbol = symbol;
            this.source = source;
            this.type = type;
            attribute = ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            source.Read(parameter);
            type.Read(parameter);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (source.range.Contain(position)) return source.OnHover(manager, position, out info);
            if (type.range.Contain(position)) return type.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (source.range.Contain(position)) return source.OnHighlight(manager, position, infos);
            if (type.range.Contain(position)) return type.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (source.range.Contain(position)) return source.TryGetDefinition(manager, position, out definition);
            if (type.range.Contain(position)) return type.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (source.range.Contain(position)) return source.FindReferences(manager, position, references);
            if (type.range.Contain(position)) return type.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.KeywordCtrl, symbol);
            source.CollectSemanticToken(manager, collector);
            type.CollectSemanticToken(manager, collector);
        }
    }
}
