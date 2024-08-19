namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class CastExpression : Expression
    {
        public readonly TypeExpression type;
        public readonly TextRange symbol;
        public readonly Expression expression;
        public override bool Valid => true;

        public CastExpression(TextRange range, TypeExpression type, TextRange symbol, Expression expression, Manager.KernelManager manager) : base(range, type.type)
        {
            this.type = type;
            this.symbol = symbol;
            this.expression = expression;
            attribute = ExpressionAttribute.Value | type.type.GetAttribute(manager);
        }

    }
    internal class TupleCastExpression : Expression
    {
        public readonly Expression expression;
        public override bool Valid => true;
        public TupleCastExpression(Expression expression, Tuple tuple, Manager.KernelManager manager) : base(expression.range, tuple)
        {
            this.expression = expression;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
            attribute |= expression.attribute & ~ExpressionAttribute.Assignable;
        }
    }
    internal class IsCastExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly TextRange? identifier;
        public readonly Expression source;
        public readonly TypeExpression type;
        public readonly Local? local;
        public override bool Valid => true;

        public IsCastExpression(TextRange range, TextRange symbol, TextRange? identifier, Expression source, TypeExpression type, Local? local, Manager.KernelManager manager) : base(range, manager.BOOL)
        {
            this.symbol = symbol;
            this.identifier = identifier;
            this.source = source;
            this.type = type;
            this.local = local;
            attribute = ExpressionAttribute.Value;
        }
    }
    internal class AsCastExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression source;
        public readonly TypeExpression type;
        public override bool Valid => true;

        public AsCastExpression(TextRange range, TextRange symbol, Expression source, TypeExpression type) : base(range, type.type)
        {
            this.symbol = symbol;
            this.source = source;
            this.type = type;
            attribute = ExpressionAttribute.Value;
        }
    }
}
