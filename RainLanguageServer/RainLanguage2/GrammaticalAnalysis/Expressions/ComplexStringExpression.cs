namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class ComplexStringExpression : Expression
    {
        public readonly List<Expression> expressions;
        public override bool Valid => true;
        public ComplexStringExpression(TextRange range, List<Expression> expressions, Manager.KernelManager manager) : base(range, manager.STRING)
        {
            this.expressions = expressions;
            attribute = ExpressionAttribute.Value | manager.STRING.GetAttribute(manager);
        }
    }
}
