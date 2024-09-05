﻿
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class TaskCreateExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly InvokerExpression invoker;
        public override bool Valid => true;

        public TaskCreateExpression(TextRange range, Type type, TextRange symbol, InvokerExpression invoker, Manager.KernelManager manager) : base(range, type)
        {
            this.symbol = symbol;
            this.invoker = invoker;
            attribute = ExpressionAttribute.Value | type.GetAttribute(manager);
        }
        public override void Read(ExpressionParameter parameter) => invoker.Read(parameter);

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (invoker.range.Contain(position)) return invoker.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => invoker.range.Contain(position) && invoker.OnHighlight(manager, position, infos);

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (invoker.range.Contain(position)) return invoker.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => invoker.range.Contain(position) && invoker.FindReferences(manager, position, references);

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.KeywordCtrl, symbol);
            invoker.CollectSemanticToken(manager, collector);
        }

        public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            if (invoker.range.Contain(position)) return invoker.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }
    }
    internal class TaskEvaluationExpression : Expression
    {
        public readonly Expression source;
        public readonly BracketExpression indices;
        public override bool Valid => true;
        public TaskEvaluationExpression(TextRange range, Tuple tuple, Expression source, BracketExpression indices, Manager.KernelManager manager) : base(range, tuple)
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

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (source.range.Contain(position)) return source.OnHover(manager, position, out info);
            if (indices.range.Contain(position)) return indices.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (source.range.Contain(position)) return source.OnHighlight(manager, position, infos);
            if (indices.range.Contain(position)) return indices.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (source.range.Contain(position)) return source.TryGetDefinition(manager, position, out definition);
            if (indices.range.Contain(position)) return indices.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (source.range.Contain(position)) return source.FindReferences(manager, position, references);
            if (indices.range.Contain(position)) return indices.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            source.CollectSemanticToken(manager, collector);
            indices.CollectSemanticToken(manager, collector);
        }

        public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            if (source.range.Contain(position)) return source.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            if (indices.range.Contain(position)) return indices.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }
    }
}
