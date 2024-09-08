namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class QuestionExpression : Expression
    {
        public readonly TextRange questionSymbol;
        public readonly TextRange? elseSymbol;
        public readonly Expression condition;
        public readonly Expression left;
        public readonly Expression? right;
        public override bool Valid => left.Valid;

        public QuestionExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange questionSymbol, TextRange? elseSymbol, Expression condition, Expression left, Expression? right) : base(range, left.tuple, snapshoot)
        {
            this.questionSymbol = questionSymbol;
            this.elseSymbol = elseSymbol;
            this.condition = condition;
            this.left = left;
            this.right = right;
            attribute = left.attribute & ~ExpressionAttribute.Assignable;
        }
        public override void Read(ExpressionParameter parameter)
        {
            condition.Read(parameter);
            left.Read(parameter);
            right?.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (condition.range.Contain(position)) return condition.Operator(position, action);
            if (left.range.Contain(position)) return left.Operator(position, action);
            if (right != null && right.range.Contain(position)) return right.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (condition.range.Contain(position)) return condition.BreadthFirstOperator(position, action);
            if (left.range.Contain(position)) return left.BreadthFirstOperator(position, action);
            if (right != null && right.range.Contain(position)) return right.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            condition.Operator(action);
            left.Operator(action);
            right?.Operator(action);
            action(this);
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.Operator, questionSymbol);
            if (elseSymbol != null) collector.Add(DetailTokenType.Operator, elseSymbol.Value);
        }
    }
}
