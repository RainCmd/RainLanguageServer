
namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
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
        public override void Read(ExpressionParameter parameter)
        {
            if (parameter.manager.TryGetDeclaration(type, out var declaration))
                declaration.references.Add(file.name.name);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info) => file.OnHover(manager, position, type, ManagerOperator.GetSpace(manager, position), out info);

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => file.OnHighlight(manager, position, type, infos);

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition) => file.TryGetDefinition(manager, position, type, out definition);

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => file.FindReferences(manager, position, type, references);
    }
    internal class TypeKeyworldExpression(TextRange range, TextRange? qualifier, FileType file, Type type) : TypeExpression(range, qualifier, file, type) { }
}
