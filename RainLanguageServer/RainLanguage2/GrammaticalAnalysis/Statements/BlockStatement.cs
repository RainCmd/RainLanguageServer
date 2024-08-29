
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
            foreach(var statement in statements) statement.Operator(action);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            foreach(var statement in statements)
                if(statement.range.Contain(position))
                    return statement.Operator(position, action);
            return false;
        }
    }
}
