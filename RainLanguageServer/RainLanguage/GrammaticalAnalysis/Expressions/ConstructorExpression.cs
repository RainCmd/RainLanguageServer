using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
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
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (parameters.range.Contain(position)) return parameters.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if(action(this)) return true;
            if (parameters.range.Contain(position)) return parameters.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            type.Operator(action);
            parameters.Operator(action);
            action(this);
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
                if (callables != null && callables.Count > 0)
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

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            type.CollectSemanticToken(manager, collector);
            parameters.CollectSemanticToken(manager, collector);
        }

        protected override bool InternalTrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            if (parameters.range.Contain(position))
            {
                if (parameters.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex)) return true;
                if (manager.TryGetDeclaration(type.type, out var declaration))
                {
                    if (declaration is AbstractStruct abstractStruct)
                    {
                        infos = InfoUtility.GetStructConstructorSignatureInfos(manager, abstractStruct, ManagerOperator.GetSpace(manager, position));
                        if (parameters.tuple.Count > 0 && infos.Count > 1)
                        {
                            functionIndex = 1;
                            parameterIndex = parameters.GetTupleIndex(position);
                        }
                        else
                        {
                            functionIndex = 0;
                            parameterIndex = 0;
                        }
                        return true;
                    }
                    else if (declaration is AbstractClass abstractClass)
                    {
                        var callables = this.callables ?? [];
                        if (callables.Count == 0)
                        {
                            if (ManagerOperator.TryGetContext(manager, position, out var context))
                            {
                                foreach (var ctor in abstractClass.constructors)
                                    if (context.IsVisiable(manager, ctor.declaration))
                                        callables.Add(ctor);
                            }
                            else if (callable != null) callables.Add(callable);
                            else
                            {
                                infos = default;
                                functionIndex = 0;
                                parameterIndex = 0;
                                return false;
                            }
                        }
                        infos = [];
                        var space = ManagerOperator.GetSpace(manager, position);
                        var find = false;
                        functionIndex = 0;
                        foreach (var callable in callables)
                        {
                            infos.Add(callable.GetSignatureInfo(manager, abstractClass, space));
                            if (!find)
                            {
                                if (callable == this.callable) find = true;
                                else functionIndex++;
                            }
                        }
                        parameterIndex = parameters.GetTupleIndex(position);
                        return true;
                    }
                }
            }
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }
    }
}
