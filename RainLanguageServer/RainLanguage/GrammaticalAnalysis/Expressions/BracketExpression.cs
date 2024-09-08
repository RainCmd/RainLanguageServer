namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class BracketExpression : Expression
    {
        public readonly TextRange left, right;
        public readonly Expression expression;
        public override bool Valid => expression.Valid;
        public BracketExpression(TextRange left, TextRange right, Expression expression, LocalContextSnapshoot snapshoot) : base(left & right, expression.tuple, snapshoot)
        {
            this.left = left;
            this.right = right;
            this.expression = expression;
            attribute = expression.attribute;
        }
        public BracketExpression Replace(Expression expression)
        {
            if (this.expression == expression) return this;
            return new BracketExpression(left, right, expression, snapshoot);
        }
        public override bool TryEvaluateIndices(List<long> indices)
        {
            return expression.TryEvaluateIndices(indices);
        }
        public override bool Calculability() => expression.Calculability();
        public override void Read(ExpressionParameter parameter) => expression.Read(parameter);
        public override void Write(ExpressionParameter parameter) => expression.Write(parameter);
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

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.Operator, left);
            collector.Add(DetailTokenType.Operator, right);
        }

        public override int GetTupleIndex(TextPosition position)
        {
            if (expression.range.Contain(position)) return expression.GetTupleIndex(position);
            return 0;
        }
    }
}
