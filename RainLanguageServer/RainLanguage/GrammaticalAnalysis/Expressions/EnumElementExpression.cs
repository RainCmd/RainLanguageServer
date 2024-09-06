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

        public EnumElementExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange symbol, TextRange identifier, AbstractEnum abstractEnum, AbstractEnum.Element element, TypeExpression type) : base(range, type.type, snapshoot)
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
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (type.range.Contain(position)) return type.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if(action(this)) return true;
            if (type.range.Contain(position)) return type.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            type.Operator(action);
            action(this);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (type.range.Contain(position)) return type.OnHover(manager, position, out info);
            if (identifier.Contain(position))
            {
                info = new HoverInfo(identifier, element.CodeInfo(manager, ManagerOperator.GetSpace(manager, position)), true);
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
