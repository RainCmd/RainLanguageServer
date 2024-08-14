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
    internal class VariableGlobalExpression : Expression
    {
        public readonly AbstractVariable variable;
        public override bool Valid => true;
        public VariableGlobalExpression(TextRange range, AbstractVariable variable, Manager.KernelManager manager) : base(range, variable.type)
        {
            this.variable = variable;
            attribute = ExpressionAttribute.Value | variable.type.GetAttribute(manager);
            if (!variable.isReadonly) attribute |= ExpressionAttribute.Assignable;
            else if (variable.declaration.library == Manager.LIBRARY_SELF) attribute |= ExpressionAttribute.Constant;
        }
    }
    internal class VariableMemberExpression : Expression
    {
        public readonly TextRange symbol;
        public readonly TextRange identifier;
        public readonly Expression target;
        public readonly AbstractDeclaration member;
        public override bool Valid => true;
        public VariableMemberExpression(TextRange range, Type type, TextRange symbol, TextRange identifier, Expression target, AbstractDeclaration member, Manager.KernelManager manager) : base(range, type)
        {
            this.symbol = symbol;
            this.identifier = identifier;
            this.target = target;
            this.member = member;
            attribute = ExpressionAttribute.Value | type.GetAttribute(manager);
            if (member is not AbstractStruct) attribute |= ExpressionAttribute.Assignable;
            else attribute |= target.attribute & ExpressionAttribute.Assignable;
        }
    }
}
