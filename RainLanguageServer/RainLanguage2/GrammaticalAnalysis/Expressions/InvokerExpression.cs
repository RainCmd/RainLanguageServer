namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal abstract class InvokerExpression : Expression
    {
        public readonly BracketExpression parameters;
        public override bool Valid => true;

        public InvokerExpression(TextRange range, Tuple tuple, BracketExpression parameters, Manager.KernelManager manager) : base(range, tuple)
        {
            this.parameters = parameters;
            if (tuple.Count == 1) attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
            else attribute = ExpressionAttribute.Tuple;
        }
    }
    internal class InvokerDelegateExpression(TextRange range, Tuple tuple, Expression invoker, BracketExpression parameters, Manager.KernelManager manager) : InvokerExpression(range, tuple, parameters, manager)
    {
        public readonly Expression invoker = invoker;
        public override void Read(ExpressionParameter parameter)
        {
            invoker.Read(parameter);
            parameters.Read(parameter);
        }
    }
    internal class InvokerFunctionExpression(TextRange range, Tuple tuple, TextRange? qualifier, QualifiedName name, AbstractCallable callable, BracketExpression parameters, Manager.KernelManager manager) : InvokerExpression(range, tuple, parameters, manager)
    {
        public readonly TextRange? qualifier = qualifier;
        public readonly QualifiedName name = name;
        public readonly AbstractCallable callable = callable;
        public override void Read(ExpressionParameter parameter)
        {
            callable.references.Add(name.name);
            parameters.Read(parameter);
        }
    }
    internal class InvokerMemberExpression(TextRange range, Tuple tuple, TextRange? symbol, TextRange method, Expression? target, AbstractCallable callable, BracketExpression parameters, Manager.KernelManager manager) : InvokerExpression(range, tuple, parameters, manager)
    {
        public readonly TextRange? symbol = symbol;
        public readonly TextRange method = method;
        public readonly Expression? target = target;
        public readonly AbstractCallable callable = callable;
        public override void Read(ExpressionParameter parameter)
        {
            target?.Read(parameter);
            callable.references.Add(method);
            parameters.Read(parameter);
        }
    }
    internal class InvokerVirtualExpression(TextRange range, Tuple tuple, TextRange? symbol, TextRange method, Expression? target, AbstractCallable callable, BracketExpression parameters, Manager.KernelManager manager) : InvokerMemberExpression(range, tuple, symbol, method, target, callable, parameters, manager)
    {
        public override void Read(ExpressionParameter parameter)
        {
            target?.Read(parameter);
            if (callable is AbstractClass.Function function) Reference(function);
            else callable.references.Add(method);
            parameters.Read(parameter);
        }
        private void Reference(AbstractClass.Function function)
        {
            function.references.Add(method);
            foreach (var item in function.implements)
                Reference(item);
        }
    }
}
