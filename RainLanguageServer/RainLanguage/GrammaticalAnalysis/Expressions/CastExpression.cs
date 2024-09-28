namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class CastExpression : Expression
    {
        public readonly TypeExpression type;
        public readonly TextRange symbol;
        public readonly Expression expression;
        public Manager.KernelManager manager;
        public override bool Valid => true;

        public CastExpression(TextRange range, TypeExpression type, LocalContextSnapshoot snapshoot, TextRange symbol, Expression expression, Manager.KernelManager manager) : base(range, type.type, snapshoot)
        {
            this.type = type;
            this.symbol = symbol;
            this.expression = expression;
            this.manager = manager;
            attribute = ExpressionAttribute.Value | type.type.GetAttribute(manager);
        }
        public override bool TryEvaluateIndices(List<long> indices)
        {
            if (tuple.Count == 1 && tuple[0] == manager.INT) return expression.TryEvaluateIndices(indices);
            return false;
        }
        public override void Read(ExpressionParameter parameter)
        {
            type.Read(parameter);
            expression.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (type.range.Contain(position)) return type.Operator(position, action);
            if (expression.range.Contain(position)) return expression.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (type.range.Contain(position)) return type.BreadthFirstOperator(position, action);
            if (expression.range.Contain(position)) return expression.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            type.Operator(action);
            expression.Operator(action);
            action(this);
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.Operator, symbol);
    }
    internal class TupleCastExpression : Expression
    {
        public readonly Expression expression;
        public override bool Valid => true;
        public TupleCastExpression(Expression expression, Tuple tuple, LocalContextSnapshoot snapshoot, Manager.KernelManager manager) : base(expression.range, tuple, snapshoot)
        {
            this.expression = expression;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
            attribute |= expression.attribute & ~ExpressionAttribute.Assignable;
        }
        public override void Read(ExpressionParameter parameter) => expression.Read(parameter);
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (expression.range.Contain(position)) return expression.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (expression.range.Contain(position)) return expression.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            expression.Operator(action);
            action(this);
        }

        public override int GetTupleIndex(TextPosition position)
        {
            if (expression.range.Contain(position)) return expression.GetTupleIndex(position);
            return 0;
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

        public IsCastExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange symbol, TextRange? identifier, Expression source, TypeExpression type, Local? local, Manager.KernelManager manager) : base(range, manager.BOOL, snapshoot)
        {
            this.symbol = symbol;
            this.identifier = identifier;
            this.source = source;
            this.type = type;
            this.local = local;
            attribute = ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            source.Read(parameter);
            type.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (source.range.Contain(position)) return source.Operator(position, action);
            if (type.range.Contain(position)) return type.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (source.range.Contain(position)) return source.BreadthFirstOperator(position, action);
            if (type.range.Contain(position)) return type.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            source.Operator(action);
            type.Operator(action);
            action(this);
        }

        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (local != null && local.Value.range.Contain(position))
            {
                info = local.Value.Hover(manager, position);
                return true;
            }
            info = default;
            return false;
        }

        protected override bool InternalOnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (local != null && local.Value.range.Contain(position))
            {
                local.Value.OnHighlight(infos);
                return true;
            }
            return false;
        }

        protected override bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (local != null && local.Value.range.Contain(position))
            {
                definition = local.Value.range;
                return true;
            }
            definition = default;
            return false;
        }

        protected override bool InternalFindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (local != null && local.Value.range.Contain(position))
            {
                local.Value.FindReferences(references);
                return true;
            }
            return false;
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.KeywordCtrl, symbol);
            if (identifier != null) collector.Add(DetailTokenType.Local, identifier.Value);
        }

        protected override void InternalRename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (identifier != null && identifier.Value.Contain(position) && local != null) local.Value.Rename(ranges);
        }
    }
    internal class AsCastExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression source;
        public readonly TypeExpression type;
        public override bool Valid => true;

        public AsCastExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange symbol, Expression source, TypeExpression type) : base(range, type.type, snapshoot)
        {
            this.symbol = symbol;
            this.source = source;
            this.type = type;
            attribute = ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            source.Read(parameter);
            type.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (source.range.Contain(position)) return source.Operator(position, action);
            if (type.range.Contain(position)) return type.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (source.range.Contain(position)) return source.BreadthFirstOperator(position, action);
            if (type.range.Contain(position)) return type.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            source.Operator(action);
            type.Operator(action);
            action(this);
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.KeywordCtrl, symbol);
    }
}
