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
        public virtual bool TryEvaluate([MaybeNullWhen(false)] out string value)
        {
            value = default;
            return false;
        }
        public virtual bool TryEvaluate(out Type value)
        {
            value = default;
            return false;
        }
        public override void Read(ExpressionParameter parameter) { }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (manager.TryGetDeclaration(tuple[0], out var declaration))
            {
                info = new HoverInfo(range, declaration.Info(manager, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => false;

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (manager.TryGetDeclaration(tuple[0], out var declaration))
            {
                definition = declaration.name;
                return true;
            }
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => false;
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
    internal class ConstBooleanKeyworldExpression(TextRange range, bool value, Manager.KernelManager manager) : ConstBooleanExpression(range, value, manager) { }
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
        public override bool TryEvaluateIndices(List<long> indices)
        {
            indices.Add(value);
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
        public override bool TryEvaluateIndices(List<long> indices)
        {
            indices.Add(value);
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
        public override bool TryEvaluateIndices(List<long> indices)
        {
            indices.Add(value);
            return true;
        }
    }
    internal class ConstCharsExpression(TextRange range, long value, Manager.KernelManager manager) : ConstIntegerExpression(range, value, manager) { }
    internal class ConstRealExpression(TextRange range, double value, Manager.KernelManager manager) : ConstExpression(range, manager.REAL)
    {
        public readonly double value = value;
        public override bool TryEvaluate(out double value)
        {
            value = this.value;
            return true;
        }
    }
    internal class ConstStringExpression : ConstExpression
    {
        public readonly string value;

        public ConstStringExpression(TextRange range, string value, Manager.KernelManager manager) : base(range, manager.STRING)
        {
            this.value = value;
            attribute |= ExpressionAttribute.Array;
        }

        public override bool TryEvaluate(out string value)
        {
            value = this.value;
            return true;
        }
    }
    internal class ConstTypeExpression(TextRange range, TextRange symbolLeft, TextRange symbolRight, FileType file, Type value, Manager.KernelManager manager) : ConstExpression(range, manager.TYPE)
    {
        public readonly TextRange symbolLeft = symbolLeft, symbolRight = symbolRight;
        public readonly FileType file = file;
        public readonly Type value = value;
        public override bool TryEvaluate(out Type value)
        {
            value = this.value;
            return true;
        }
        public override void Read(ExpressionParameter parameter)
        {
            if (parameter.manager.TryGetDeclaration(value, out var declaration)) declaration.references.Add(file.name.name);
        }
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info) => InfoUtility.OnHover(file, manager, position, value, ManagerOperator.GetSpace(manager, position), out info);
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => InfoUtility.OnHighlight(file, manager, position, value, infos);
        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition) => file.TryGetDefinition(manager, position, value, out definition);
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => file.FindReferences(manager, position, value, references);
    }
    internal class ConstNullExpression(TextRange range) : ConstExpression(range, NULL) { }
    internal class ConstHandleNullExpression(TextRange range, Type type) : ConstExpression(range, type) { }
    internal class ConstEntityNullExpression(TextRange range, Manager.KernelManager manager) : ConstExpression(range, manager.ENTITY) { }
}
