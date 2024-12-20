﻿using RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions;
using RainLanguageServer.RainLanguage.GrammaticalAnalysis.Statements;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis
{
    internal class LogicBlock
    {
        public readonly List<Local> parameters = [];
        public readonly List<Statement> statements = [];
    }
    internal readonly struct LogicBlockParser
    {
        private readonly Manager manager;
        private readonly TextRange name;
        private readonly Context context;
        private readonly LocalContext localContext;
        private readonly Tuple returns;
        private readonly List<TextLine> body;
        private readonly MessageCollector collector;
        private readonly bool destructor;
        private readonly LogicBlock logicBlock;
        private LogicBlockParser(Manager manager, TextRange name, Context context, LocalContext localContext, Tuple returns, List<TextLine> body, MessageCollector collector, bool destructor, LogicBlock logicBlock)
        {
            this.manager = manager;
            this.name = name;
            this.context = context;
            this.localContext = localContext;
            this.returns = returns;
            this.body = body;
            this.collector = collector;
            this.destructor = destructor;
            this.logicBlock = logicBlock;
        }
        private void PushBlock(Stack<List<Statement>> stack, Stack<int> indents, TextLine line)
        {
            var block = new BlockStatement(line.start & line.start);
            stack.Peek().Add(block);
            localContext.PushBlock();
            stack.Push(block.statements);
            indents.Push(line.indent);
        }
        private void Parse()
        {
            if (body.Count > 0)
            {
                var symbolGroup = new List<TextRange>();
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
                                branchStatement.trueBranch = new BlockStatement(line.start & line.start);
                                statements = branchStatement.trueBranch.statements;
                            }
                            else if (prev is LoopStatement loopStatement)
                            {
                                loopStatement.loopBlock = new BlockStatement(line.start & line.start);
                                statements = loopStatement.loopBlock.statements;
                            }
                            else if (prev is TryStatement tryStatement)
                            {
                                if (tryStatement.tryBlock == null)
                                {
                                    tryStatement.tryBlock = new BlockStatement(line.start & line.start);
                                    statements = tryStatement.tryBlock.statements;
                                }
                                else statements = tryStatement.catchBlocks[^1].block.statements;
                            }
                            else if (prev is SubStatement subStatement) statements = subStatement.CreateBlock(line.start & line.start).statements;
                        }
                        if (statements == null)
                        {
                            var block = new BlockStatement(line.start & line.start);
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
                                        localContext.PopBlock();
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
                                    branch.falseBranch = new BlockStatement(line.end & line.end);
                                    stack.Push(branch.falseBranch.statements);
                                    indents.Push(line.indent);
                                    localContext.PushBlock();
                                    ParseBranch(manager, parser, stack.Peek(), line, lexical, branch.group);
                                    continue;
                                }
                                else if (stack.Peek()[^1] is LoopStatement loop)
                                {
                                    loop.elseSymbol = lexical.anchor;
                                    loop.elseBlock = new BlockStatement(line.end & line.end);
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
                            PushBlock(stack, indents, line);
                            var range = lexical.anchor.end & line.end;
                            var separator1 = ExpressionSplit.Split(range, SplitFlag.Colon, out var left, out var right, collector);
                            if (separator1.type == LexicalType.Colon)
                            {
                                var element = parser.Parse(left);
                                var iterator = parser.Parse(right);
                                if (iterator.attribute.ContainAny(ExpressionAttribute.Value))
                                {
                                    if (ExpressionParser.Convert(manager, iterator.tuple[0], manager.kernelManager.ENUMERABLE) >= 0)
                                    {
                                        if (!element.attribute.ContainAny(ExpressionAttribute.Assignable)) collector.Add(element.range, ErrorLevel.Error, "不可赋值");
                                        else if (element.tuple.Count != 1) collector.Add(element.range, ErrorLevel.Error, "不能是元组");
                                        else parser.TryInferLeftValueType(ref element, manager.kernelManager.HANDLE);
                                        if (manager.TryGetDeclaration(manager.kernelManager.ENUMERABLE, out var declaration) && declaration is AbstractInterface abstractInterface)
                                        {
                                            var function = abstractInterface.functions[0];
                                            foreach (var implement in function.implements)
                                                if (implement.declaration.DefineType == iterator.tuple[0])
                                                {
                                                    implement.references.Add(separator1.anchor);
                                                    break;
                                                }
                                        }
                                    }
                                    else if (manager.TryGetDeclaration(iterator.tuple[0], out var abstractDeclaration) && abstractDeclaration is AbstractDelegate abstractDelegate)
                                    {
                                        if (abstractDelegate.parameters.Count == 0 && abstractDelegate.returns.Count > 0 && abstractDelegate.returns[0] == manager.kernelManager.BOOL)
                                        {
                                            if (!element.attribute.ContainAny(ExpressionAttribute.Assignable)) collector.Add(element.range, ErrorLevel.Error, "不可赋值");
                                            else if (element.tuple.Count != abstractDelegate.returns.Count - 1) collector.Add(element.range, ErrorLevel.Error, "类型数量不一致");
                                            element = parser.InferLeftValueType(element, abstractDelegate.returns[1..]);
                                            iterator = new TupleCastExpression(iterator, abstractDelegate.returns[1..], localContext.Snapshoot, manager.kernelManager);
                                            iterator = parser.AssignmentConvert(iterator, element.tuple);
                                        }
                                        else collector.Add(iterator.range, ErrorLevel.Error, "必须是无参且第一个返回值是bool类型的委托才能迭代");
                                    }
                                    else collector.Add(iterator.range, ErrorLevel.Error, "不是可迭代对象");
                                }
                                else collector.Add(iterator.range, ErrorLevel.Error, "不是个值");
                                var loop = new ForeachStatement(lexical.anchor, iterator, separator1.anchor, element);
                                loop.group.Add(lexical.anchor);
                                stack.Peek().Add(loop);
                            }
                            else
                            {
                                separator1 = ExpressionSplit.Split(range, SplitFlag.Semicolon, out left, out right, collector);
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
                                    if (condition.Valid && !(condition.attribute.ContainAny(ExpressionAttribute.Value) && condition.tuple[0] == manager.kernelManager.BOOL))
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
                            stack.Peek().Add(new BreakStatement(lexical.anchor, condition));
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
                            stack.Peek().Add(new ContinueStatement(lexical.anchor, condition));
                        }
                        else if (lexical.anchor == KeyWords.RETURN)
                        {
                            var result = parser.Parse(lexical.anchor.end & line.end);
                            if (result.Valid) result = parser.AssignmentConvert(result, returns);
                            stack.Peek().Add(new ReturnStatement(lexical.anchor, result, symbolGroup));
                        }
                        else if (lexical.anchor == KeyWords.WAIT)
                        {
                            var range = (lexical.anchor.end & line.end).Trim;
                            Expression? expression = null;
                            if (range.Count > 0)
                            {
                                expression = parser.Parse(range);
                                if (!expression.attribute.ContainAny(ExpressionAttribute.Value)) collector.Add(expression.range, ErrorLevel.Error, "表达式返回值不是一个有效值");
                                else if (expression.tuple[0] != manager.kernelManager.BOOL && expression.tuple[0] != manager.kernelManager.INT && expression.tuple[0].code != TypeCode.Task)
                                    collector.Add(expression.range, ErrorLevel.Error, "wait语句的等待目标类型必须是bool、integer或task");
                            }
                            stack.Peek().Add(new WaitStatement(lexical.anchor, expression, symbolGroup));
                        }
                        else if (lexical.anchor == KeyWords.EXIT)
                        {
                            var expression = parser.Parse(lexical.anchor.end & line.end);
                            if (expression.Valid)
                            {
                                if (!expression.attribute.ContainAny(ExpressionAttribute.Value)) collector.Add(expression.range, ErrorLevel.Error, "表达式返回值不是一个有效值");
                                else if (expression.tuple[0] != manager.kernelManager.STRING)
                                    collector.Add(expression.range, ErrorLevel.Error, "exit语句的参数必须是字符串");
                            }
                            stack.Peek().Add(new ExitStatement(lexical.anchor, expression, symbolGroup));
                        }
                        else if (lexical.anchor == KeyWords.TRY)
                        {
                            stack.Peek().Add(new TryStatement(lexical.anchor));
                            if (Lexical.TryAnalysis(line, lexical.anchor.end, out lexical, collector))
                                collector.Add(lexical.anchor, ErrorLevel.Error, "无效的表达式");
                        }
                        else if (lexical.anchor == KeyWords.CATCH)
                        {
                            if (stack.Peek().Count > 0 && stack.Peek()[^1] is TryStatement tryStatement)
                            {
                                if (tryStatement.tryBlock == null)
                                {
                                    var tryLine = tryStatement.trySymbol.start.Line;
                                    tryStatement.tryBlock = new BlockStatement(tryLine.end & tryLine.end);
                                }
                                tryStatement.group.Add(lexical.anchor);
                                var range = (lexical.anchor.end & line.end).Trim;
                                Expression? expression = null;
                                if (range.Count > 0)
                                {
                                    expression = parser.Parse(range);
                                    if (expression.Valid)
                                    {
                                        parser.TryInferLeftValueType(ref expression, manager.kernelManager.STRING);
                                        if (!expression.attribute.ContainAny(ExpressionAttribute.Value)) collector.Add(expression.range, ErrorLevel.Error, "表达式返回值不是一个有效值");
                                        else if (expression.tuple[0] != manager.kernelManager.STRING) collector.Add(expression.range, ErrorLevel.Error, "catch语句的表达式必须是字符串类型");
                                    }
                                }
                                tryStatement.catchBlocks.Add(new TryStatement.CatchBlock(lexical.anchor, expression, new BlockStatement(line.end & line.end)));
                            }
                            else collector.Add(lexical.anchor, ErrorLevel.Error, "catch语句必须在try或catch语句之后");
                        }
                        else if (lexical.anchor == KeyWords.FINALLY)
                        {
                            if (stack.Peek().Count > 0 && stack.Peek()[^1] is TryStatement tryStatement)
                            {
                                if (tryStatement.tryBlock == null)
                                {
                                    var tryLine = tryStatement.trySymbol.start.Line;
                                    tryStatement.tryBlock = new BlockStatement(tryLine.end & tryLine.end);
                                }
                                tryStatement.finallySymbol = lexical.anchor;
                                tryStatement.group.Add(lexical.anchor);
                                stack.Peek().Add(new SubStatement(tryStatement));
                            }
                            else collector.Add(lexical.anchor, ErrorLevel.Error, "finally语句必须在try或catch语句之后");
                            if (Lexical.TryAnalysis(line, lexical.anchor.end, out lexical, collector))
                                collector.Add(lexical.anchor, ErrorLevel.Error, "无效的表达式");
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
                Trim(logicBlock.statements);
                var parameter = new ExpressionParameter(manager, collector);
                foreach (var statement in logicBlock.statements)
                    statement.Read(parameter);
            }
            for (var i = 0; i < logicBlock.statements.Count; i++)
            {
                CheckFunctionStatementValidity(logicBlock.statements[i], null, false);
                if (CheckReturn(logicBlock.statements[i], out var exit) || exit)
                {
                    InaccessibleCodeWarning(logicBlock.statements, i);
                    return;
                }
            }
            if (returns.Count > 0)
                collector.Add(name, ErrorLevel.Error, "不是所有路径都有返回值");
        }
        private void CheckFunctionStatementValidity(Statement statement, List<TextRange>? loopGroup, bool exited)
        {
            if (statement is BlockStatement blockStatement)
            {
                foreach (var item in blockStatement.statements)
                    CheckFunctionStatementValidity(item, loopGroup, exited);
            }
            else if (statement is BranchStatement branchStatement)
            {
                if (branchStatement.trueBranch != null) CheckFunctionStatementValidity(branchStatement.trueBranch, loopGroup, exited);
                if (branchStatement.falseBranch != null) CheckFunctionStatementValidity(branchStatement.falseBranch, loopGroup, exited);
            }
            else if (statement is LoopStatement loopStatement)
            {
                if (loopStatement.loopBlock != null) CheckFunctionStatementValidity(loopStatement.loopBlock, loopStatement.group, exited);
                if (loopStatement.elseBlock != null) CheckFunctionStatementValidity(loopStatement.elseBlock, loopGroup, exited);
            }
            else if (statement is TryStatement tryStatement)
            {
                if (tryStatement.tryBlock != null) CheckFunctionStatementValidity(tryStatement.tryBlock, loopGroup, exited);
                foreach (var item in tryStatement.catchBlocks)
                    CheckFunctionStatementValidity(item.block, null, true);
                if (tryStatement.finallyBlock != null) CheckFunctionStatementValidity(tryStatement.finallyBlock, null, true);
            }
            else if (statement is JumpStatement jumpStatement)
            {
                if (loopGroup == null) collector.Add(jumpStatement.symbol, ErrorLevel.Error, "不在循环语句中");
                else
                {
                    jumpStatement.group = loopGroup;
                    loopGroup.Add(jumpStatement.symbol);
                }
            }
            else if (statement is ReturnStatement returnStatement)
            {
                if (exited) collector.Add(returnStatement.symbol, ErrorLevel.Error, "catch和finally中不能返回");
            }
        }
        private static void Trim(List<Statement> statements)
        {
            for (var i = statements.Count - 1; i >= 0; i--)
            {
                var statement = statements[i];
                if (statement is BlockStatement blockStatement)
                {
                    Trim(blockStatement.statements);
                    TryTidyBlockRange(blockStatement);
                }
                else if (statement is BranchStatement branchStatement)
                {
                    branchStatement.range = branchStatement.ifSymbol & branchStatement.condition.range;
                    if (branchStatement.trueBranch != null)
                    {
                        Trim(branchStatement.trueBranch.statements);
                        if (TryTidyBlockRange(branchStatement.trueBranch))
                            branchStatement.range &= branchStatement.trueBranch.range;
                    }
                    if (branchStatement.falseBranch != null)
                    {
                        Trim(branchStatement.falseBranch.statements);
                        if (TryTidyBlockRange(branchStatement.falseBranch))
                            branchStatement.range &= branchStatement.falseBranch.range;
                        else if (branchStatement.elseSymbol != null) branchStatement.range &= branchStatement.elseSymbol.Value;
                    }
                }
                else if (statement is LoopStatement loopStatement)
                {
                    if (loopStatement is ForStatement forStatement)
                    {
                        if (forStatement.back != null) forStatement.range = forStatement.symbol & forStatement.back.range;
                        else if (forStatement.separator2 != null) forStatement.range = forStatement.symbol & forStatement.separator2.Value;
                        else if (forStatement.condition != null) forStatement.range = forStatement.symbol & forStatement.condition.range;
                        else if (forStatement.separator1 != null) forStatement.range = forStatement.symbol & forStatement.separator1.Value;
                        else if (forStatement.front != null) forStatement.range = forStatement.symbol & forStatement.front.range;
                        else forStatement.range = forStatement.symbol;
                    }
                    else loopStatement.range = loopStatement.condition == null ? loopStatement.symbol : loopStatement.symbol & loopStatement.condition.range;
                    if (loopStatement.loopBlock != null)
                    {
                        Trim(loopStatement.loopBlock.statements);
                        if (TryTidyBlockRange(loopStatement.loopBlock))
                            loopStatement.range &= loopStatement.loopBlock.range;
                    }
                    if (loopStatement.elseBlock != null)
                    {
                        Trim(loopStatement.elseBlock.statements);
                        if (TryTidyBlockRange(loopStatement.elseBlock))
                            loopStatement.range &= loopStatement.elseBlock.range;
                        else if (loopStatement.elseSymbol != null) loopStatement.range &= loopStatement.elseSymbol.Value;
                    }
                }
                else if (statement is TryStatement tryStatement)
                {
                    tryStatement.range = tryStatement.trySymbol;
                    if (tryStatement.tryBlock != null)
                    {
                        Trim(tryStatement.tryBlock.statements);
                        if (TryTidyBlockRange(tryStatement.tryBlock))
                            tryStatement.range &= tryStatement.tryBlock.range;
                    }
                    foreach (var catchBlock in tryStatement.catchBlocks)
                    {
                        Trim(catchBlock.block.statements);
                        if (TryTidyBlockRange(catchBlock.block))
                            tryStatement.range &= catchBlock.block.range;
                        else if (catchBlock.expression != null)
                            tryStatement.range &= catchBlock.expression.range;
                        else tryStatement.range &= catchBlock.catchSymbol;
                    }
                    if (tryStatement.finallySymbol != null) tryStatement.range &= tryStatement.finallySymbol.Value;
                    if (tryStatement.finallyBlock != null)
                    {
                        Trim(tryStatement.finallyBlock.statements);
                        if (TryTidyBlockRange(tryStatement.finallyBlock))
                            tryStatement.range &= tryStatement.finallyBlock.range;
                    }
                }
                else if (statement is SubStatement) statements.RemoveAt(i);
            }
        }
        private static bool TryTidyBlockRange(BlockStatement blockStatement)
        {
            if (blockStatement.statements.Count > 0)
            {
                blockStatement.range = blockStatement.statements[0].range & blockStatement.statements[^1].range;
                return true;
            }
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
        private bool CheckReturn(Statement? statement, out bool exit)
        {
            if (statement is ExitStatement)
            {
                exit = true;
                return false;
            }
            exit = false;
            if (statement is ReturnStatement) return true;
            else if (statement is JumpStatement jumpStatement) return jumpStatement.condition == null && jumpStatement.group != null;
            else if (statement is BlockStatement blockStatement)
            {
                for (var i = 0; i < blockStatement.statements.Count; i++)
                {
                    var subStatement = blockStatement.statements[i];
                    if (CheckReturn(subStatement, out exit))
                    {
                        InaccessibleCodeWarning(blockStatement.statements, i);
                        return true;
                    }
                    else if (exit)
                    {
                        InaccessibleCodeWarning(blockStatement.statements, i);
                        return false;
                    }
                }
            }
            else if (statement is BranchStatement branchStatement)
            {
                var result = CheckReturn(branchStatement.trueBranch, out var exitT) & CheckReturn(branchStatement.falseBranch, out var exitF);
                exit = exitT & exitF | result;
                return result;
            }
            else if (statement is LoopStatement loopStatement)
            {
                var resultLoop = false;
                var exitLoop = false;
                var resultElse = false;
                var exitElse = false;
                if (loopStatement.loopBlock != null)
                {
                    if (loopStatement.condition == null)
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
                                if (CheckReturn(subStatement, out exit))
                                {
                                    InaccessibleCodeWarning(loopStatement.loopBlock.statements, i);
                                    return true;
                                }
                                else if (exit)
                                {
                                    InaccessibleCodeWarning(loopStatement.loopBlock.statements, i);
                                    return false;
                                }
                            }
                        }
                    }
                    else resultLoop = CheckReturn(loopStatement.loopBlock, out exitLoop);
                }
                if (loopStatement.elseBlock != null)
                {
                    if (loopStatement.condition == null)
                    {
                        collector.Add(loopStatement.elseBlock.statements[0].range, ErrorLevel.Hint, "无法访问的代码", true);
                        return true;
                    }
                    else resultElse = CheckReturn(loopStatement.elseBlock, out exitElse);
                }
                exit = (exitLoop | resultLoop) & (exitElse | resultElse);
                return resultLoop & resultElse;
            }
            if (statement is TryStatement tryStatement)
            {
                if (CheckReturn(tryStatement.tryBlock, out exit)) return true;
                else if (exit) exit = tryStatement.catchBlocks.Count == 0;
            }
            return false;
        }
        private void InaccessibleCodeWarning(List<Statement> statements, int index)
        {
            if (index + 1 < statements.Count)
                collector.Add(statements[index + 1].range, ErrorLevel.Hint, "无法访问的代码", true);
        }
        public static void Parse(Manager manager, LogicBlock logicBlock, AbstractDeclaration? declaration, AbstractCallable callable, List<TextLine> body)
        {
            var context = new Context(callable.file.space.document, callable.space, callable.file.space.relies, declaration);
            var localContext = new LocalContext(callable.file.space.collector, declaration);
            foreach (var parameter in callable.parameters)
                logicBlock.parameters.Add(localContext.Add(parameter.name, parameter.type, true));

            var parser = new LogicBlockParser(manager, callable.name, context, localContext, callable.returns, body, callable.file.space.collector, false, logicBlock);
            parser.Parse();
            localContext.CollectUnnecessary();
        }
        public static void Parse(Manager manager, TextRange name, LogicBlock logicBlock, AbstractDeclaration declaration, HashSet<AbstractSpace> relies, List<TextLine> body)
        {
            var context = new Context(declaration.file.space.document, declaration.space, relies, declaration);
            var localContext = new LocalContext(declaration.file.space.collector, declaration);
            var parser = new LogicBlockParser(manager, name, context, localContext, Tuple.Empty, body, declaration.file.space.collector, true, logicBlock);
            parser.Parse();
            localContext.CollectUnnecessary();
        }
        public static Expression Parse(Manager manager, AbstractClass declaration, List<AbstractCallable> callables, TextRange invokerRange, List<Local> locals, TextRange parameterRange, MessageCollector collector)
        {
            var context = new Context(declaration.file.space.document, declaration.space, declaration.file.space.relies, declaration);
            var localContext = new LocalContext(collector, declaration, locals);
            var parser = new ExpressionParser(manager, context, localContext, collector, false);
            var expression = parser.Parse(parameterRange);
            if (expression.Valid)
            {
                if (expression is BracketExpression bracket)
                {
                    if (parser.TryGetFunction(invokerRange, callables, expression, out var callable))
                    {
                        bracket = bracket.Replace(parser.AssignmentConvert(bracket, callable.signature));
                        return new InvokerMemberExpression(invokerRange & bracket.range, Tuple.Empty, localContext.Snapshoot, null, invokerRange, null, callable, bracket, manager.kernelManager);
                    }
                    else collector.Add(invokerRange, ErrorLevel.Error, "未找到匹配的构造函数");
                }
                else collector.Add(expression.range, ErrorLevel.Error, "无效的表达式");
            }
            return new InvalidExpression(localContext.Snapshoot, new InvalidKeyworldExpression(invokerRange, localContext.Snapshoot), expression);
        }
    }
}
