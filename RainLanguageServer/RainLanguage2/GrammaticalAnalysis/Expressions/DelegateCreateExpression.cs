namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal abstract class DelegateCreateExpression : Expression
    {
        public readonly AbstractCallable callable;
        public override bool Valid => true;
        public DelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager) : base(range, type)
        {
            this.callable = callable;
            attribute = ExpressionAttribute.Value | type.GetAttribute(manager);
        }
    }
    internal class FunctionDelegateCreateExpression(TextRange range, TextRange? qualifier, QualifiedName name, Type type, AbstractCallable callable, Manager.KernelManager manager) : DelegateCreateExpression(range, type, callable, manager)
    {
        public readonly TextRange? qualifier = qualifier;
        public readonly QualifiedName name = name;
        public override void Read(ExpressionParameter parameter) => callable.references.Add(name.name);

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (InfoUtility.OnHover(name.qualify, position, out info)) return true;
            if (name.name.Contain(position))
            {
                info = new HoverInfo(name.name, callable.Info(manager, null, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (InfoUtility.OnHighlight(name.qualify, position, callable.space, infos)) return true;
            if (name.name.Contain(position))
            {
                InfoUtility.Highlight(callable, infos);
                return true;
            }
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (name.name.Contain(position))
            {
                definition = callable.name;
                return true;
            }
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if(InfoUtility.FindReferences(name.qualify,position,callable.space, references)) return true;
            if (name.name.Contain(position))
            {
                references.AddRange(callable.references);
                return true;
            }
            return false;
        }

    }
    internal class MemberFunctionDelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager, Expression? target, TextRange? symbol, TextRange member) : DelegateCreateExpression(range, type, callable, manager)
    {
        public readonly Expression? target = target;
        public readonly TextRange? symbol = symbol;
        public readonly TextRange member = member;
        public override void Read(ExpressionParameter parameter)
        {
            target?.Read(parameter);
            callable.references.Add(member);
        }
    }
    internal class VirtualFunctionDelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager, Expression? target, TextRange? symbol, TextRange member) : DelegateCreateExpression(range, type, callable, manager)
    {
        public readonly Expression? target = target;
        public readonly TextRange? symbol = symbol;
        public readonly TextRange member = member;
        public override void Read(ExpressionParameter parameter)
        {
            target?.Read(parameter);
            if (callable is AbstractClass.Function function) Reference(function);
            else callable.references.Add(member);
        }
        private void Reference(AbstractClass.Function function)
        {
            function.references.Add(member);
            foreach (var item in function.implements)
                Reference(item);
        }
    }
    internal class LambdaDelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager, List<Local> parmeters, TextRange symbol, Expression body) : DelegateCreateExpression(range, type, callable, manager)
    {
        public readonly List<Local> parmeters = parmeters;
        public readonly TextRange symbol = symbol;
        public readonly Expression body = body;
        public override void Read(ExpressionParameter parameter) => body.Read(parameter);
    }
}
