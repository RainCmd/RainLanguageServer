namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class TupleExpression : Expression
    {
        public readonly IList<Expression> expressions;
        public override bool Valid
        {
            get
            {
                foreach (var expression in expressions) if (!expression.Valid) return false;
                return true;
            }
        }
        public TupleExpression(TextRange range, Tuple tuple, IList<Expression> expressions) : base(range, tuple)
        {
            this.expressions = expressions;
            attribute = ExpressionAttribute.Assignable;
            foreach (var expression in expressions) attribute &= expression.attribute;
            if (tuple.Count == 1) attribute |= ExpressionAttribute.Value;
            else attribute |= ExpressionAttribute.Tuple;
        }
        public TupleExpression(TextRange range) : this(range, Tuple.Empty, empty) { }
        public override bool TryEvaluateIndices(List<long> indices)
        {
            foreach (var expression in expressions)
                if (!expression.TryEvaluateIndices(indices)) return false;
            return true;
        }
        public static Expression Create(IList<Expression> expressions, MessageCollector collector)
        {
            if (expressions.Count == 0) throw new Exception("至少需要一个表达式，否则无法计算表达式范围");
            var types = new List<Type>();
            foreach (var expression in expressions)
            {
                if (expression.attribute == ExpressionAttribute.Invalid) return new InvalidExpression(expressions);
                else if (expression.attribute.ContainAny(ExpressionAttribute.Value | ExpressionAttribute.Tuple)) types.AddRange(expression.tuple);
                else
                {
                    collector.Add(expression.range, ErrorLevel.Error, "无效的操作");
                    return new InvalidExpression(expressions);
                }
            }
            return new TupleExpression(expressions[0].range & expressions[^1].range, new Tuple([.. types]), expressions);
        }
        private static readonly IList<Expression> empty = [];
    }
    internal class TupleEvaluationExpression : Expression
    {
        public readonly Expression source;
        public readonly BracketExpression indices;
        public override bool Valid => true;
        public TupleEvaluationExpression(TextRange range, Tuple tuple, Expression source, BracketExpression indices, Manager.KernelManager manager) : base(range, tuple)
        {
            this.source = source;
            this.indices = indices;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
        }
    }
}
