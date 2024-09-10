namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
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
        public TupleExpression(TextRange range, Tuple tuple, LocalContextSnapshoot snapshoot, IList<Expression> expressions) : base(range, tuple, snapshoot)
        {
            this.expressions = expressions;
            attribute = ExpressionAttribute.Assignable;
            foreach (var expression in expressions) attribute &= expression.attribute;
            if (tuple.Count == 1) attribute |= ExpressionAttribute.Value;
            else attribute |= ExpressionAttribute.Tuple;
        }
        public TupleExpression(TextRange range, LocalContextSnapshoot snapshoot) : this(range, Tuple.Empty, snapshoot, empty) { }
        public override bool TryEvaluateIndices(List<long> indices)
        {
            foreach (var expression in expressions)
                if (!expression.TryEvaluateIndices(indices)) return false;
            return true;
        }
        public override bool Calculability()
        {
            foreach (var expression in expressions)
                if (!expression.Calculability())
                    return false;
            return true;
        }
        public override void Read(ExpressionParameter parameter)
        {
            foreach (var expression in expressions) expression.Read(parameter);
        }
        public override void Write(ExpressionParameter parameter)
        {
            foreach (var expression in expressions) expression.Write(parameter);
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
            if (action(this)) return true;
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

        public override int GetTupleIndex(TextPosition position)
        {
            var result = 0;
            foreach (var expression in expressions)
                if (position > expression.range.end)
                    result += expression.tuple.Count;
            return result;
        }

        public static Expression Create(IList<Expression> expressions, LocalContextSnapshoot snapshoot, MessageCollector collector)
        {
            if (expressions.Count == 0) throw new Exception("至少需要一个表达式，否则无法计算表达式范围");
            var types = new List<Type>();
            foreach (var expression in expressions)
            {
                if (expression.attribute == ExpressionAttribute.Invalid) return new InvalidExpression(expressions, snapshoot);
                else if (expression.attribute.ContainAny(ExpressionAttribute.Assignable | ExpressionAttribute.Value | ExpressionAttribute.Tuple)) types.AddRange(expression.tuple);
                else
                {
                    collector.Add(expression.range, ErrorLevel.Error, "无效的操作");
                    return new InvalidExpression(expressions, snapshoot);
                }
            }
            return new TupleExpression(expressions[0].range & expressions[^1].range, new Tuple([.. types]), snapshoot, expressions);
        }

        private static readonly IList<Expression> empty = [];
    }
    internal class TupleEvaluationExpression : Expression
    {
        public readonly Expression source;
        public readonly BracketExpression indices;
        public override bool Valid => true;
        public TupleEvaluationExpression(TextRange range, Tuple tuple, LocalContextSnapshoot snapshoot, Expression source, BracketExpression indices, Manager.KernelManager manager) : base(range, tuple, snapshoot)
        {
            this.source = source;
            this.indices = indices;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
        }
        public override void Read(ExpressionParameter parameter)
        {
            source.Read(parameter);
            indices.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (source.range.Contain(position)) return source.Operator(position, action);
            if (indices.range.Contain(position)) return indices.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (source.range.Contain(position)) return source.BreadthFirstOperator(position, action);
            if (indices.range.Contain(position)) return indices.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            source.Operator(action);
            indices.Operator(action);
            action(this);
        }
    }
}
