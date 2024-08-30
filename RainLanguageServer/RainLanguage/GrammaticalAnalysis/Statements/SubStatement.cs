
namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements
{
    internal class SubStatement(Statement parent) : Statement
    {
        public readonly Statement parent = parent;

        public BlockStatement CreateBlock(TextRange range)
        {
            if (parent is BranchStatement branchStatement) return branchStatement.falseBranch = new BlockStatement(range);
            else if (parent is LoopStatement loopStatement) return loopStatement.elseBlock = new BlockStatement(range);
            else if (parent is TryStatement tryStatement) return tryStatement.finallyBlock = new BlockStatement(range);
            else throw new InvalidOperationException();
        }

        public override void Operator(Action<Expression> action) => throw new NotImplementedException();
        public override bool Operator(TextPosition position, ExpressionOperator action) => throw new NotImplementedException();
        public override bool TryHighlightGroup(TextPosition position, List<HighlightInfo> infos) => throw new NotImplementedException();
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector) => throw new NotImplementedException();
    }
}
