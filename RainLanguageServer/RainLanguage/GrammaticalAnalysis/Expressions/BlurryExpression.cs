namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class BlurryVariableDeclarationExpression : Expression
    {
        public readonly TextRange declaration;
        public readonly TextRange identifier;
        public override bool Valid => true;

        public BlurryVariableDeclarationExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange declaration, TextRange identifier) : base(range, TUPLE_BLURRY, snapshoot)
        {
            this.declaration = declaration;
            this.identifier = identifier;
            attribute = ExpressionAttribute.Value | ExpressionAttribute.Assignable;
        }
        public override void Read(ExpressionParameter parameter) => parameter.collector.Add(declaration, ErrorLevel.Error, "无法推断类型");
        public override bool Operator(TextPosition position, ExpressionOperator action) => action(this);
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action) => action(this);
        public override void Operator(Action<Expression> action) => action(this);

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => false;

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => false;

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.KeywordCtrl, declaration);
            collector.Add(DetailTokenType.Local, identifier);
        }
    }
    internal class MethodExpression : Expression//global & native
    {
        public readonly TextRange? qualifier;// global关键字
        public readonly QualifiedName name;
        public readonly List<AbstractCallable> callables;
        public override bool Valid => true;
        public MethodExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange? qualifier, QualifiedName name, List<AbstractCallable> callables) : base(range, TUPLE_BLURRY, snapshoot)
        {
            this.qualifier = qualifier;
            this.name = name;
            this.callables = callables;
            attribute = ExpressionAttribute.Method | ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            var msg = new Message(name.name, ErrorLevel.Error, "语义不明确");
            foreach (var callable in callables)
            {
                msg.related.Add(new RelatedInfo(callable.name, "符合条件的函数"));
                callable.references.Add(name.name);
            }
            parameter.collector.Add(msg);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action) => action(this);
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action) => action(this);
        public override void Operator(Action<Expression> action) => action(this);

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (InfoUtility.OnHover(name.qualify, position, out info)) return true;
            if (name.name.Contain(position))
            {
                info = new HoverInfo(name.name, callables[0].Info(manager, null, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (InfoUtility.OnHighlight(name.qualify, position, callables[0].space, infos)) return true;
            if (name.name.Contain(position))
            {
                foreach (var callable in callables)
                    InfoUtility.Highlight(callable, infos);
                return true;
            }
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (name.name.Contain(position))
            {
                definition = callables[0].name;
                return true;
            }
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (InfoUtility.FindReferences(name.qualify, position, callables[0].space, references)) return true;
            if (name.name.Contain(position))
            {
                foreach (var callable in callables)
                    references.AddRange(callable.references);
                return true;
            }
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            if (qualifier != null) collector.Add(DetailTokenType.KeywordCtrl, qualifier.Value);
            InfoUtility.AddNamespace(collector, name);
            collector.Add(DetailTokenType.GlobalFunction, name.name);
        }
    }
    internal class MethodMemberExpression : Expression
    {
        public readonly TextRange? symbol;
        public readonly TextRange member;
        public readonly Expression? target;
        public readonly List<AbstractCallable> callables;
        public override bool Valid => true;
        public MethodMemberExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange? symbol, TextRange member, Expression? target, List<AbstractCallable> callables) : base(range, TUPLE_BLURRY, snapshoot)
        {
            this.target = target;
            this.symbol = symbol;
            this.member = member;
            this.callables = callables;
            attribute = ExpressionAttribute.Method | ExpressionAttribute.Value;
        }
        public MethodMemberExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange member, List<AbstractCallable> callables) : this(range, snapshoot, null, member, null, callables) { }
        public override void Read(ExpressionParameter parameter)
        {
            var msg = new Message(member, ErrorLevel.Error, "语义不明确");
            foreach (var callable in callables)
            {
                msg.related.Add(new RelatedInfo(callable.name, "符合条件的函数"));
                callable.references.Add(member);
            }
            parameter.collector.Add(msg);
            target?.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (target != null && target.range.Contain(position)) return target.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (target != null && target.range.Contain(position)) return target.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            target?.Operator(action);
            action(this);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (target != null && target.range.Contain(position)) return target.OnHover(manager, position, out info);
            if (member.Contain(position))
            {
                manager.TryGetDefineDeclaration(callables[0].declaration, out var declaration);
                info = new HoverInfo(member, callables[0].Info(manager, declaration, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (target != null && target.range.Contain(position)) return target.OnHighlight(manager, position, infos);
            if (member.Contain(position))
            {
                foreach (var callable in callables)
                    InfoUtility.Highlight(callable, infos);
                return true;
            }
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (target != null && target.range.Contain(position)) return target.TryGetDefinition(manager, position, out definition);
            if (member.Contain(position))
            {
                definition = callables[0].name;
                return true;
            }
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (target != null && target.range.Contain(position)) return target.FindReferences(manager, position, references);
            if (member.Contain(position))
            {
                foreach (var callable in callables)
                    references.AddRange(callable.references);
                return true;
            }
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            if (symbol != null) collector.Add(DetailTokenType.Operator, symbol.Value);
            collector.Add(DetailTokenType.MemberFunction, member);
            target?.CollectSemanticToken(manager, collector);
        }
    }
    internal class MethodVirtualExpression : MethodMemberExpression
    {
        public MethodVirtualExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange? symbol, TextRange member, Expression? target, List<AbstractCallable> callables) : base(range, snapshoot, symbol, member, target, callables) { }
        public MethodVirtualExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange member, List<AbstractCallable> callables) : base(range, snapshoot, member, callables) { }
        public override void Read(ExpressionParameter parameter)
        {
            var msg = new Message(member, ErrorLevel.Error, "语义不明确");
            foreach (var callable in callables)
            {
                msg.related.Add(new RelatedInfo(callable.name, "符合条件的函数"));
                if (callable is AbstractClass.Function function)
                {
                    function.references.Add(member);
                    foreach (var item in function.implements)
                        item.references.Add(member);
                }
            }
            parameter.collector.Add(msg);
            target?.Read(parameter);
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (target != null && target.range.Contain(position)) return target.OnHighlight(manager, position, infos);
            if (member.Contain(position))
            {
                foreach (var callable in callables)
                {
                    InfoUtility.Highlight(callable, infos);
                    if (callable is AbstractClass.Function function)
                        foreach (var item in function.overrides)
                            InfoUtility.Highlight(item, infos);
                }
                return true;
            }
            return false;
        }
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (target != null && target.range.Contain(position)) return target.FindReferences(manager, position, references);
            if (member.Contain(position))
            {
                foreach (var callable in callables)
                {
                    references.AddRange(callable.references);
                    if (callable is AbstractClass.Function function)
                        foreach (var item in function.overrides)
                            references.AddRange(item.references);
                }
                return true;
            }
            return false;
        }
    }
    internal class BlurryTaskExpression : Expression
    {
        public readonly TextRange symbol;// start new
        public readonly InvokerExpression invoker;
        public override bool Valid => true;
        public BlurryTaskExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange symbol, InvokerExpression invoker) : base(range, TUPLE_BLURRY, snapshoot)
        {
            this.symbol = symbol;
            this.invoker = invoker;
            attribute = ExpressionAttribute.Value;
        }

        public override void Read(ExpressionParameter parameter) => invoker.Read(parameter);
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (invoker.range.Contain(position)) return invoker.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (invoker.range.Contain(position)) return invoker.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            invoker.Operator(action);
            action(this);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (invoker.range.Contain(position)) return invoker.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => invoker.range.Contain(position) && invoker.OnHighlight(manager, position, infos);

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (invoker.range.Contain(position)) return invoker.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => invoker.range.Contain(position) && invoker.FindReferences(manager, position, references);

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.Operator, symbol);
            invoker.CollectSemanticToken(manager, collector);
        }
    }
    internal class BlurrySetExpression : Expression
    {
        public readonly BracketExpression expression;
        public override bool Valid => expression.Valid;

        public BlurrySetExpression(BracketExpression expression, LocalContextSnapshoot snapshoot) : base(expression.range, TUPLE_BLURRY, snapshoot)
        {
            this.expression = expression;
            attribute = ExpressionAttribute.Value | ExpressionAttribute.Array;
        }
        public override void Read(ExpressionParameter parameter)
        {
            parameter.collector.Add(range, ErrorLevel.Error, "无法推断集合类型");
            expression.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (expression.range.Contain(position)) return expression.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (expression.range.Contain(position)) return expression.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            expression.Operator(action);
            action(this);
        }

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

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector) => expression.CollectSemanticToken(manager, collector);
    }
    internal class BlurryLambdaExpression : Expression
    {
        public readonly List<TextRange> parameters;
        public readonly TextRange symbol;
        public readonly TextRange body;
        public override bool Valid => true;
        public BlurryLambdaExpression(TextRange range, LocalContextSnapshoot snapshoot, List<TextRange> parameters, TextRange symbol, TextRange body) : base(range, TUPLE_BLURRY, snapshoot)
        {
            this.parameters = parameters;
            this.symbol = symbol;
            this.body = body;
            attribute = ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            parameter.collector.Add(range, ErrorLevel.Error, "无法推断lambda表达式类型");
        }
        public override bool Operator(TextPosition position, ExpressionOperator action) => action(this);
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action) => action(this);
        public override void Operator(Action<Expression> action) => action(this);

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => false;

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => false;

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            foreach (var parameter in parameters)
                collector.Add(DetailTokenType.Local, parameter);
            collector.Add(DetailTokenType.Operator, symbol);
            collector.Add(DetailTokenType.Label, body);
        }
    }
}
