namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class TypeExpression : Expression
    {
        public readonly FileType file;
        public readonly Type type;
        public override bool Valid => true;
        public TypeExpression(TextRange range, FileType file, Type type) : base(range, Tuple.Empty)
        {
            this.file = file;
            this.type = type;
            attribute = ExpressionAttribute.Type;
        }
    }
}
