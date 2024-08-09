namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class MethodExpression : Expression//global & native
    {
        public readonly List<AbstractCallable> callables;
        public override bool Valid => true;
        public MethodExpression(TextRange range, List<AbstractCallable> callables) : base(range, TUPLE_BLURRY)
        {
            this.callables = callables;
            attribute = ExpressionAttribute.Method | ExpressionAttribute.Value;
        }
    }
    internal class MethodMemberExpression : Expression
    {
        public readonly Expression target;
        public readonly List<AbstractCallable> callables;
        public override bool Valid => true;
        public MethodMemberExpression(TextRange range, Expression target, List<AbstractCallable> callables) : base(range, TUPLE_BLURRY)
        {
            this.target = target;
            this.callables = callables;
            attribute = ExpressionAttribute.Method | ExpressionAttribute.Value;
        }
    }
    internal class MethodVirtualExpression : MethodMemberExpression
    {
        public MethodVirtualExpression(TextRange range, Expression target, List<AbstractCallable> callables) : base(range, target, callables)
        {
        }
    }
    internal class BlurryTaskExpression : Expression
    {
        public readonly InvokerExpression invoker;

        public BlurryTaskExpression(TextRange range, InvokerExpression invoker) : base(range, TUPLE_BLURRY)
        {
            this.invoker = invoker;
            attribute = ExpressionAttribute.Value;
        }

        public override bool Valid => true;

    }
    internal class BlurrySetExpression : Expression
    {
        public readonly TextRange? type;
        public readonly BracketExpression expression;
        public override bool Valid => expression.Valid;

        public BlurrySetExpression(BracketExpression expression) : base(expression.range, TUPLE_BLURRY)
        {
            this.expression = expression;
            attribute = ExpressionAttribute.Value | ExpressionAttribute.Array;
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
    }
}
