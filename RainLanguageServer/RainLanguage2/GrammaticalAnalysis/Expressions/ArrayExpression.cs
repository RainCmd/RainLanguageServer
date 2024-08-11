namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class ArrayCreateExpression : Expression
    {
        public readonly TypeExpression? type;
        public readonly BracketExpression length;
        public override bool Valid => true;
        public ArrayCreateExpression(TextRange range, Type arrayType, TypeExpression? type, BracketExpression length) : base(range, arrayType)
        {
            this.type = type;
            this.length = length;
            attribute = ExpressionAttribute.Value | ExpressionAttribute.Array;
        }
    }
    internal class ArrayInitExpression : Expression
    {
        public readonly Expression elements;
        public override bool Valid => true;
        public ArrayInitExpression(TextRange range, Expression elements, Type type) : base(range, type)
        {
            this.elements = elements;
            attribute = ExpressionAttribute.Value | ExpressionAttribute.Array;
        }
    }
    internal class ArrayEvaluationExpression : Expression
    {
        public readonly Expression array;
        public readonly BracketExpression index;
        public override bool Valid => true;
        public ArrayEvaluationExpression(TextRange range, Expression array, BracketExpression index, Manager.KernelManager manager) : base(range, new Type(array.tuple[0], array.tuple[0].dimension - 1))
        {
            this.array = array;
            this.index = index;
            attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
        }
    }
    internal class StringEvaluationExpression : Expression
    {
        public readonly Expression source;
        public readonly BracketExpression index;
        public override bool Valid => true;
        public StringEvaluationExpression(TextRange range, Expression source, BracketExpression index, Manager.KernelManager manager) : base(range, manager.CHAR)
        {
            this.source = source;
            this.index = index;
            attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
        }
    }
    internal class ArraySubExpression : Expression
    {
        public readonly Expression source;
        public readonly BracketExpression indies;
        public override bool Valid => true;
        public ArraySubExpression(TextRange range, Expression source, BracketExpression indies, Manager.KernelManager manager) : base(range, source.tuple)
        {
            this.source = source;
            this.indies = indies;
            attribute = ExpressionAttribute.Value | ExpressionAttribute.Array;
        }
    }
}
