using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class InvalidExpression : Expression
    {
        public readonly IList<Expression> expressions;
        public override bool Valid => false;
        public InvalidExpression(TextRange range, LocalContextSnapshoot snapshoot) : base(range, Tuple.Empty, snapshoot)
        {
            expressions = [];
            attribute = ExpressionAttribute.Invalid;
        }
        public InvalidExpression(LocalContextSnapshoot snapshoot, params Expression[] expressions) : this(expressions, snapshoot) { }
        public InvalidExpression(TextRange range, LocalContextSnapshoot snapshoot, IList<Expression> expressions) : base(range, Tuple.Empty, snapshoot)
        {
            this.expressions = expressions;
            attribute = ExpressionAttribute.Invalid;
        }
        public InvalidExpression(IList<Expression> expressions, LocalContextSnapshoot snapshoot) : base(expressions[0].range & expressions[^1].range, Tuple.Empty, snapshoot)
        {
            this.expressions = expressions;
            attribute = ExpressionAttribute.Invalid;
        }
        public InvalidExpression(Expression expression, Tuple tuple, LocalContextSnapshoot snapshoot) : base(expression.range, tuple, snapshoot)
        {
            expressions = [expression];
            attribute = ExpressionAttribute.Invalid;
        }
        public override void Read(ExpressionParameter parameter)
        {
            foreach (var expression in expressions) expression.Read(parameter);
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

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            foreach (var expression in expressions)
                if (expression.range.Contain(position))
                    return expression.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            foreach (var expression in expressions)
                if (expression.range.Contain(position))
                    return expression.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            foreach (var expression in expressions)
                if (expression.range.Contain(position))
                    return expression.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            foreach (var expression in expressions)
                if (expression.range.Contain(position))
                    return expression.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            foreach (var expression in expressions)
                expression.CollectSemanticToken(manager, collector);
        }

        public override int GetTupleIndex(TextPosition position)
        {
            var result = 0;
            foreach (var expression in expressions)
                if (expression.range.start < position) break;
                else result += expression.tuple.Count;
            return result;
        }
    }
    internal class InvalidKeyworldExpression(TextRange range, LocalContextSnapshoot snapshoot) : InvalidExpression(range, snapshoot) { }
    internal class InvalidOperationExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression? parameters;
        public override bool Valid => false;

        public InvalidOperationExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange symbol, Expression? parameters = null) : base(range, Tuple.Empty, snapshoot)
        {
            this.symbol = symbol;
            this.parameters = parameters;
            attribute = ExpressionAttribute.Invalid;
        }
        public override void Read(ExpressionParameter parameter) => parameters?.Read(parameter);
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (parameters != null && parameters.range.Contain(position))
                return parameters.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (parameters != null && parameters.range.Contain(position))
                return parameters.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            parameters?.Operator(action);
            action(this);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (parameters != null && parameters.range.Contain(position))
                return parameters.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (parameters != null && parameters.range.Contain(position))
                return parameters.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (parameters != null && parameters.range.Contain(position))
                return parameters.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (parameters != null && parameters.range.Contain(position))
                return parameters.FindReferences(manager, position, references);
            return false;
        }

        protected override bool InternalCompletion(Manager manager, TextPosition position, List<CompletionInfo> infos)
        {
            if (parameters != null)
            {
                if (parameters is InvalidExpression)
                {
                    if (Lexical.TryExtractName(range, 0, out var names, null))
                    {
                        if ((names[0] & names[^1]).Contain(position) && ManagerOperator.TryGetContext(manager, position, out var context))
                        {
                            InfoUtility.Completion(manager, context, names, position, infos, CompletionFilter.All);
                            return default;
                        }
                    }
                }
                else if (parameters.attribute.ContainAny(ExpressionAttribute.Value) && symbol == "." && ManagerOperator.TryGetContext(manager, position, out var context))
                {
                    InfoUtility.CollectMember(manager, parameters.tuple[0], context, infos);
                    return default;
                }
            }
            return base.InternalCompletion(manager, position, infos);
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.Operator, symbol);
            parameters?.CollectSemanticToken(manager, collector);
        }
    }
    internal class InvalidInvokerExpression : Expression
    {
        public readonly Expression method;
        public readonly BracketExpression parameters;
        private readonly List<AbstractCallable> callables;
        public override bool Valid => false;
        public InvalidInvokerExpression(TextRange range, LocalContextSnapshoot snapshoot, Expression method, BracketExpression parameters) : base(range, Tuple.Empty, snapshoot)
        {
            this.method = method;
            this.parameters = parameters;
            if (method is MethodExpression methodExpression)
                callables = methodExpression.callables;
            else if (method is MethodMemberExpression methodMemberExpression)
                callables = methodMemberExpression.callables;
            else callables = [];
            attribute = ExpressionAttribute.Invalid;
        }

        public override void Read(ExpressionParameter parameter)
        {
            method.Read(parameter);
            parameters.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (method.range.Contain(position)) return method.Operator(position, action);
            if (parameters.range.Contain(position)) return parameters.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (method.range.Contain(position)) return method.BreadthFirstOperator(position, action);
            if (parameters.range.Contain(position)) return parameters.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            method.Operator(action);
            parameters.Operator(action);
            action(this);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (method.range.Contain(position)) return method.OnHover(manager, position, out info);
            if (parameters.range.Contain(position)) return parameters.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (method.range.Contain(position)) return method.OnHighlight(manager, position, infos);
            if (parameters.range.Contain(position)) return parameters.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (method.range.Contain(position)) return method.TryGetDefinition(manager, position, out definition);
            if (parameters.range.Contain(position)) return parameters.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (method.range.Contain(position)) return method.FindReferences(manager, position, references);
            if (parameters.range.Contain(position)) return parameters.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            method.CollectSemanticToken(manager, collector);
            parameters.CollectSemanticToken(manager, collector);
        }

        protected override bool InternalTrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            if (parameters.range.Contain(position))
            {
                if (parameters.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex)) return true;
                infos = [];
                functionIndex = 0;
                parameterIndex = parameters.GetTupleIndex(position);
                foreach (var callable in callables)
                {
                    if (manager.TryGetDefineDeclaration(callable.declaration, out var declaration) && declaration == callable) declaration = null;
                    infos.Add(callable.GetSignatureInfo(manager, declaration, ManagerOperator.GetSpace(manager, position)));
                }
                return true;
            }
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }
    }
}
