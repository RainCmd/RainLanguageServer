namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class LogicExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression left, right;
        public override bool Valid => true;

        public LogicExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange symbol, Expression left, Expression right, Manager.KernelManager manager) : base(range, manager.BOOL, snapshoot)
        {
            this.symbol = symbol;
            this.left = left;
            this.right = right;
            attribute = ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            left.Read(parameter);
            right.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (left.range.Contain(position)) return left.Operator(position, action);
            if (right.range.Contain(position)) return right.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (left.range.Contain(position)) return left.BreadthFirstOperator(position, action);
            if (right.range.Contain(position)) return right.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            left.Operator(action);
            right.Operator(action);
            action(this);
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.Operator, symbol);
    }
}
