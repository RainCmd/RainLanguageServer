namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class TypeExpression : Expression
    {
        public readonly TextRange? qualifier; // global
        public readonly FileType file;
        public readonly Type type;
        public override bool Valid => true;
        public TypeExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange? qualifier, FileType file, Type type) : base(range, Tuple.Empty, snapshoot)
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
        public override bool Operator(TextPosition position, ExpressionOperator action) => action(this);
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action) => action(this);
        public override void Operator(Action<Expression> action) => action(this);

        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info) => file.OnHover(manager, position, type, ManagerOperator.GetSpace(manager, position), out info);

        protected override bool InternalOnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => file.OnHighlight(manager, position, type, infos);

        protected override bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition) => file.TryGetDefinition(manager, position, type, out definition);

        protected override bool InternalFindReferences(Manager manager, TextPosition position, List<TextRange> references) => file.FindReferences(manager, position, type, references);

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.AddType(file, manager, type);
            if (qualifier != null) collector.Add(DetailTokenType.KeywordCtrl, qualifier.Value);
        }

        protected override void InternalRename(Manager manager, TextPosition position, HashSet<TextRange> ranges) => file.Rename(manager, position, type, ranges);
    }
    internal class TypeKeyworldExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange? qualifier, FileType file, Type type) : TypeExpression(range, snapshoot, qualifier, file, type)
    {
        protected override void InternalCollectInlayHint(Manager manager, List<InlayHintInfo> infos)
        {
            if (range == KeyWords.VAR)
                infos.Add(new InlayHintInfo($":{type.Info(manager, ManagerOperator.GetSpace(manager, range.end))}", range.end, InlayHintInfo.Kind.Type));
        }
    }
}
