﻿namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
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
            foreach(var item in function.implements)
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
