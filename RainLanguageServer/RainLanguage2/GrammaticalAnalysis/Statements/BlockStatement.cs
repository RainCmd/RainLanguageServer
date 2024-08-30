
namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
{
    internal class BlockStatement : Statement
    {
        public readonly List<Statement> statements = [];
        public BlockStatement(TextRange range)
        {
            base.range = range;
        }

        public override void Operator(Action<Expression> action)
        {
            foreach (var statement in statements) statement.Operator(action);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            foreach (var statement in statements)
                if (statement.range.Contain(position))
                    return statement.Operator(position, action);
            return false;
        }
        public override bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos)
        {
            foreach(var statement in statements)
                if (statement.range.Contain(position))
                    return statement.TryHighlightGroup(position, infos);
            return false;
        }
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            foreach (var statement in statements)
                statement.CollectSemanticToken(manager, collector);
        }
    }
}
