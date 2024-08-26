namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class ExpressionStatement : Statement
    {
        public readonly Expression expression;

        public ExpressionStatement(Expression expression)
        {
            range = expression.range;
            this.expression = expression;
        }
        public override void Read(StatementParameter parameter) => expression.Read(parameter);
    }
}
