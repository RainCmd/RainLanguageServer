namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class VectorMemberExpression : Expression
    {
        public readonly Expression target;
        public readonly TextRange member;
        public override bool Valid => true;
        public VectorMemberExpression(TextRange range, Type type, Expression target, TextRange member) : base(range, type)
        {
            this.target = target;
            this.member = member;
            attribute = ExpressionAttribute.Value | (target.attribute & ExpressionAttribute.Assignable);
        }
        public override void Read(ExpressionParameter parameter) => target.Read(parameter);
        public override void Write(ExpressionParameter parameter) => target.Write(parameter);
    }
    internal class VectorConstructorExpression : Expression
    {
        public readonly TypeExpression type;
        public readonly BracketExpression parameters;
        public override bool Valid => true;
        public VectorConstructorExpression(TextRange range, TypeExpression type, BracketExpression parameters) : base(range, type.type)
        {
            this.type = type;
            this.parameters = parameters;
            attribute = ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            type.Read(parameter);
            parameters.Read(parameter);
        }
    }
}
