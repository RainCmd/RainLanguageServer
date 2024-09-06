using LanguageServer.Parameters;
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis
{
    internal abstract class Statement
    {
        public delegate bool ExpressionOperator(Expression expression);
        public delegate bool StatementOperator(Statement expression);
        public TextRange range;
        public void Read(ExpressionParameter parameter) => Operator((Expression value) => value.Read(parameter));
        protected virtual void InternalOperator(Action<Expression> action) { }
        protected void Operator(Action<Expression> action) => Operator((Statement value) => value.InternalOperator(action));
        public abstract void Operator(Action<Statement> action);

        protected virtual bool InternalOperator(TextPosition position, ExpressionOperator action) { return false; }
        protected bool Operator(TextPosition position, ExpressionOperator action) => Operator(position, (Statement value) => value.InternalOperator(position, action));
        public abstract bool Operator(TextPosition position, StatementOperator action);

        public bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            HoverInfo result = default;
            if (Operator(position, (Expression value) => value.OnHover(manager, position, out result)))
            {
                info = result;
                return true;
            }
            info = default;
            return false;
        }
        protected virtual bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos) { return false; }
        public bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (Operator(position, (Expression value) => value.OnHighlight(manager, position, infos))) return true;
            return Operator(position, (Statement value) => value.TryHighlightGroup(position, infos));
        }

        public bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            TextRange result = default;
            if (Operator(position, (Expression value) => value.TryGetDefinition(manager, position, out result)))
            {
                definition = result;
                return true;
            }
            definition = default;
            return false;
        }
        public bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => Operator(position, (Expression value) => value.FindReferences(manager, position, references));

        protected virtual void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) { }
        public void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            Operator((Statement value) => value.InternalCollectSemanticToken(manager, collector));
            Operator((Expression value) => value.CollectSemanticToken(manager, collector));
        }

        public bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            List<SignatureInfo>? resultInfos = default;
            int resultFunctionIndex = default;
            int resultParameterIndex = default;
            if (Operator(position, (Expression value) => value.TrySignatureHelp(manager, position, out resultInfos, out resultFunctionIndex, out resultParameterIndex)))
            {
                infos = resultInfos!;
                functionIndex = resultFunctionIndex;
                parameterIndex = resultParameterIndex;
                return true;
            }
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }

        protected virtual void InternalCollectInlayHint(Manager manager, List<InlayHintInfo> infos) { }
        public void CollectInlayHint(Manager manager, List<InlayHintInfo> infos)
        {
            Operator((Expression expression) => expression.CollectInlayHint(manager, infos));
        }
    }
}
