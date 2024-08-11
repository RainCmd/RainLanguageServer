namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class VariableLocalExpression : Expression
    {
        public readonly Local local;
        public readonly TextRange? declaration;
        public readonly TextRange identifier;
        public override bool Valid => true;

        public VariableLocalExpression(TextRange range, Local local, TextRange? declaration, TextRange identifier, ExpressionAttribute attribute, Manager.KernelManager manager) : base(range, local.type)
        {
            this.local = local;
            this.declaration = declaration;
            this.identifier = identifier;
            this.attribute = attribute | local.type.GetAttribute(manager);
        }
    }
}
