namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class DelegateCreateExpression : Expression
    {
        public readonly AbstractCallable callable;
        public override bool Valid => true;
        public DelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager) : base(range, type)
        {
            this.callable = callable;
            attribute = ExpressionAttribute.Value | type.GetAttribute(manager);
        }
    }
    internal class FunctionDelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager) : DelegateCreateExpression(range, type, callable, manager) { }
    internal class MemberFunctionDelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager, Expression target, TextRange symbol, TextRange member) : DelegateCreateExpression(range, type, callable, manager)
    {
        public readonly Expression target = target;
        public readonly TextRange symbol = symbol;
        public readonly TextRange member = member;
    }
    internal class VirtualFunctionDelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager, Expression target, TextRange symbol, TextRange member) : DelegateCreateExpression(range, type, callable, manager)
    {
        public readonly Expression target = target;
        public readonly TextRange symbol = symbol;
        public readonly TextRange member = member;
    }
    internal class LambdaDelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager, List<Local> parmeters, TextRange symbol, Expression body) : DelegateCreateExpression(range, type, callable, manager)
    {
        public readonly List<Local> parmeters = parmeters;
        public readonly TextRange symbol = symbol;
        public readonly Expression body = body;
    }
}
