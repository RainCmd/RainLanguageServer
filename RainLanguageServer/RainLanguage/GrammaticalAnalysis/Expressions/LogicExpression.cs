﻿
namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class LogicExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly Expression left, right;
        public override bool Valid => true;

        public LogicExpression(TextRange range, TextRange symbol, Expression left, Expression right, Manager.KernelManager manager) : base(range, manager.BOOL)
        {
            this.symbol = symbol;
            this.left = left;
            this.right = right;
            attribute = ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            left.Read(parameter);
            right.Read(parameter);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (left.range.Contain(position)) return left.OnHover(manager, position, out info);
            if (right.range.Contain(position)) return right.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (left.range.Contain(position)) return left.OnHighlight(manager, position, infos);
            if (right.range.Contain(position)) return right.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (left.range.Contain(position)) return left.TryGetDefinition(manager, position, out definition);
            if (right.range.Contain(position)) return right.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (left.range.Contain(position)) return left.FindReferences(manager, position, references);
            if (right.range.Contain(position)) return right.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.Operator, symbol);
            left.CollectSemanticToken(manager, collector);
            right.CollectSemanticToken(manager, collector);
        }
    }
}