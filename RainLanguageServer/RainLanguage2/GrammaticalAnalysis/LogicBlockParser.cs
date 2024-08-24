using RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements;

namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis
{
    internal class LogicBlock
    {
        public readonly List<Local> parameters = [];
        public readonly List<Statement> statements = [];
    }
    internal class LogicBlockParser
    {
        private readonly TextRange name;
        private readonly Context context;
        private readonly LocalContext localContext;
        private readonly Tuple returns;
        private readonly List<TextLine> body;
        private readonly MessageCollector collector;
        private readonly bool destructor;
        private readonly LogicBlock logicBlock;
        public LogicBlockParser(LogicBlock logicBlock, AbstractDeclaration? declaration, AbstractCallable callable, List<TextLine> body)
        {
            name = callable.name;
            context = new Context(callable.file.space.document, callable.space, callable.file.space.relies, declaration);
            localContext = new LocalContext(callable.file.space.collector, declaration);
            foreach (var parameter in callable.parameters)
                if (parameter.name != null)
                    logicBlock.parameters.Add(localContext.Add(parameter.name.Value, parameter.type, true));
            returns = callable.returns;
            this.body = body;
            collector = callable.file.space.collector;
            destructor = false;
            this.logicBlock = logicBlock;
        }
        public LogicBlockParser(TextRange name, LogicBlock logicBlock, AbstractDeclaration declaration, HashSet<AbstractSpace> relies, List<TextLine> body)
        {
            this.name = name;
            context = new Context(declaration.file.space.document, declaration.space, relies, declaration);
            localContext = new LocalContext(declaration.file.space.collector, declaration);
            returns = [];
            this.body = body;
            collector = declaration.file.space.collector;
            destructor = true;
            this.logicBlock = logicBlock;
        }
        private bool CheckReturn(Statement? statement)
        {
            if (statement is ExitStatement || statement is ReturnStatement) return true;
            else if (statement is JumpStatement jumpStatement) return jumpStatement.condition == null;
            else if (statement is BlockStatement blockStatement)
            {
                for (var i = 0; i < blockStatement.statements.Count; i++)
                {
                    var subStatement = blockStatement.statements[i];
                    if (CheckReturn(subStatement))
                    {
                        InaccessibleCodeWarning(blockStatement.statements, i);
                        return true;
                    }
                }
            }
            else if (statement is BranchStatement branchStatement) return CheckReturn(branchStatement.trueBranch) && CheckReturn(branchStatement.falseBranch);
            else if (statement is LoopStatement loopStatement)
            {
                if (loopStatement.loopBlock != null && loopStatement.condition == null)
                {
                    var hasContinue = false;
                    for (int i = 0; i < loopStatement.loopBlock.statements.Count; i++)
                    {
                        var subStatement = loopStatement.loopBlock.statements[i];
                        if (subStatement is BreakStatement breakStatement)
                        {
                            if (breakStatement.condition == null)
                                InaccessibleCodeWarning(loopStatement.loopBlock.statements, i);
                            return false;
                        }
                        else if (!hasContinue)
                        {
                            if (subStatement is ContinueStatement continueStatement)
                            {
                                if (continueStatement.condition == null)
                                {
                                    InaccessibleCodeWarning(loopStatement.loopBlock.statements, i);
                                    return true;
                                }
                                else hasContinue = true;
                            }
                            if (CheckReturn(subStatement))
                            {
                                InaccessibleCodeWarning(loopStatement.loopBlock.statements, i);
                                return true;
                            }
                        }
                    }
                }
                if (loopStatement.elseBlock != null)
                {
                    if (loopStatement.condition == null)
                    {
                        collector.Add(loopStatement.elseBlock.statements[0].range, ErrorLevel.Warning, "无法访问的代码");
                        return true;
                    }
                    else return CheckReturn(loopStatement.elseBlock);
                }
            }
            if(statement is TryStatement tryStatement) return CheckReturn(tryStatement.tryBlock);
            return false;
        }
        private void InaccessibleCodeWarning(List<Statement> statements, int index)
        {
            if (index + 1 < statements.Count)
                collector.Add(statements[index].range, ErrorLevel.Warning, "无法访问的代码");
        }
    }
}
