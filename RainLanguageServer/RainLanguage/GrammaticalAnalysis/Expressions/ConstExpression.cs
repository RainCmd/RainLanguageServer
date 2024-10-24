namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal abstract class ConstExpression : Expression
    {
        public override bool Valid => true;
        public ConstExpression(TextRange range, Type type, LocalContextSnapshoot snapshoot) : base(range, type, snapshoot)
        {
            attribute = ExpressionAttribute.Constant;
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
        public override bool Calculability() => true;
        public override void Read(ExpressionParameter parameter) { }
        public override bool Operator(TextPosition position, ExpressionOperator action) => action(this);
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action) => action(this);
        public override void Operator(Action<Expression> action) => action(this);

        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (manager.TryGetDeclaration(tuple[0], out var declaration))
            {
                info = new HoverInfo(range, declaration.CodeInfo(manager, ManagerOperator.GetSpace(manager, position)), true);
                return true;
            }
            info = default;
            return false;
        }

        protected override bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (manager.TryGetDeclaration(tuple[0], out var declaration))
            {
                definition = declaration.name;
                return true;
            }
            definition = default;
            return false;
        }
    }
    internal class ConstBooleanExpression(TextRange range, LocalContextSnapshoot snapshoot, bool value, Manager.KernelManager manager) : ConstExpression(range, manager.BOOL, snapshoot)
    {
        public readonly bool value = value;
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.KeywordConst, range);
    }
    internal class ConstBooleanKeyworldExpression(TextRange range, LocalContextSnapshoot snapshoot, bool value, Manager.KernelManager manager) : ConstBooleanExpression(range, snapshoot, value, manager) { }
    internal class ConstByteExpression(TextRange range, LocalContextSnapshoot snapshoot, byte value, Manager.KernelManager manager) : ConstExpression(range, manager.BYTE, snapshoot)
    {
        public readonly byte value = value;
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
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.Numeric, range);
    }
    internal class ConstCharExpression(TextRange range, LocalContextSnapshoot snapshoot, char value, Manager.KernelManager manager) : ConstExpression(range, manager.CHAR, snapshoot)
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
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.Numeric, range);
    }
    internal class ConstIntegerExpression(TextRange range, LocalContextSnapshoot snapshoot, long value, Manager.KernelManager manager) : ConstExpression(range, manager.INT, snapshoot)
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
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.Numeric, range);
    }
    internal class ConstCharsExpression(TextRange range, LocalContextSnapshoot snapshoot, long value, Manager.KernelManager manager) : ConstIntegerExpression(range, snapshoot, value, manager)
    {
        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (manager.TryGetDeclaration(tuple[0], out var declaration))
            {
                info = new HoverInfo(range, declaration.CodeInfo(manager, ManagerOperator.GetSpace(manager, position)) + "\n= " + value.ToString(), true);
                return true;
            }
            info = default;
            return false;
        }
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) { }
    }
    internal class ConstRealExpression(TextRange range, LocalContextSnapshoot snapshoot, double value, Manager.KernelManager manager) : ConstExpression(range, manager.REAL, snapshoot)
    {
        public readonly double value = value;
        public override bool TryEvaluate(out double value)
        {
            value = this.value;
            return true;
        }
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.Numeric, range);
    }
    internal class ConstStringExpression : ConstExpression
    {
        public ConstStringExpression(TextRange range, LocalContextSnapshoot snapshoot, Manager.KernelManager manager) : base(range, manager.STRING, snapshoot)
        {
            attribute |= ExpressionAttribute.Array;
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.String, range);
    }
    internal class ConstTypeExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange symbolLeft, TextRange symbolRight, FileType file, Type value, Manager.KernelManager manager) : ConstExpression(range, manager.TYPE, snapshoot)
    {
        public readonly TextRange symbolLeft = symbolLeft, symbolRight = symbolRight;
        public readonly FileType file = file;
        public readonly Type value = value;
        public override void Read(ExpressionParameter parameter)
        {
            if (parameter.manager.TryGetDeclaration(value, out var declaration)) declaration.references.Add(file.name.name);
        }
        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info) => InfoUtility.OnHover(file, manager, position, value, ManagerOperator.GetSpace(manager, position), out info);
        protected override bool InternalOnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => InfoUtility.OnHighlight(file, manager, position, value, infos);
        protected override bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition) => file.TryGetDefinition(manager, position, value, out definition);
        protected override bool InternalFindReferences(Manager manager, TextPosition position, List<TextRange> references) => file.FindReferences(manager, position, value, references);
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.Operator, symbolLeft);
            collector.Add(DetailTokenType.Operator, symbolRight);
            collector.AddType(file, manager, value);
        }
        protected override void InternalRename(Manager manager, TextPosition position, HashSet<TextRange> ranges) => file.Rename(manager, position, value, ranges);
    }
    internal class ConstNullExpression(TextRange range, LocalContextSnapshoot snapshoot) : ConstExpression(range, NULL, snapshoot)
    {
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.KeywordConst, range);
    }
    internal class ConstHandleNullExpression(TextRange range, Type type, LocalContextSnapshoot snapshoot) : ConstExpression(range, type, snapshoot)
    {
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.KeywordConst, range);
    }
    internal class ConstEntityNullExpression(TextRange range, LocalContextSnapshoot snapshoot, Manager.KernelManager manager) : ConstExpression(range, manager.ENTITY, snapshoot)
    {
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.KeywordConst, range);
    }
}
