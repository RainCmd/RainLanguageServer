
namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class OperationExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly AbstractCallable callable;
        public readonly Expression parameters;
        public override bool Valid => true;

        public OperationExpression(TextRange range, TextRange symbol, AbstractCallable callable, Expression parameters, Manager.KernelManager manager) : base(range, callable.returns)
        {
            this.symbol = symbol;
            this.callable = callable;
            this.parameters = parameters;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
        }
        public override void Read(ExpressionParameter parameter)
        {
            callable.references.Add(symbol);
            parameters.Read(parameter);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (symbol.Contain(position))
            {
                info = new HoverInfo(symbol, callable.Info(manager, null, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (symbol.Contain(position))
            {
                InfoUtility.Highlight(callable, infos);
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (symbol.Contain(position))
            {
                definition = callable.name;
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (symbol.Contain(position))
            {
                references.AddRange(callable.references);
                return true;
            }
            if (parameters.range.Contain(position)) return parameters.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.Operator, symbol);
            parameters.CollectSemanticToken(manager, collector);
        }
    }
}
