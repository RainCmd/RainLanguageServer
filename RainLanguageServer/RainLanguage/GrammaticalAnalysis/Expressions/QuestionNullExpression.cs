namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class QuestionNullExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression left;
        public readonly Expression right;
        public override bool Valid => left.Valid;

        public QuestionNullExpression(TextRange symbol, Expression left, Expression right, LocalContextSnapshoot snapshoot) : base(left.range & right.range, left.tuple, snapshoot)
        {
            this.symbol = symbol;
            this.left = left;
            this.right = right;
            attribute = left.attribute & ~ExpressionAttribute.Assignable;
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
