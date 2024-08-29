
namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal abstract class InvokerExpression : Expression
    {
        public readonly BracketExpression parameters;
        public override bool Valid => true;

        public InvokerExpression(TextRange range, Tuple tuple, BracketExpression parameters, Manager.KernelManager manager) : base(range, tuple)
        {
            this.parameters = parameters;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
        }
    }
    internal class InvokerDelegateExpression(TextRange range, Tuple tuple, Expression invoker, BracketExpression parameters, Manager.KernelManager manager) : InvokerExpression(range, tuple, parameters, manager)
    {
        public readonly Expression invoker = invoker;

        public override void Read(ExpressionParameter parameter)
        {
            invoker.Read(parameter);
            parameters.Read(parameter);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (invoker.range.Contain(position)) return invoker.OnHover(manager, position, out info);
            if (parameters.range.Contain(position)) return parameters.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (invoker.range.Contain(position)) return invoker.OnHighlight(manager, position, infos);
            if (parameters.range.Contain(position)) return parameters.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (invoker.range.Contain(position)) return invoker.TryGetDefinition(manager, position, out definition);
            if (parameters.range.Contain(position)) return parameters.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (invoker.range.Contain(position)) return invoker.FindReferences(manager, position, references);
            if (parameters.range.Contain(position)) return parameters.FindReferences(manager, position, references);
            return false;
        }
    }
    internal class InvokerFunctionExpression(TextRange range, Tuple tuple, TextRange? qualifier, QualifiedName name, AbstractCallable callable, BracketExpression parameters, Manager.KernelManager manager) : InvokerExpression(range, tuple, parameters, manager)
    {
        public readonly TextRange? qualifier = qualifier;
        public readonly QualifiedName name = name;
        public readonly AbstractCallable callable = callable;

        public override void Read(ExpressionParameter parameter)
        {
            callable.references.Add(name.name);
            parameters.Read(parameter);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (InfoUtility.OnHover(name.qualify, position, out info)) return true;
            if (name.name.Contain(position))
            {
                info = new HoverInfo(name.name, callable.Info(manager, null, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (InfoUtility.OnHighlight(name.qualify, position, callable.space, infos)) return true;
            if (name.name.Contain(position))
            {
                InfoUtility.Highlight(callable, infos);
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (name.name.Contain(position))
            {
                definition = callable.name;
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (InfoUtility.FindReferences(name.qualify, position, callable.space, references)) return true;
            if (name.name.Contain(position))
            {
                references.AddRange(callable.references);
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.FindReferences(manager, position, references);
            return false;
        }
    }
    internal class InvokerMemberExpression(TextRange range, Tuple tuple, TextRange? symbol, TextRange method, Expression? target, AbstractCallable callable, BracketExpression parameters, Manager.KernelManager manager) : InvokerExpression(range, tuple, parameters, manager)
    {
        public readonly TextRange? symbol = symbol;
        public readonly TextRange method = method;
        public readonly Expression? target = target;
        public readonly AbstractCallable callable = callable;

        public override void Read(ExpressionParameter parameter)
        {
            target?.Read(parameter);
            callable.references.Add(method);
            parameters.Read(parameter);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (target != null && target.range.Contain(position)) return target.OnHover(manager, position, out info);
            if (method.Contain(position))
            {
                manager.TryGetDefineDeclaration(callable.declaration, out var declaration);
                info = new HoverInfo(method, callable.Info(manager, declaration, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (target != null && target.range.Contain(position)) return target.OnHighlight(manager, position, infos);
            if (method.Contain(position))
            {
                InfoUtility.Highlight(callable, infos);
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (target != null && target.range.Contain(position)) return target.TryGetDefinition(manager, position, out definition);
            if (method.Contain(position))
            {
                definition = callable.name;
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (target != null && target.range.Contain(position)) return target.FindReferences(manager, position, references);
            if (method.Contain(position))
            {
                references.AddRange(callable.references);
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.FindReferences(manager, position, references);
            return false;
        }
    }
    internal class InvokerVirtualExpression(TextRange range, Tuple tuple, TextRange? symbol, TextRange method, Expression? target, AbstractCallable callable, BracketExpression parameters, Manager.KernelManager manager) : InvokerMemberExpression(range, tuple, symbol, method, target, callable, parameters, manager)
    {
        public override void Read(ExpressionParameter parameter)
        {
            target?.Read(parameter);
            if (callable is AbstractClass.Function function) Reference(function);
            else callable.references.Add(method);
            parameters.Read(parameter);
        }
        private void Reference(AbstractClass.Function function)
        {
            function.references.Add(method);
            foreach (var item in function.implements)
                Reference(item);
        }
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (target != null && target.range.Contain(position)) return target.OnHighlight(manager, position, infos);
            if (method.Contain(position))
            {
                InfoUtility.Highlight(callable, infos);
                if (callable is AbstractClass.Function function)
                    foreach (var item in function.overrides)
                        InfoUtility.Highlight(item, infos);
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.OnHighlight(manager, position, infos);
            return false;
        }
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (target != null && target.range.Contain(position)) return target.FindReferences(manager, position, references);
            if (method.Contain(position))
            {
                references.AddRange(callable.references);
                if (callable is AbstractClass.Function function)
                    foreach (var item in function.overrides)
                        references.AddRange(item.references);
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.FindReferences(manager, position, references);
            return false;
        }
    }
}
