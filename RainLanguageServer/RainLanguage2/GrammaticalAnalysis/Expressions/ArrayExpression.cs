namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
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
}
