using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class ConstExpression : Expression
    {
        public override bool Valid => true;
        public ConstExpression(TextRange range, Type type) : base(range, type)
        {
            attribute = ExpressionAttribute.Constant;
        }
        public virtual bool TryEvaluate(out bool value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluate(out byte value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluate(out char value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluate(out long value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluate(out double value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluate([MaybeNullWhen(false)]out string value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluate(out Type value)
        {
            value = default;
            return false;
        }
    }
    internal class ConstBooleanExpression(TextRange range, bool value, Manager.KernelManager manager) : ConstExpression(range, manager.BOOL)
    {
        public readonly bool value = value;
        public override bool TryEvaluate(out bool value)
        {
            value = this.value;
            return true;
        }
    }
    internal class ConstByteExpression(TextRange range, byte value, Manager.KernelManager manager) : ConstExpression(range, manager.BYTE)
    {
        public readonly byte value = value;
        public override bool TryEvaluate(out byte value)
        {
            value = this.value;
            return true;
        }
        public override bool TryEvaluate(out char value)
        {
            value = (char)this.value;
            return true;
        }
        public override bool TryEvaluate(out long value)
        {
            value = this.value;
            return true;
        }
        public override bool TryEvaluate(out double value)
        {
            value = this.value;
            return true;
        }
    }
    internal class ConstCharExpression(TextRange range, char value, Manager.KernelManager manager) : ConstExpression(range, manager.CHAR)
    {
        public readonly char value = value;
        public override bool TryEvaluate(out char value)
        {
            value = this.value;
            return true;
        }
        public override bool TryEvaluate(out long value)
        {
            value = this.value;
            return true;
        }
        public override bool TryEvaluate(out double value)
        {
            value = this.value;
            return true;
        }
    }
    internal class ConstIntegerExpression(TextRange range, long value, Manager.KernelManager manager) : ConstExpression(range, manager.INT)
    {
        public readonly long value = value;
        public override bool TryEvaluate(out long value)
        {
            value = this.value;
            return true;
        }
        public override bool TryEvaluate(out double value)
        {
            value = this.value;
            return true;
        }
    }
    internal class ConstRealExpression(TextRange range, double value, Manager.KernelManager manager) : ConstExpression(range, manager.REAL)
    {
        public readonly double value = value;
        public override bool TryEvaluate(out double value)
        {
            value = this.value;
            return true;
        }
    }
    internal class ConstStringExpression(TextRange range, string value, Manager.KernelManager manager) : ConstExpression(range, manager.STRING)
    {
        public readonly string value = value;
        public override bool TryEvaluate(out string value)
        {
            value = this.value;
            return true;
        }
    }
    internal class ConstTypeExpression(TextRange range, Type value, Manager.KernelManager manager) : ConstExpression(range, manager.TYPE)
    {
        public readonly Type value = value;
        public override bool TryEvaluate(out Type value)
        {
            value = this.value;
            return true;
        }
    }
    internal class ConstNullExpression(TextRange range) : ConstExpression(range, NULL) { }
    internal class ConstHandleNullExpression(TextRange range, Type type) : ConstExpression(range, type) { }
    internal class ConstEntityNullExpression(TextRange range, Manager.KernelManager manager) : ConstExpression(range, manager.ENTITY) { }
}
