namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class BlurryLambdaExpression : Expression
    {
        public readonly List<TextRange> parameters;
        public readonly TextRange symbol;
        public readonly TextRange body;
        public override bool Valid => true;

        public BlurryLambdaExpression(TextRange range, List<TextRange> parameters, TextRange symbol,TextRange body) : base(range, new Tuple([BLURRY]))
        {
            this.parameters = parameters;
            this.symbol = symbol;
            this.body = body;
            attribute = ExpressionAttribute.Value;
        }
    }
}
