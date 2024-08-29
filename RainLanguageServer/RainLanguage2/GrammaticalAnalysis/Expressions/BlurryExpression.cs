namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class BlurryVariableDeclarationExpression : Expression
    {
        public readonly TextRange declaration;
        public readonly TextRange identifier;
        public override bool Valid => true;

        public BlurryVariableDeclarationExpression(TextRange range, TextRange declaration, TextRange identifier) : base(range, TUPLE_BLURRY)
        {
            this.declaration = declaration;
            this.identifier = identifier;
            attribute = ExpressionAttribute.Assignable;
        }
        public override void Read(ExpressionParameter parameter) => parameter.collector.Add(declaration, ErrorLevel.Error, "无法推断类型");

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
    }
    internal class MethodExpression : Expression//global & native
    {
        public readonly TextRange? qualifier;// global关键字
        public readonly QualifiedName name;
        public readonly List<AbstractCallable> callables;
        public override bool Valid => true;
        public MethodExpression(TextRange range, TextRange? qualifier, QualifiedName name, List<AbstractCallable> callables) : base(range, TUPLE_BLURRY)
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
    }
    internal class MethodMemberExpression : Expression
    {
        public readonly TextRange? symbol;
        public readonly TextRange member;
        public readonly Expression? target;
        public readonly List<AbstractCallable> callables;
        public override bool Valid => true;
        public MethodMemberExpression(TextRange range, TextRange? symbol, TextRange member, Expression? target, List<AbstractCallable> callables) : base(range, TUPLE_BLURRY)
        {
            this.target = target;
            this.symbol = symbol;
            this.member = member;
            this.callables = callables;
            attribute = ExpressionAttribute.Method | ExpressionAttribute.Value;
        }
        public MethodMemberExpression(TextRange range, TextRange member, List<AbstractCallable> callables) : this(range, null, member, null, callables) { }
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
    }
    internal class MethodVirtualExpression : MethodMemberExpression
    {
        public MethodVirtualExpression(TextRange range, TextRange? symbol, TextRange member, Expression? target, List<AbstractCallable> callables) : base(range, symbol, member, target, callables) { }
        public MethodVirtualExpression(TextRange range, TextRange member, List<AbstractCallable> callables) : base(range, member, callables) { }
        public override void Read(ExpressionParameter parameter)
        {
            var msg = new Message(member, ErrorLevel.Error, "语义不明确");
            foreach (var callable in callables)
            {
                msg.related.Add(new RelatedInfo(callable.name, "符合条件的函数"));
                if (callable is AbstractClass.Function function)
                    Reference(function);
            }
            parameter.collector.Add(msg);
            target?.Read(parameter);
        }
        private void Reference(AbstractClass.Function function)
        {
            function.references.Add(member);
            foreach (var item in function.implements)
                Reference(item);
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
        public BlurryTaskExpression(TextRange range, TextRange symbol, InvokerExpression invoker) : base(range, TUPLE_BLURRY)
        {
            this.symbol = symbol;
            this.invoker = invoker;
            attribute = ExpressionAttribute.Value;
        }

        public override void Read(ExpressionParameter parameter) => invoker.Read(parameter);

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
    }
    internal class BlurrySetExpression : Expression
    {
        public readonly BracketExpression expression;
        public override bool Valid => expression.Valid;

        public BlurrySetExpression(BracketExpression expression) : base(expression.range, TUPLE_BLURRY)
        {
            this.expression = expression;
            attribute = ExpressionAttribute.Value | ExpressionAttribute.Array;
        }
        public override void Read(ExpressionParameter parameter)
        {
            parameter.collector.Add(range, ErrorLevel.Error, "无法推断集合类型");
            expression.Read(parameter);
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
    }
    internal class BlurryLambdaExpression : Expression
    {
        public readonly List<TextRange> parameters;
        public readonly TextRange symbol;
        public readonly TextRange body;
        public override bool Valid => true;
        public BlurryLambdaExpression(TextRange range, List<TextRange> parameters, TextRange symbol, TextRange body) : base(range, TUPLE_BLURRY)
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
    }
}
