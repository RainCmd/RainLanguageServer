namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
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
    }
    internal class MethodExpression : Expression//global & native
    {
        public readonly TextRange? qualifier;
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
    }
}
