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
        public readonly Expression source;
        public readonly TextRange symbol;
        public readonly TextRange member;
        public MemberFunctionDelegateCreateExpression(TextRange range, Type type, AbstractCallable callable, Manager.KernelManager manager) : base(range, type, callable, manager)
        {

        }
    }
}
