namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class ComplexStringExpression : Expression
    {
        public readonly List<Expression> expressions;
        public override bool Valid => true;
        public ComplexStringExpression(TextRange range, LocalContextSnapshoot snapshoot, List<Expression> expressions, Manager.KernelManager manager) : base(range, manager.STRING, snapshoot)
        {
            this.expressions = expressions;
            attribute = ExpressionAttribute.Value | manager.STRING.GetAttribute(manager);
        }
        public override void Read(ExpressionParameter parameter)
        {
            foreach (var expression in expressions) expression.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            foreach (var expression in expressions)
                if (expression.range.Contain(position))
                    return expression.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if(action(this)) return true;
            foreach (var expression in expressions)
                if (expression.range.Contain(position))
                    return expression.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            foreach (var expression in expressions)
                expression.Operator(action);
            action(this);
        }
    }
}
