

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
{
    internal class ExpressionStatement : Statement
    {
        public readonly Expression expression;

        public ExpressionStatement(Expression expression)
        {
            range = expression.range;
            this.expression = expression;
        }

        public override void Operator(Action<Expression> action) => action(expression);
        public override bool Operator(TextPosition position, ExpressionOperator action) => expression.range.Contain(position) && action(expression);
        public override bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos) => false;
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector) => expression.CollectSemanticToken(manager, collector);
    }
}
