namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class VariableLocalExpression : Expression
    {
        public readonly Local local;
        public readonly TextRange identifier;
        public override bool Valid => true;
        public VariableLocalExpression(TextRange range, Local local, Type type, LocalContextSnapshoot snapshoot, TextRange identifier, ExpressionAttribute attribute, Manager.KernelManager manager) : base(range, type, snapshoot)
        {
            this.local = local;
            this.identifier = identifier;
            this.attribute = attribute | local.type.GetAttribute(manager);
        }
        public VariableLocalExpression(TextRange range, Local local, LocalContextSnapshoot snapshoot, TextRange identifier, ExpressionAttribute attribute, Manager.KernelManager manager) : this(range, local, local.type, snapshoot, identifier, attribute, manager) { }
        public override void Read(ExpressionParameter parameter) => local.read.Add(identifier);
        public override void Write(ExpressionParameter parameter) => local.write.Add(identifier);
        public override bool Operator(TextPosition position, ExpressionOperator action) => action(this);
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action) => action(this);
        public override void Operator(Action<Expression> action) => action(this);

        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (identifier.Contain(position))
            {
                info = local.Hover(manager, position);
                return true;
            }
            info = default;
            return false;
        }

        protected override bool InternalOnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (identifier.Contain(position))
            {
                local.OnHighlight(infos);
                return true;
            }
            return false;
        }

        protected override bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (identifier.Contain(position))
            {
                definition = local.range;
                return true;
            }
            definition = default;
            return false;
        }

        protected override bool InternalFindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (identifier.Contain(position))
            {
                local.FindReferences(references);
                return true;
            }
            return false;
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            if (local.read.Count == 0) collector.Add(DetailTokenType.DeprecatedLocal, identifier);
            else if (local.parameter) collector.Add(DetailTokenType.Parameter, identifier);
            else collector.Add(DetailTokenType.Local, identifier);
        }

        protected override void InternalRename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (identifier.Contain(position)) local.Rename(ranges);
        }
    }
    internal class VariableDeclarationLocalExpression(TextRange range, Local local, LocalContextSnapshoot snapshoot, TextRange identifier, TypeExpression typeExpression, ExpressionAttribute attribute, Manager.KernelManager manager) : VariableLocalExpression(range, local, snapshoot, identifier, attribute, manager)
    {
        public readonly TypeExpression typeExpression = typeExpression;

        public override void Read(ExpressionParameter parameter)
        {
            base.Read(parameter);
            typeExpression.Read(parameter);
        }
        public override void Write(ExpressionParameter parameter)
        {
            base.Write(parameter);
            typeExpression.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (typeExpression.range.Contain(position)) return typeExpression.Operator(position, action);
            return base.Operator(position, action);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (base.BreadthFirstOperator(position, action)) return true;
            if (typeExpression.range.Contain(position)) return typeExpression.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            typeExpression.Operator(action);
            base.Operator(action);
        }
    }
    internal class VariableKeyworldLocalExpression(TextRange range, Local local, Type type, LocalContextSnapshoot snapshoot, TextRange identifier, ExpressionAttribute attribute, Manager.KernelManager manager) : VariableLocalExpression(range, local, type, snapshoot, identifier, attribute, manager)
    {
        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (identifier.Contain(position) && manager.TryGetDeclaration(local.type, out var declaration))
            {
                info = new HoverInfo(identifier, declaration.CodeInfo(manager, ManagerOperator.GetSpace(manager, position)), true);
                return true;
            }
            info = default;
            return false;
        }
        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.KeywordVariable, identifier);
        }
    }
    internal class VariableGlobalExpression : Expression
    {
        public readonly TextRange? qualifier;// global
        public readonly QualifiedName name;
        public readonly AbstractVariable variable;
        public override bool Valid => true;
        public VariableGlobalExpression(TextRange range, LocalContextSnapshoot snapshoot, TextRange? qualifier, QualifiedName name, AbstractVariable variable, Manager.KernelManager manager) : base(range, variable.type, snapshoot)
        {
            this.qualifier = qualifier;
            this.name = name;
            this.variable = variable;
            attribute = ExpressionAttribute.Value | variable.type.GetAttribute(manager);
            if (!variable.isReadonly) attribute |= ExpressionAttribute.Assignable;
            else if (variable.declaration.library == Manager.LIBRARY_SELF) attribute |= ExpressionAttribute.Constant;
        }
        public override bool Calculability() => variable.isReadonly && variable.calculated && variable.declaration.library == Manager.LIBRARY_SELF;
        public override void Read(ExpressionParameter parameter) => variable.references.Add(name.name);
        public override void Write(ExpressionParameter parameter) => variable.write.Add(name.name);
        public override bool Operator(TextPosition position, ExpressionOperator action) => action(this);
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action) => action(this);
        public override void Operator(Action<Expression> action) => action(this);

        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (InfoUtility.OnHover(name.qualify, position, out info)) return true;
            if (name.name.Contain(position))
            {
                info = new HoverInfo(name.name, variable.CodeInfo(manager, ManagerOperator.GetSpace(manager, position)), true);
                return true;
            }
            info = default;
            return false;
        }

        protected override bool InternalOnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (InfoUtility.OnHighlight(name.qualify, position, variable.space, infos)) return true;
            if (name.name.Contain(position))
            {
                InfoUtility.Highlight(variable, infos);
                return true;
            }
            return false;
        }

        protected override bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (name.name.Contain(position))
            {
                definition = variable.name;
                return true;
            }
            definition = default;
            return false;
        }

        protected override bool InternalFindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (InfoUtility.FindReferences(name.qualify, position, variable.space, references)) return true;
            if (name.name.Contain(position))
            {
                references.AddRange(variable.references);
                return true;
            }
            return false;
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            if (qualifier != null) collector.Add(DetailTokenType.KeywordCtrl, qualifier.Value);
            collector.AddNamespace(name);
            if (variable.isReadonly)
                collector.Add(DetailTokenType.Constant, name.name);
            else
                collector.Add(DetailTokenType.GlobalVariable, name.name);
        }

        protected override void InternalRename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (name.name.Contain(position)) InfoUtility.Rename(variable, ranges);
            else InfoUtility.Rename(name.qualify, position, ManagerOperator.GetSpace(manager, position), ranges);
        }
    }
    internal class VariableMemberExpression : Expression
    {
        public readonly TextRange? symbol;
        public readonly TextRange identifier;
        public readonly Expression? target;
        public readonly AbstractDeclaration member;
        public override bool Valid => true;
        public VariableMemberExpression(TextRange range, Type type, LocalContextSnapshoot snapshoot, TextRange? symbol, TextRange identifier, Expression? target, AbstractDeclaration member, Manager.KernelManager manager) : base(range, type, snapshoot)
        {
            this.symbol = symbol;
            this.identifier = identifier;
            this.target = target;
            this.member = member;
            attribute = ExpressionAttribute.Value | type.GetAttribute(manager);
            if (member is not AbstractStruct) attribute |= ExpressionAttribute.Assignable;
            else if (target != null) attribute |= target.attribute & ExpressionAttribute.Assignable;
        }
        public VariableMemberExpression(TextRange range, Type type, LocalContextSnapshoot snapshoot, TextRange identifier, AbstractDeclaration member, Manager.KernelManager manager) : this(range, type, snapshoot, null, identifier, null, member, manager) { }
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
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (target != null && target.range.Contain(position)) return target.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (target != null && target.range.Contain(position)) return target.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            target?.Operator(action);
            action(this);
        }

        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (identifier.Contain(position))
            {
                info = new HoverInfo(identifier, member.CodeInfo(manager, ManagerOperator.GetSpace(manager, position)), true);
                return true;
            }
            info = default;
            return false;
        }

        protected override bool InternalOnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (identifier.Contain(position))
            {
                InfoUtility.Highlight(member, infos);
                return true;
            }
            return false;
        }

        protected override bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (identifier.Contain(position))
            {
                definition = member.name;
                return true;
            }
            definition = default;
            return false;
        }

        protected override bool InternalFindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (identifier.Contain(position))
            {
                references.AddRange(member.references);
                if(member is AbstractStruct.Variable structMember) references.AddRange(structMember.write);
                else if(member is AbstractClass.Variable classMember) references.AddRange(classMember.write);
                return true;
            }
            return false;
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            if (symbol != null) collector.Add(DetailTokenType.Operator, symbol.Value);
            collector.Add(DetailTokenType.MemberField, identifier);
        }

        protected override void InternalRename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (identifier.Contain(position)) InfoUtility.Rename(member, ranges);
        }
    }
}
