namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class EnumElementExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly TextRange identifier;
        public readonly AbstractEnum abstractEnum;
        public readonly AbstractEnum.Element element;
        public readonly TypeExpression type;
        public override bool Valid => true;

        public EnumElementExpression(TextRange range, TextRange symbol, TextRange identifier, AbstractEnum abstractEnum, AbstractEnum.Element element, TypeExpression type) : base(range, type.type)
        {
            this.symbol = symbol;
            this.identifier = identifier;
            this.abstractEnum = abstractEnum;
            this.element = element;
            this.type = type;
            attribute = ExpressionAttribute.Constant;
        }
    }
}
