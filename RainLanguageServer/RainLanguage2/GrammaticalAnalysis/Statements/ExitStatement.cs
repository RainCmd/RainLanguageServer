﻿
namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class ExitStatement : Statement
    {
        public readonly TextRange symbol;
        public readonly Expression expression;
        public readonly List<TextRange> group;

        public ExitStatement(TextRange symbol, Expression expression, List<TextRange> group)
        {
            range = symbol & expression.range;
            this.symbol = symbol;
            this.expression = expression;
            this.group = group;
            group.Add(symbol);
        }
        public override void Operator(Action<Expression> action) => action(expression);
        public override bool Operator(TextPosition position, ExpressionOperator action) => expression.range.Contain(position) && action(expression);
    }
}