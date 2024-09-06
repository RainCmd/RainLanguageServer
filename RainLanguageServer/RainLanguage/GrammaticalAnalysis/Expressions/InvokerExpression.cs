using System.Diagnostics.CodeAnalysis;

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

        protected abstract int CollectSignatureInfos(Manager manager, List<SignatureInfo> infos, Context context, AbstractSpace? space);
        protected override bool InternalTrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            if (parameters.range.Contain(position))
            {
                if (parameters.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex)) return true;
                if (ManagerOperator.TryGetContext(manager, position, out var context))
                {
                    infos = [];
                    functionIndex = CollectSignatureInfos(manager, infos, context, ManagerOperator.GetSpace(manager, position));
                    parameterIndex = parameters.GetTupleIndex(position);
                    return true;
                }
            }
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
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
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (invoker.range.Contain(position)) return invoker.Operator(position, action);
            if (parameters.range.Contain(position)) return parameters.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (invoker.range.Contain(position)) return invoker.BreadthFirstOperator(position, action);
            if (parameters.range.Contain(position)) return parameters.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            invoker.Operator(action);
            parameters.Operator(action);
            action(this);
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

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            invoker.CollectSemanticToken(manager, collector);
            parameters.CollectSemanticToken(manager, collector);
        }

        protected override int CollectSignatureInfos(Manager manager, List<SignatureInfo> infos, Context context, AbstractSpace? space)
        {
            if (!manager.TryGetDeclaration(invoker.tuple[0], out var declaration)) throw new Exception("类型错误");
            if (declaration is not AbstractDelegate abstractDelegate) throw new Exception($"{declaration.GetType()} 不是委托类型");
            infos.Add(abstractDelegate.GetSignatureInfo(manager, null, space));
            return 0;
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
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (parameters.range.Contain(position)) return parameters.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (parameters.range.Contain(position)) return parameters.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            parameters.Operator(action);
            action(this);
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

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            if (qualifier != null) collector.Add(DetailTokenType.KeywordCtrl, qualifier.Value);
            collector.AddNamespace(name);
            collector.Add(DetailTokenType.GlobalFunction, name.name);
            parameters.CollectSemanticToken(manager, collector);
        }

        protected override int CollectSignatureInfos(Manager manager, List<SignatureInfo> infos, Context context, AbstractSpace? space)
        {
            if (callable.space.declarations.TryGetValue(callable.name.ToString(), out var declarations))
            {
                var result = -1;
                for (var i = 0; i < declarations.Count; i++)
                {
                    if (!manager.TryGetDeclaration(declarations[i], out var declaration)) throw new Exception("类型错误");
                    if (declaration is not AbstractCallable callable) throw new Exception($"{declaration.GetType()} 不是可调用对象");
                    if (context.IsVisiable(manager, callable.declaration))
                    {
                        infos.Add(callable.GetSignatureInfo(manager, null, space));
                        if (callable == this.callable) result = i;
                    }
                }
                if (result < 0)
                {
                    result = infos.Count;
                    infos.Add(callable.GetSignatureInfo(manager, null, space));
                }
                return result;
            }
            else
            {
                infos.Add(callable.GetSignatureInfo(manager, null, space));
                return 0;
            }
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
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (target != null && target.range.Contain(position)) return target.Operator(position, action);
            if (parameters.range.Contain(position)) return parameters.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (target != null && target.range.Contain(position)) return target.BreadthFirstOperator(position, action);
            if (parameters.range.Contain(position)) return parameters.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            target?.Operator(action);
            parameters.Operator(action);
            action(this);
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

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            target?.CollectSemanticToken(manager, collector);
            if (symbol != null) collector.Add(DetailTokenType.Operator, symbol.Value);
            collector.Add(DetailTokenType.MemberFunction, method);
            parameters.CollectSemanticToken(manager, collector);
        }

        protected override int CollectSignatureInfos(Manager manager, List<SignatureInfo> infos, Context context, AbstractSpace? space)
        {
            if (!manager.TryGetDefineDeclaration(callable.declaration, out var declaration)) throw new Exception("类型错误");
            if (declaration is AbstractStruct abstractStruct)
            {
                var result = 0;
                var find = false;
                var name = callable.name.ToString();
                foreach (var function in abstractStruct.functions)
                    if (function.name == name && context.IsVisiable(manager, function.declaration))
                    {
                        infos.Add(function.GetSignatureInfo(manager, declaration, space));
                        if (!find)
                        {
                            if (function == callable) find = true;
                            else result++;
                        }
                    }
                if (!find)
                {
                    result = infos.Count;
                    infos.Add(callable.GetSignatureInfo(manager, declaration, space));
                }
                return result;
            }
            else if (declaration is AbstractInterface abstractInterface)
            {
                var result = 0;
                var find = false;
                var name = callable.name.ToString();
                var set = new HashSet<AbstractCallable>();
                foreach (var inherit in manager.GetInheritIterator(abstractInterface))
                    foreach (var function in inherit.functions)
                        if (function.name == name && set.Add(function) && context.IsVisiable(manager, function.declaration))
                        {
                            set.AddRange(function.overrides);
                            infos.Add(function.GetSignatureInfo(manager, inherit, space));
                            if (!find)
                            {
                                if (function == callable) find = true;
                                else result++;
                            }
                        }
                if (!find)
                {
                    result = infos.Count;
                    infos.Add(callable.GetSignatureInfo(manager, declaration, space));
                }
                return result;
            }
            else if (declaration is AbstractClass abstractClass)
            {
                var result = 0;
                var find = false;
                var name = callable.name.ToString();
                var set = new HashSet<AbstractCallable>();
                foreach (var inherit in manager.GetInheritIterator(abstractClass))
                    foreach (var function in inherit.functions)
                        if (function.name == name && set.Add(function) && context.IsVisiable(manager, function.declaration))
                        {
                            set.AddRange(function.overrides);
                            infos.Add(function.GetSignatureInfo(manager, inherit, space));
                            if (!find)
                            {
                                if (function == callable) find = true;
                                else result++;
                            }
                        }
                if (!find)
                {
                    result = infos.Count;
                    infos.Add(callable.GetSignatureInfo(manager, declaration, space));
                }
                return result;
            }
            else
            {
                infos.Add(callable.GetSignatureInfo(manager, null, space));
                return 0;
            }
        }
    }
    internal class InvokerVirtualExpression(TextRange range, Tuple tuple, TextRange? symbol, TextRange method, Expression? target, AbstractCallable callable, BracketExpression parameters, Manager.KernelManager manager) : InvokerMemberExpression(range, tuple, symbol, method, target, callable, parameters, manager)
    {
        public override void Read(ExpressionParameter parameter)
        {
            target?.Read(parameter);
            if (callable is AbstractClass.Function function)
            {
                function.references.Add(method);
                foreach (var item in function.implements)
                    item.references.Add(method);
            }
            else callable.references.Add(method);
            parameters.Read(parameter);
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
