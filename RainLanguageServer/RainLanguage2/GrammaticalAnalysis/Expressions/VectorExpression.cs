
namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class VectorMemberExpression : Expression
    {
        public readonly Expression target;
        public readonly TextRange member;
        public override bool Valid => true;
        public VectorMemberExpression(TextRange range, Type type, Expression target, TextRange member) : base(range, type)
        {
            this.target = target;
            this.member = member;
            attribute = ExpressionAttribute.Value | (target.attribute & ExpressionAttribute.Assignable);
        }
        public override void Read(ExpressionParameter parameter) => target.Read(parameter);
        public override void Write(ExpressionParameter parameter) => target.Write(parameter);

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (target.range.Contain(position)) return target.OnHover(manager, position, out info);
            if (member.Contain(position))
            {
                info = new HoverInfo(member, tuple[0].Info(manager, true, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (target.range.Contain(position)) return target.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (target.range.Contain(position)) return target.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (target.range.Contain(position)) return target.FindReferences(manager, position, references);
            return false;
        }
    }
    internal class VectorConstructorExpression : Expression
    {
        public readonly TypeExpression type;
        public readonly BracketExpression parameters;
        public override bool Valid => true;
        public VectorConstructorExpression(TextRange range, TypeExpression type, BracketExpression parameters) : base(range, type.type)
        {
            this.type = type;
            this.parameters = parameters;
            attribute = ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            type.Read(parameter);
            parameters.Read(parameter);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (type.range.Contain(position)) return type.OnHover(manager, position, out info);
            if (parameters.range.Contain(position)) return parameters.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (type.range.Contain(position)) return type.OnHighlight(manager, position, infos);
            if (parameters.range.Contain(position)) return parameters.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (type.range.Contain(position)) return type.TryGetDefinition(manager, position, out definition);
            if (parameters.range.Contain(position)) return parameters.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (type.range.Contain(position)) return type.FindReferences(manager, position, references);
            if (parameters.range.Contain(position)) return parameters.FindReferences(manager, position, references);
            return false;
        }
    }
}
