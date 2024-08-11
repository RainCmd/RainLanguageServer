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
    internal class MemberFunctionDelegateCreateExpression : DelegateCreateExpression
    {
        public readonly Expression target;
        public readonly TextRange symbol;
        public readonly TextRange member;
        public MemberFunctionDelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager, Expression target, TextRange symbol, TextRange member) : base(range, type, callable, manager)
        {
            this.target = target;
            this.symbol = symbol;
            this.member = member;
        }
    }
    internal class VirtualFunctionDelegateCreateExpression : DelegateCreateExpression
    {
        public readonly Expression target;
        public readonly TextRange symbol;
        public readonly TextRange member;
        public VirtualFunctionDelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager, Expression target, TextRange symbol, TextRange member) : base(range, type, callable, manager)
        {
            this.target = target;
            this.symbol = symbol;
            this.member = member;
        }
    }
    internal class LambdaDelegateCreateExpression : DelegateCreateExpression
    {
        public readonly List<Local> parmeters;
        public readonly TextRange symbol;
        public readonly Expression body;

        public LambdaDelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager, List<Local> parmeters, TextRange symbol, Expression body) : base(range, type, callable, manager)
        {
            this.parmeters = parmeters;
            this.symbol = symbol;
            this.body = body;
        }
    }
}
