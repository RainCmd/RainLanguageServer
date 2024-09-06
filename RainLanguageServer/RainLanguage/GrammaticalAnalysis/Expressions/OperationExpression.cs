using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class OperationExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly AbstractCallable callable;
        public readonly Expression parameters;
        public override bool Valid => true;

        public OperationExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange symbol, AbstractCallable callable, Expression parameters, Manager.KernelManager manager) : base(range, callable.returns, snapshoot)
        {
            this.symbol = symbol;
            this.callable = callable;
            this.parameters = parameters;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
        }
        public override bool Calculability() => callable.declaration.library == Manager.LIBRARY_KERNEL && parameters.Calculability();
        public override void Read(ExpressionParameter parameter)
        {
            callable.references.Add(symbol);
            parameters.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (parameters.range.Contain(position)) return parameters.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (parameters.range.Contain(position)) return parameters.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            parameters.Operator(action);
            action(this);
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

        protected override bool InternalTrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            if (parameters.range.Contain(position))
            {
                if (parameters.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex)) return true;
                infos = [];
                infos.Add(callable.GetSignatureInfo(manager, null, ManagerOperator.GetSpace(manager, position)));
                functionIndex = 0;
                parameterIndex = parameters.GetTupleIndex(position);
                return true;
            }
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }
    }
}
