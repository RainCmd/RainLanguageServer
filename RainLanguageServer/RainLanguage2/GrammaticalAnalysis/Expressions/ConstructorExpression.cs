namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class ConstructorExpression : Expression
    {
        public readonly TypeExpression type;
        public readonly AbstractCallable? callable;
        public readonly List<AbstractCallable>? callables;
        public readonly BracketExpression parameters;
        public override bool Valid => true;
        public ConstructorExpression(TextRange range, TypeExpression type, AbstractCallable? callable, List<AbstractCallable>? callables, BracketExpression parameters, Manager.KernelManager manager) : base(range, type.type)
        {
            this.type = type;
            this.callable = callable;
            this.callables = callables;
            this.parameters = parameters;
            attribute = ExpressionAttribute.Value | type.type.GetAttribute(manager);
        }
        public override void Read(ExpressionParameter parameter)
        {
            type.Read(parameter);
            callable?.references.Add(type.range);
            if (callables != null)
                foreach(var item in callables)
                    item.references.Add(type.range);
            parameters.Read(parameter);
        }
    }
}
