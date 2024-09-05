﻿
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class InvalidExpression : Expression
    {
        public readonly IList<Expression> expressions;
        public override bool Valid => false;
        public InvalidExpression(TextRange range) : base(range, Tuple.Empty)
        {
            expressions = [];
            attribute = ExpressionAttribute.Invalid;
        }
        public InvalidExpression(params Expression[] expressions) : this((IList<Expression>)expressions) { }
        public InvalidExpression(TextRange range, IList<Expression> expressions) : base(range, Tuple.Empty)
        {
            this.expressions = expressions;
            attribute = ExpressionAttribute.Invalid;
        }
        public InvalidExpression(IList<Expression> expressions) : base(expressions[0].range & expressions[^1].range, Tuple.Empty)
        {
            this.expressions = expressions;
            attribute = ExpressionAttribute.Invalid;
        }
        public InvalidExpression(Expression expression, Tuple tuple) : base(expression.range, tuple)
        {
            expressions = [expression];
            attribute = ExpressionAttribute.Invalid;
        }
        public override void Read(ExpressionParameter parameter)
        {
            foreach (var expression in expressions) expression.Read(parameter);
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
        public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            foreach (var expression in expressions)
                if (expression.range.Contain(position))
                    return expression.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }
    }
    internal class InvalidKeyworldExpression(TextRange range) : InvalidExpression(range) { }
    internal class InvalidOperationExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression? parameters;
        public override bool Valid => false;

        public InvalidOperationExpression(TextRange range, TextRange symbol, Expression? parameters = null) : base(range, Tuple.Empty)
        {
            this.symbol = symbol;
            this.parameters = parameters;
            attribute = ExpressionAttribute.Invalid;
        }
        public override void Read(ExpressionParameter parameter) => parameters?.Read(parameter);

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

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.Operator, symbol);
            parameters?.CollectSemanticToken(manager, collector);
        }

        public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            if (parameters != null && parameters.range.Contain(position)) return parameters.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }
    }
}
