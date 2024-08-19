namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class TypeExpression : Expression
    {
        public readonly TextRange? qualifier;
        public readonly FileType file;
        public readonly Type type;
        public override bool Valid => true;
        public TypeExpression(TextRange range, TextRange? qualifier, FileType file, Type type) : base(range, Tuple.Empty)
        {
            this.qualifier = qualifier;
            this.file = file;
            this.type = type;
            attribute = ExpressionAttribute.Type;
        }
    }
    internal class TypeKeyworldExpression(TextRange range, TextRange? qualifier, FileType file, Type type) : TypeExpression(range, qualifier, file, type) { }
}
