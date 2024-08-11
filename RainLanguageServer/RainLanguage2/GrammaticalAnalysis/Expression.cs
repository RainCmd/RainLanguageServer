using RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions;

namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis
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
    internal abstract class Expression(TextRange range, Tuple tuple)
    {
        public readonly TextRange range = range;
        public readonly Tuple tuple = tuple;
        public ExpressionAttribute attribute;
        public abstract bool Valid { get; }
        public Expression ToInvalid()
        {
            if (Valid) return new InvalidExpression(this, tuple);
            return this;
        }
        public static readonly Type BLURRY = new(-3, TypeCode.Invalid, 0, 0);
        public static readonly Type NULL = new(-3, TypeCode.Invalid, 1, 0);
        public static readonly Tuple TUPLE_BLURRY = new([BLURRY]);
        public virtual bool TryEvaluateIndices(List<long> indices) => false;
    }
}
