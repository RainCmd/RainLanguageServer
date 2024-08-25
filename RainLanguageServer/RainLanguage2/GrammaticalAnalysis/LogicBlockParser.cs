using RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions;
using RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Statements;
using System.Diagnostics.CodeAnalysis;

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
        public void Parse(Manager manager)
        {
            if (body.Count > 0)
            {
                var parser = new ExpressionParser(manager, context, localContext, collector, destructor);
                var stack = new Stack<List<Statement>>();
                stack.Push(logicBlock.statements);
                var indents = new Stack<int>();
                indents.Push(-1);
                for (var index = 0; index < body.Count; index++)
                {
                    var line = body[index];
                    if (indents.Peek() < 0)
                    {
                        indents.Pop(); indents.Push(line.indent);
                    }
                    else if (indents.Peek() < line.indent)
                    {
                        List<Statement>? statements = null;
                        if (stack.Peek().Count > 0)
                        {
                            var prev = stack.Peek()[^1];
                            if (prev is BranchStatement branchStatement)
                            {
                                branchStatement.trueBranch = new BlockStatement();
                                statements = branchStatement.trueBranch.statements;
                            }
                            else if (prev is LoopStatement loopStatement)
                            {
                                loopStatement.loopBlock = new BlockStatement();
                                statements = loopStatement.loopBlock.statements;
                            }
                            else if (prev is TryStatement tryStatement)
                            {
                                if (tryStatement.tryBlock == null)
                                {
                                    tryStatement.tryBlock = new BlockStatement();
                                    statements = tryStatement.tryBlock.statements;
                                }
                                else statements = tryStatement.catchBlocks[^1].block.statements;
                            }
                            else if (prev is SubStatement subStatement) statements = subStatement.CreateBlock().statements;
                        }
                        if (statements == null)
                        {
                            var block = new BlockStatement();
                            stack.Peek().Add(block);
                            statements = block.statements;
                        }
                        localContext.PushBlock();
                        stack.Push(statements);
                        indents.Push(line.indent);
                    }
                    else while (stack.Count > 0)
                        {
                            if (indents.Peek() > line.indent)
                            {
                                if (stack.Count > 1)
                                {
                                    localContext.PopBlock();
                                    stack.Pop();
                                    indents.Pop();
                                }
                                else
                                {
                                    collector.Add(line, ErrorLevel.Error, "缩进错误");
                                    break;
                                }
                            }
                            else if (indents.Peek() < line.indent)
                            {
                                collector.Add(line, ErrorLevel.Error, "缩进错误");
                                break;
                            }
                            else
                            {
                                if (Lexical.TryAnalysis(line, 0, out var value, collector) && value.anchor != KeyWords.ELSEIF && value.anchor != KeyWords.ELSE)
                                {
                                    var indent = indents.Pop();
                                    while (indents.Count > 0 && indents.Peek() == line.indent)
                                    {
                                        indent = indents.Pop();
                                        stack.Pop();
                                        localContext.PushBlock();
                                    }
                                    indents.Push(indent);
                                }
                                break;
                            }
                        }
                    if (Lexical.TryAnalysis(line, 0, out var lexical, collector))
                    {
                        if (lexical.anchor == KeyWords.IF) ParseBranch(manager, parser, stack.Peek(), line, lexical, []);
                        else if (lexical.anchor == KeyWords.ELSEIF)
                        {
                            if (stack.Peek().Count > 0)
                            {
                                if (stack.Peek()[^1] is BranchStatement branch)
                                {
                                    branch.elseSymbol = lexical.anchor;
                                    branch.falseBranch = new BlockStatement();
                                    stack.Push(branch.falseBranch.statements);
                                    indents.Push(line.indent);
                                    localContext.PushBlock();
                                    ParseBranch(manager, parser, stack.Peek(), line, lexical, branch.group);
                                    continue;
                                }
                                else if (stack.Peek()[^1] is LoopStatement loop)
                                {
                                    loop.elseSymbol = lexical.anchor;
                                    loop.elseBlock = new BlockStatement();
                                    stack.Push(loop.elseBlock.statements);
                                    indents.Push(line.indent);
                                    localContext.PushBlock();
                                    ParseBranch(manager, parser, stack.Peek(), line, lexical, loop.group);
                                    continue;
                                }
                            }
                            collector.Add(lexical.anchor, ErrorLevel.Error, "elseif语句必须在if、elseif、while和for语句之后");
                        }
                        else if (lexical.anchor == KeyWords.ELSE)
                        {
                            if (stack.Peek().Count > 0)
                            {
                                if (stack.Peek()[^1] is BranchStatement branch)
                                {
                                    branch.elseSymbol = lexical.anchor;
                                    branch.group.Add(lexical.anchor);
                                    stack.Peek().Add(new SubStatement(branch));
                                }
                                else if (stack.Peek()[^1] is LoopStatement loop)
                                {
                                    loop.elseSymbol = lexical.anchor;
                                    loop.group.Add(lexical.anchor);
                                    stack.Peek().Add(new SubStatement(loop));
                                }
                                else collector.Add(lexical.anchor, ErrorLevel.Error, "else语句必须在if、elseif、while和for语句之后");
                            }
                            else collector.Add(lexical.anchor, ErrorLevel.Error, "else语句必须在if、elseif、while和for语句之后");
                            if (Lexical.TryAnalysis(line, lexical.anchor.end, out lexical, collector))
                                collector.Add(lexical.anchor, ErrorLevel.Error, "无效的表达式");
                        }
                        else if (lexical.anchor == KeyWords.WHILE)
                        {
                            var range = (lexical.anchor.end & line.end).Trim;
                            if (range.Count > 0)
                            {
                                var condition = parser.Parse(lexical.anchor.end & line.end);
                                if (condition.Valid)
                                {
                                    if (!condition.attribute.ContainAny(ExpressionAttribute.Value)) collector.Add(condition.range, ErrorLevel.Error, "表达式返回值不是一个有效值");
                                    else if (condition.tuple[0] != manager.kernelManager.BOOL) collector.Add(condition.range, ErrorLevel.Error, "表达式返回值不是一个布尔值");
                                }
                                stack.Peek().Add(new WhileStatement(lexical.anchor, condition));
                                continue;
                            }
                            stack.Peek().Add(new WhileStatement(lexical.anchor, null));
                        }
                        else if (lexical.anchor == KeyWords.FOR)
                        {
                            var range = lexical.anchor.end & line.end;
                            var separator1 = ExpressionSplit.Split(range, SplitFlag.Semicolon, out var left, out var right, collector);
                            if (separator1.type == LexicalType.Semicolon)
                            {
                                var front = parser.Parse(left);
                                Expression? condition, back;
                                range = right;
                                var separator2 = ExpressionSplit.Split(range, SplitFlag.Semicolon, out left, out right, collector);
                                if (separator2.type == LexicalType.Semicolon)
                                {
                                    condition = parser.Parse(left);
                                    back = parser.Parse(right);
                                }
                                else
                                {
                                    condition = parser.Parse(range);
                                    back = null;
                                }
                                if (condition.Valid && !(condition.attribute.ContainAny(ExpressionAttribute.Value) && condition.tuple[0] != manager.kernelManager.BOOL))
                                    collector.Add(condition.range, ErrorLevel.Error, "表达式返回值不是布尔类型");
                                var loop = new ForStatement(lexical.anchor, condition, separator1.anchor, separator2.type == LexicalType.Semicolon ? separator2.anchor : null, front, back);
                                loop.group.Add(lexical.anchor);
                                stack.Peek().Add(loop);
                            }
                            else
                            {
                                collector.Add(lexical.anchor, ErrorLevel.Error, "for循环需要用 ; 分隔初始化表达式、条件表达式和更新表达式");
                                var expression = parser.Parse(range);
                                var loop = new ForStatement(lexical.anchor, null, null, null, expression, null);
                                loop.group.Add(lexical.anchor);
                                stack.Peek().Add(loop);
                            }
                        }
                        else if (lexical.anchor == KeyWords.BREAK)
                        {
                            var range = (lexical.anchor.end & line.end).Trim;
                            Expression? condition = null;
                            if (range.Count > 0)
                            {
                                condition = parser.Parse(range);
                                if (condition.Valid)
                                {
                                    if (!condition.attribute.ContainAny(ExpressionAttribute.Value)) collector.Add(condition.range, ErrorLevel.Error, "表达式返回值不是一个有效值");
                                    else if (condition.tuple[0] != manager.kernelManager.BOOL) collector.Add(condition.range, ErrorLevel.Error, "表达式返回值不是一个布尔值");
                                }
                            }
                            if (!TryGetLoopStatement(stack, out var loop))
                                collector.Add(lexical.anchor, ErrorLevel.Error, "brack语句必须while或for循环中");
                            var jump = new BreakStatement(loop, condition);
                            loop?.group.Add(lexical.anchor);
                            stack.Peek().Add(jump);
                        }
                        else if (lexical.anchor == KeyWords.CONTINUE)
                        {
                            var range = (lexical.anchor.end & line.end).Trim;
                            Expression? condition = null;
                            if (range.Count > 0)
                            {
                                condition = parser.Parse(range);
                                if (condition.Valid)
                                {
                                    if (!condition.attribute.ContainAny(ExpressionAttribute.Value)) collector.Add(condition.range, ErrorLevel.Error, "表达式返回值不是一个有效值");
                                    else if (condition.tuple[0] != manager.kernelManager.BOOL) collector.Add(condition.range, ErrorLevel.Error, "表达式返回值不是一个布尔值");
                                }
                            }
                            if (!TryGetLoopStatement(stack, out var loop))
                                collector.Add(lexical.anchor, ErrorLevel.Error, "continue语句必须while或for循环中");
                            var jump = new ContinueStatement(loop, condition);
                            loop?.group.Add(lexical.anchor);
                            stack.Peek().Add(jump);
                        }
                        else if (lexical.anchor == KeyWords.RETURN)
                        {
                        }
                        else if (lexical.anchor == KeyWords.WAIT)
                        {
                        }
                        else if (lexical.anchor == KeyWords.EXIT)
                        {
                        }
                        else if (lexical.anchor == KeyWords.TRY)
                        {
                        }
                        else if (lexical.anchor == KeyWords.CATCH)
                        {
                        }
                        else if (lexical.anchor == KeyWords.FINALLY)
                        {
                        }
                        else
                        {
                            var expression = parser.Parse(line);
                            if (ExpressionParser.ContainBlurry(expression))
                                collector.Add(expression.range, ErrorLevel.Error, "类型不明确");
                            stack.Peek().Add(new ExpressionStatement(expression));
                        }
                    }
                }
            }
            for (var i = 0; i < logicBlock.statements.Count; i++)
                if (CheckReturn(logicBlock.statements[i]))
                {
                    InaccessibleCodeWarning(logicBlock.statements, i);
                    return;
                }
            if (returns.Count > 0)
                collector.Add(name, ErrorLevel.Error, "不是所有路径都有返回值");
        }
        private bool TryGetLoopStatement(IEnumerable<List<Statement>> statements, [MaybeNullWhen(false)] out LoopStatement result)
        {
            foreach (var list in statements)
                if (list.Count > 0 && list[^1] is LoopStatement loop)
                {
                    result = loop;
                    return true;
                }
            result = null;
            return false;
        }
        private void ParseBranch(Manager manager, ExpressionParser parser, List<Statement> statements, TextLine line, Lexical lexical, List<TextRange> group)
        {
            var condition = parser.Parse(lexical.anchor.end & line.end);
            if (condition.Valid)
            {
                if (!condition.attribute.ContainAny(ExpressionAttribute.Value)) collector.Add(condition.range, ErrorLevel.Error, "表达式返回值不是一个有效值");
                else if (condition.tuple[0] != manager.kernelManager.BOOL) collector.Add(condition.range, ErrorLevel.Error, "表达式返回值不是一个布尔值");
            }
            group.Add(lexical.anchor);
            statements.Add(new BranchStatement(lexical.anchor, condition, group));
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
            if (statement is TryStatement tryStatement) return CheckReturn(tryStatement.tryBlock);
            return false;
        }
        private void InaccessibleCodeWarning(List<Statement> statements, int index)
        {
            if (index + 1 < statements.Count)
                collector.Add(statements[index].range, ErrorLevel.Warning, "无法访问的代码");
        }
    }
}
