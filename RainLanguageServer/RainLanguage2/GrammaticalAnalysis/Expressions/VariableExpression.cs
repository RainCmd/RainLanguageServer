
namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
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

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (identifier.Contain(position))
            {
                info = local.Hover(manager, position);
                return true;
            }
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (identifier.Contain(position))
            {
                local.OnHighlight(infos);
                return true;
            }
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (identifier.Contain(position))
            {
                definition = local.range;
                return true;
            }
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (identifier.Contain(position))
            {
                local.FindReferences(references);
                return true;
            }
            return false;
        }
    }
    internal class VariableDeclarationLocalExpression(TextRange range, Local local, TextRange identifier, TypeExpression typeExpression, ExpressionAttribute attribute, Manager.KernelManager manager) : VariableLocalExpression(range, local, identifier, attribute, manager)
    {
        public readonly TypeExpression typeExpression = typeExpression;
        public override void Read(ExpressionParameter parameter)
        {
            base.Read(parameter);
            typeExpression.Read(parameter);
        }
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (typeExpression.range.Contain(position)) return typeExpression.OnHover(manager, position, out info);
            return base.OnHover(manager, position, out info);
        }
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (typeExpression.range.Contain(position)) return typeExpression.OnHighlight(manager, position, infos);
            return base.OnHighlight(manager, position, infos);
        }
        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (typeExpression.range.Contain(position)) return typeExpression.TryGetDefinition(manager, position, out definition);
            return base.TryGetDefinition(manager, position, out definition);
        }
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (typeExpression.range.Contain(position)) return typeExpression.FindReferences(manager, position, references);
            return base.FindReferences(manager, position, references);
        }
    }
    internal class VariableKeyworldLocalExpression(TextRange range, Local local, Type type, TextRange identifier, ExpressionAttribute attribute, Manager.KernelManager manager) : VariableLocalExpression(range, local, type, identifier, attribute, manager)
    {
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (identifier.Contain(position) && manager.TryGetDeclaration(local.type, out var declaration))
            {
                info = new HoverInfo(identifier, declaration.Info(manager, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            info = default;
            return false;
        }
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

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (InfoUtility.OnHover(name.qualify, position, out info)) return true;
            if (name.name.Contain(position))
            {
                info = new HoverInfo(name.name, variable.Info(manager, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (InfoUtility.OnHighlight(name.qualify, position, variable.space, infos)) return true;
            if (name.name.Contain(position))
            {
                InfoUtility.Highlight(variable, infos);
                return true;
            }
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (name.name.Contain(position))
            {
                definition = variable.name;
                return true;
            }
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (InfoUtility.FindReferences(name.qualify, position, variable.space, references)) return true;
            if (name.name.Contain(position))
            {
                references.AddRange(variable.references);
                return true;
            }
            return false;
        }
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

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (target != null && target.range.Contain(position)) return target.OnHover(manager, position, out info);
            if (identifier.Contain(position))
            {
                info = new HoverInfo(identifier, member.Info(manager, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (target != null && target.range.Contain(position)) return target.OnHighlight(manager, position, infos);
            if (identifier.Contain(position))
            {
                InfoUtility.Highlight(member, infos);
                return true;
            }
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (target != null && target.range.Contain(position)) return target.TryGetDefinition(manager, position, out definition);
            if (identifier.Contain(position))
            {
                definition = member.name;
                return true;
            }
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (target != null && target.range.Contain(position)) return target.FindReferences(manager, position, references);
            if (identifier.Contain(position))
            {
                references.AddRange(member.references);
                return true;
            }
            return false;
        }
    }
}
