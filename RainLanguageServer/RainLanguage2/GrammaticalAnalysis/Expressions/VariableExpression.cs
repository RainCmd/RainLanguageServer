namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class VariableLocalExpression : Expression
    {
        public readonly Local local;
        public readonly TextRange identifier;
        public override bool Valid => true;
        public VariableLocalExpression(TextRange range, Local local, Type type, TextRange identifier, ExpressionAttribute attribute, Manager.KernelManager manager) : base(range, type)
        {
            this.local = local;
            this.identifier = identifier;
            this.attribute = attribute | local.type.GetAttribute(manager);
        }
        public VariableLocalExpression(TextRange range, Local local, TextRange identifier, ExpressionAttribute attribute, Manager.KernelManager manager) : this(range, local, local.type, identifier, attribute, manager) { }
        public override void Read(ExpressionParameter parameter) => local.read.Add(identifier);
        public override void Write(ExpressionParameter parameter) => local.write.Add(identifier);
    }
    internal class VariableDeclarationLocalExpression(TextRange range, Local local, TextRange identifier, TypeExpression typeExpression, ExpressionAttribute attribute, Manager.KernelManager manager) : VariableLocalExpression(range, local, identifier, attribute, manager)
    {
        public readonly TypeExpression typeExpression = typeExpression;
        public override void Read(ExpressionParameter parameter)
        {
            base.Read(parameter);
            typeExpression.Read(parameter);
        }
    }
    internal class VariableKeyworldLocalExpression(TextRange range, Local local, Type type, TextRange identifier, ExpressionAttribute attribute, Manager.KernelManager manager) : VariableLocalExpression(range, local, type, identifier, attribute, manager)
    {
    }
    internal class VariableGlobalExpression : Expression
    {
        public readonly TextRange? qualifier;// global
        public readonly QualifiedName name;
        public readonly AbstractVariable variable;
        public override bool Valid => true;
        public VariableGlobalExpression(TextRange range, TextRange? qualifier, QualifiedName name, AbstractVariable variable, Manager.KernelManager manager) : base(range, variable.type)
        {
            this.qualifier = qualifier;
            this.name = name;
            this.variable = variable;
            attribute = ExpressionAttribute.Value | variable.type.GetAttribute(manager);
            if (!variable.isReadonly) attribute |= ExpressionAttribute.Assignable;
            else if (variable.declaration.library == Manager.LIBRARY_SELF) attribute |= ExpressionAttribute.Constant;
        }
        public override void Read(ExpressionParameter parameter) => variable.references.Add(name.name);
        public override void Write(ExpressionParameter parameter) => variable.write.Add(name.name);
    }
    internal class VariableMemberExpression : Expression
    {
        public readonly TextRange? symbol;
        public readonly TextRange identifier;
        public readonly Expression? target;
        public readonly AbstractDeclaration member;
        public override bool Valid => true;
        public VariableMemberExpression(TextRange range, Type type, TextRange? symbol, TextRange identifier, Expression? target, AbstractDeclaration member, Manager.KernelManager manager) : base(range, type)
        {
            this.symbol = symbol;
            this.identifier = identifier;
            this.target = target;
            this.member = member;
            attribute = ExpressionAttribute.Value | type.GetAttribute(manager);
            if (member is not AbstractStruct) attribute |= ExpressionAttribute.Assignable;
            else if (target != null) attribute |= target.attribute & ExpressionAttribute.Assignable;
        }
        public VariableMemberExpression(TextRange range, Type type, TextRange identifier, AbstractDeclaration member, Manager.KernelManager manager) : this(range, type, null, identifier, null, member, manager) { }
        public override void Read(ExpressionParameter parameter)
        {
            target?.Read(parameter);
            member.references.Add(identifier);
        }
        public override void Write(ExpressionParameter parameter)
        {
            target?.Read(parameter);
            if (member is AbstractStruct.Variable structMember) structMember.write.Add(identifier);
            else if (member is AbstractClass.Variable classMember) classMember.write.Add(identifier);
        }
    }
}
