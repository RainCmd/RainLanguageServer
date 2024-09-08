namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class TaskCreateExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly InvokerExpression invoker;
        public override bool Valid => true;

        public TaskCreateExpression(TextRange range, Type type, LocalContextSnapshoot snapshoot, TextRange symbol, InvokerExpression invoker, Manager.KernelManager manager) : base(range, type, snapshoot)
        {
            this.symbol = symbol;
            this.invoker = invoker;
            attribute = ExpressionAttribute.Value | type.GetAttribute(manager);
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

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.KeywordCtrl, symbol);
    }
    internal class TaskEvaluationExpression : Expression
    {
        public readonly Expression source;
        public readonly BracketExpression indices;
        public override bool Valid => true;
        public TaskEvaluationExpression(TextRange range, Tuple tuple, LocalContextSnapshoot snapshoot, Expression source, BracketExpression indices, Manager.KernelManager manager) : base(range, tuple, snapshoot)
        {
            this.source = source;
            this.indices = indices;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
        }
        public override void Read(ExpressionParameter parameter)
        {
            source.Read(parameter);
            indices.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (source.range.Contain(position)) return source.Operator(position, action);
            if (indices.range.Contain(position)) return indices.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (source.range.Contain(position)) return source.BreadthFirstOperator(position, action);
            if (indices.range.Contain(position)) return indices.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            source.Operator(action);
            indices.Operator(action);
            action(this);
        }
    }
}
