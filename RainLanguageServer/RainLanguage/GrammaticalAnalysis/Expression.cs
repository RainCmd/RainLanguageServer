using RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions;
using System.Diagnostics.CodeAnalysis;
using System.Security.AccessControl;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis
{
    internal enum ExpressionAttribute
    {
        Invalid,
        None = 0x0001,              //无
        Operator = 0x0002,          //运算符
        Value = 0x004,              //值
        Constant = 0x000C,          //常量
        Assignable = 0x0010,        //可赋值
        Callable = 0x0020,          //可调用
        Array = 0x0040,             //数组
        Tuple = 0x0080,             //元组
        Task = 0x0100,              //任务
        Type = 0x0200,              //类型
        Method = 0x0400,            //方法
    }
    internal static class ExpressionAttributeExtend
    {
        public static bool ContainAll(this ExpressionAttribute attribute, ExpressionAttribute value)
        {
            return (attribute & value) == value;
        }
        public static bool ContainAny(this ExpressionAttribute attribute, ExpressionAttribute value)
        {
            return (attribute & value) != 0;
        }
        public static ExpressionAttribute GetAttribute(this Type type, Manager.KernelManager manager)
        {
            if (type.dimension > 0 || type == manager.STRING || type == manager.ARRAY) return ExpressionAttribute.Array;
            else if (type.code == TypeCode.Delegate) return ExpressionAttribute.Callable;
            else if (type.code == TypeCode.Task) return ExpressionAttribute.Task;
            return ExpressionAttribute.Invalid;
        }
    }
    internal readonly struct ExpressionParameter(Manager manager, MessageCollector collector)
    {
        public readonly Manager manager = manager;
        public readonly MessageCollector collector = collector;
    }
    internal abstract class Expression(TextRange range, Tuple tuple, LocalContextSnapshoot snapshoot)
    {
        public delegate bool ExpressionOperator(Expression expression);
        public readonly TextRange range = range;
        public readonly Tuple tuple = tuple;
        public readonly LocalContextSnapshoot snapshoot = snapshoot;
        public ExpressionAttribute attribute;
        public abstract bool Valid { get; }
        public Expression ToInvalid()
        {
            if (Valid) return new InvalidExpression(this, tuple, snapshoot);
            return this;
        }
        public static readonly Type BLURRY = new(-3, TypeCode.Invalid, 0, 0);
        public static readonly Type NULL = new(-3, TypeCode.Invalid, 1, 0);
        public static readonly Tuple TUPLE_BLURRY = new([BLURRY]);
        public virtual bool TryEvaluateIndices(List<long> indices) => false;
        public virtual bool Calculability() => false;
        public abstract void Read(ExpressionParameter parameter);
        public virtual void Write(ExpressionParameter parameter) => parameter.collector.Add(range, ErrorLevel.Error, "表达式不可赋值");
        public abstract bool Operator(TextPosition position, ExpressionOperator action);
        public abstract bool BreadthFirstOperator(TextPosition position, ExpressionOperator action);
        public abstract void Operator(Action<Expression> action);

        protected virtual bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            info = default;
            return false;
        }
        public bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            HoverInfo result = default;
            if (Operator(position, value => value.InternalOnHover(manager, position, out result)))
            {
                info = result;
                return true;
            }
            info = default;
            return false;
        }

        protected virtual bool InternalOnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) { return false; }
        public bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => Operator(position, value => value.InternalOnHighlight(manager, position, infos));

        protected virtual bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            definition = default;
            return false;
        }
        public bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            TextRange result = default;
            if (Operator(position, value => value.InternalTryGetDefinition(manager, position, out result)))
            {
                definition = result;
                return true;
            }
            definition = default;
            return false;
        }

        protected virtual bool InternalFindReferences(Manager manager, TextPosition position, List<TextRange> references) { return false; }
        public bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => Operator(position, value => value.InternalFindReferences(manager, position, references));

        protected virtual void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) { }
        public void CollectSemanticToken(Manager manager, SemanticTokenCollector collector) => Operator(value => value.InternalCollectSemanticToken(manager, collector));

        public virtual int GetTupleIndex(TextPosition position) => 0;
        protected virtual bool InternalTrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }
        public bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            List<SignatureInfo>? refInfos = default;
            int refFunctionIndex = 0;
            int refParameterIndex = 0;
            var result = BreadthFirstOperator(position, expression => expression.InternalTrySignatureHelp(manager, position, out refInfos, out refFunctionIndex, out refParameterIndex));
            infos = refInfos;
            functionIndex = refFunctionIndex;
            parameterIndex = refParameterIndex;
            return result;
        }

        protected virtual void InternalRename(Manager manager, TextPosition position, HashSet<TextRange> ranges) { }
        public void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges) => Operator(position, value => { value.InternalRename(manager, position, ranges); return default; });

        protected virtual bool InternalCompletion(Manager manager, TextPosition position, List<CompletionInfo> infos)
        {
            if (ManagerOperator.TryGetContext(manager, position, out var context))
            {
                snapshoot.Completion(manager, context.space, infos);
                InfoUtility.CollectValueKeyword(context, infos);
                InfoUtility.CollectDeclarations(manager, infos, context, CompletionFilter.All);
                InfoUtility.CollectSpaces(manager, infos, context.space, context.relies);
                InfoUtility.CollectCtrlKeyword(infos);
                InfoUtility.CollectRelationKeyword(infos);
            }
            return default;
        }
        public void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos) => Operator(position, value => value.InternalCompletion(manager, position, infos));

        protected virtual void InternalCollectInlayHint(Manager manager, List<InlayHintInfo> infos) { }
        public void CollectInlayHint(Manager manager, List<InlayHintInfo> infos) => Operator(expression => expression.InternalCollectInlayHint(manager, infos));
    }
}
