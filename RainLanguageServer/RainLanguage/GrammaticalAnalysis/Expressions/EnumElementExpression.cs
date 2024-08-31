
namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class EnumElementExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly TextRange identifier;
        public readonly AbstractEnum abstractEnum;
        public readonly AbstractEnum.Element element;
        public readonly TypeExpression type;
        public override bool Valid => true;

        public EnumElementExpression(TextRange range, TextRange symbol, TextRange identifier, AbstractEnum abstractEnum, AbstractEnum.Element element, TypeExpression type) : base(range, type.type)
        {
            this.symbol = symbol;
            this.identifier = identifier;
            this.abstractEnum = abstractEnum;
            this.element = element;
            this.type = type;
            attribute = ExpressionAttribute.Constant;
        }
        public override void Read(ExpressionParameter parameter)
        {
            abstractEnum.references.Add(type.range);
            element.references.Add(identifier);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (type.range.Contain(position)) return type.OnHover(manager, position, out info);
            if (identifier.Contain(position))
            {
                info = new HoverInfo(identifier, element.Info(manager, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (type.range.Contain(position)) return type.OnHighlight(manager, position, infos);
            if (identifier.Contain(position))
            {
                InfoUtility.Highlight(element, infos);
                return true;
            }
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (type.range.Contain(position)) return type.TryGetDefinition(manager, position, out definition);
            if (identifier.Contain(position))
            {
                definition = element.name;
                return true;
            }
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (type.range.Contain(position)) return type.FindReferences(manager, position, references);
            if (identifier.Contain(position))
            {
                references.AddRange(element.references);
                return true;
            }
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            type.CollectSemanticToken(manager, collector);
            collector.Add(DetailTokenType.Operator, symbol);
            collector.Add(DetailTokenType.MemberElement, symbol);
        }
    }
}
