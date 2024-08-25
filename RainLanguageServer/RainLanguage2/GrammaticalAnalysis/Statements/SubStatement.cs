namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements
{
    internal class SubStatement(Statement parent) : Statement
    {
        public readonly Statement parent = parent;

        public BlockStatement CreateBlock()
        {
            if (parent is BranchStatement branchStatement) return branchStatement.falseBranch = new BlockStatement();
            else if (parent is LoopStatement loopStatement) return loopStatement.elseBlock = new BlockStatement();
            else if (parent is TryStatement tryStatement) return tryStatement.finallyBlock = new BlockStatement();
            else throw new InvalidOperationException();
        }
    }
}
