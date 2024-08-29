namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class ConstructorExpression : Expression
    {
        public readonly TypeExpression type;
        public readonly AbstractCallable? callable;
        public readonly List<AbstractCallable>? callables;
        public readonly BracketExpression parameters;
        public override bool Valid => true;
        public ConstructorExpression(TextRange range, TypeExpression type, AbstractCallable? callable, List<AbstractCallable>? callables, BracketExpression parameters, Manager.KernelManager manager) : base(range, type.type)
        {
            this.type = type;
            this.callable = callable;
            this.callables = callables;
            this.parameters = parameters;
            attribute = ExpressionAttribute.Value | type.type.GetAttribute(manager);
        }
        public override void Read(ExpressionParameter parameter)
        {
            type.Read(parameter);
            callable?.references.Add(type.range);
            if (callables != null)
                foreach (var item in callables)
                    item.references.Add(type.range);
            parameters.Read(parameter);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (type.range.Contain(position))
            {
                if (callable != null)
                {
                    manager.TryGetDeclaration(type.type, out var declaration);
                    info = new HoverInfo(type.range, callable.Info(manager, declaration, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                    return true;
                }
                return type.OnHover(manager, position, out info);
            }
            if (parameters.range.Contain(position)) return parameters.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (type.range.Contain(position))
            {
                if (callable != null)
                {
                    InfoUtility.Highlight(callable, infos);
                    return true;
                }
                if (callables != null)
                {
                    foreach (var callable in callables)
                        InfoUtility.Highlight(callable, infos);
                    return true;
                }
                return type.OnHighlight(manager, position, infos);
            }
            if (parameters.range.Contain(position)) return parameters.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (type.range.Contain(position))
            {
                if (callable != null)
                {
                    definition = callable.name;
                    return true;
                }
                if (callables != null)
                {
                    definition = callables[0].name;
                    return true;
                }
                return type.TryGetDefinition(manager, position, out definition);
            }
            if (parameters.range.Contain(position)) return parameters.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (type.range.Contain(position))
            {
                if (callable != null)
                {
                    references.AddRange(callable.references);
                    return true;
                }
                if (callables != null)
                {
                    foreach (var callable in callables)
                        references.AddRange(callable.references);
                    return true;
                }
                return type.FindReferences(manager, position, references);
            }
            if (parameters.range.Contain(position)) return parameters.FindReferences(manager, position, references);
            return false;
        }
    }
}
