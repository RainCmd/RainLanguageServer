using RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis
{
    internal class ExpressionParser(Manager manager, Context context, LocalContext localContext, MessageCollector collector, bool destructor)
    {
        public readonly Manager manager = manager;
        public readonly Context context = context;
        public readonly LocalContext localContext = localContext;
        public readonly MessageCollector collector = collector;
        public readonly bool destructor = destructor;
        public Expression Parse(TextRange range)
        {
            range = range.Trim;
            if (range.Count == 0) return new TupleExpression(range, localContext.Snapshoot);
            if (TryParseBracket(range, out var result)) return result;
            if (TryParseTuple(SplitFlag.Semicolon, LexicalType.Semicolon, range, out result)) return result;
            var lexical = ExpressionSplit.Split(range, SplitFlag.Lambda | SplitFlag.Assignment | SplitFlag.Question, out var left, out var right, collector);
            if (lexical.type == LexicalType.Lambda) return ParseLambda(left, lexical.anchor, right);
            else if (lexical.type == LexicalType.Question) return ParseQuestion(left, lexical.anchor, right);
            else if (lexical.type != LexicalType.Unknow) return ParseAssignment(left, lexical, right);
            if (TryParseTuple(SplitFlag.Comma, LexicalType.Comma, range, out result)) return result;
            lexical = ExpressionSplit.Split(range, SplitFlag.QuestionNull, out left, out right, collector);
            if (lexical.type == LexicalType.QuestionNull) return ParseQuestionNull(left, lexical.anchor, right);
            return ParseExpression(range);
        }
        private Expression ParseExpression(TextRange range)
        {
            var expressionStack = new Stack<Expression>();
            var tokenStack = new Stack<Token>();
            var attribute = ExpressionAttribute.None;
            for (var index = range.start; Lexical.TryAnalysis(range, index, out var lexical, collector);)
            {
                switch (lexical.type)
                {
                    case LexicalType.Unknow: goto default;
                    case LexicalType.BracketLeft0:
                        {
                            var bracket = ParseBracket(lexical.anchor.start & range.end, lexical.anchor, SplitFlag.Bracket0);
                            index = bracket.range.end;
                            if (attribute.ContainAny(ExpressionAttribute.Method))
                            {
                                var expression = expressionStack.Pop();
                                if (expression is MethodExpression method)
                                {
                                    if (bracket.Valid)
                                    {
                                        if (TryGetFunction(method.name.name, method.callables, bracket, out var callable))
                                        {
                                            bracket = bracket.Replace(AssignmentConvert(bracket.expression, callable.signature));
                                            expression = new InvokerFunctionExpression(method.range & bracket.range, callable.returns, localContext.Snapshoot, method.qualifier, method.name, callable, bracket, manager.kernelManager);
                                            expressionStack.Push(expression);
                                            attribute = expression.attribute;
                                            goto label_next_lexical;
                                        }
                                        else collector.Add(method.name.name, ErrorLevel.Error, "未找到匹配的函数");
                                    }
                                }
                                else if (expression is MethodMemberExpression methodMember)
                                {
                                    if (bracket.Valid)
                                    {
                                        if (TryGetFunction(methodMember.member, methodMember.callables, bracket, out var callable))
                                        {
                                            bracket = bracket.Replace(AssignmentConvert(bracket.expression, callable.signature));
                                            if (methodMember is MethodVirtualExpression)
                                                expression = new InvokerVirtualExpression(methodMember.range & bracket.range, callable.returns, localContext.Snapshoot, methodMember.symbol, methodMember.member, methodMember.target, callable, bracket, manager.kernelManager);
                                            else
                                                expression = new InvokerMemberExpression(methodMember.range & bracket.range, callable.returns, localContext.Snapshoot, methodMember.symbol, methodMember.member, methodMember.target, callable, bracket, manager.kernelManager);
                                            expressionStack.Push(expression);
                                            attribute = expression.attribute;
                                            goto label_next_lexical;
                                        }
                                        else collector.Add(methodMember.member, ErrorLevel.Error, "未找到匹配的函数");
                                    }
                                }
                                else throw new Exception("未知的函数表达式：" + expression.GetType());
                                expressionStack.Push(new InvalidInvokerExpression(expression.range & bracket.range, localContext.Snapshoot, expression, bracket));
                                attribute = ExpressionAttribute.Invalid;
                            }
                            else if (attribute.ContainAny(ExpressionAttribute.Callable))
                            {
                                var expression = expressionStack.Pop();
                                if (!manager.TryGetDeclaration(expression.tuple[0], out var declaration)) throw new Exception("类型错误");
                                if (declaration is not AbstractDelegate abstractDelegate) throw new Exception("未知的可调用类型：" + declaration.GetType());
                                bracket = bracket.Replace(AssignmentConvert(bracket.expression, abstractDelegate.signature));
                                expression = new InvokerDelegateExpression(expression.range & bracket.range, abstractDelegate.returns, localContext.Snapshoot, expression, bracket, manager.kernelManager);
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else if (attribute.ContainAny(ExpressionAttribute.Type))
                            {
                                var expression = expressionStack.Pop();
                                if (expression is not TypeExpression type) throw new Exception("未知的类型表达式");
                                if (type.type == manager.kernelManager.REAL2)
                                {
                                    if (bracket.tuple.Count > 0)
                                        bracket = bracket.Replace(ConvertVectorParameter(bracket.expression, 2));
                                    expression = new VectorConstructorExpression(expression.range & bracket.range, type, localContext.Snapshoot, bracket);
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                }
                                else if (type.type == manager.kernelManager.REAL3)
                                {
                                    if (bracket.tuple.Count > 0)
                                        bracket = bracket.Replace(ConvertVectorParameter(bracket.expression, 3));
                                    expression = new VectorConstructorExpression(expression.range & bracket.range, type, localContext.Snapshoot, bracket);
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                }
                                else if (type.type == manager.kernelManager.REAL4)
                                {
                                    if (bracket.tuple.Count > 0)
                                        bracket = bracket.Replace(ConvertVectorParameter(bracket.expression, 4));
                                    expression = new VectorConstructorExpression(expression.range & bracket.range, type, localContext.Snapshoot, bracket);
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                }
                                else if (type.type.dimension == 0)
                                {
                                    if (type.type.code == TypeCode.Struct)
                                    {
                                        if (!bracket.Valid || bracket.tuple.Count == 0)
                                        {
                                            expression = new ConstructorExpression(expression.range & bracket.range, type, localContext.Snapshoot, null, null, bracket, manager.kernelManager);
                                            expressionStack.Push(expression);
                                            attribute = expression.attribute;
                                        }
                                        else
                                        {
                                            if (!manager.TryGetDeclaration(type.type, out var declaration)) throw new Exception("无效的类型");
                                            if (declaration is not AbstractStruct abstractStruct) throw new Exception("声明不是结构体：" + declaration.GetType());
                                            var members = new List<Type>();
                                            foreach (var member in abstractStruct.variables) members.Add(member.type);
                                            bracket = bracket.Replace(AssignmentConvert(bracket.expression, new TypeSpan(members)));
                                            expression = new ConstructorExpression(expression.range & bracket.range, type, localContext.Snapshoot, null, null, bracket, manager.kernelManager);
                                            expressionStack.Push(expression);
                                            attribute = expression.attribute;
                                        }
                                    }
                                    else if (type.type.code == TypeCode.Handle)
                                    {
                                        if (!manager.TryGetDeclaration(type.type, out var declaration)) throw new Exception("无效的类型");
                                        if (declaration is not AbstractClass abstractClass) throw new Exception("声明不是结构体：" + declaration.GetType());
                                        var constructors = new List<AbstractCallable>();
                                        foreach (var constructor in abstractClass.constructors)
                                            if (context.IsVisiable(manager, constructor.declaration))
                                                constructors.Add(constructor);
                                        if (TryGetFunction(expression.range, constructors, bracket, out var callable))
                                        {
                                            bracket = bracket.Replace(AssignmentConvert(bracket.expression, callable.signature));
                                            if (destructor) collector.Add(expression.range, ErrorLevel.Error, "析构函数中不能创建托管对象");
                                            expression = new ConstructorExpression(expression.range & bracket.range, type, localContext.Snapshoot, callable, null, bracket, manager.kernelManager);
                                        }
                                        else
                                        {
                                            constructors.Clear();
                                            if (destructor) collector.Add(expression.range, ErrorLevel.Error, "析构函数中不能创建托管对象");
                                            if (abstractClass.constructors.Count > 0 && bracket.tuple.Count > 0)
                                                collector.Add(expression.range, ErrorLevel.Error, "未找到匹配的构造函数");
                                            foreach (var constructor in abstractClass.constructors) constructors.Add(constructor);
                                            expression = new ConstructorExpression(expression.range & bracket.range, type, localContext.Snapshoot, null, constructors, bracket, manager.kernelManager);
                                        }
                                        expressionStack.Push(expression);
                                        attribute = expression.attribute;
                                    }
                                    else
                                    {
                                        collector.Add(type.range, ErrorLevel.Error, "无效的操作");
                                        expressionStack.Push(expression.ToInvalid());
                                        attribute = ExpressionAttribute.Invalid;
                                    }
                                }
                                else
                                {
                                    collector.Add(type.range, ErrorLevel.Error, "数组没有构造函数");
                                    expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expression, bracket));
                                    attribute = ExpressionAttribute.Invalid;
                                }
                            }
                            else if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(bracket);
                                attribute = bracket.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "无效的操作", bracket);
                                attribute = ExpressionAttribute.Invalid;
                            }
                            goto label_next_lexical;
                        }
                    case LexicalType.BracketLeft1:
                        {
                            var bracket = ParseBracket(lexical.anchor.start & range.end, lexical.anchor, SplitFlag.Bracket1);
                            index = bracket.range.end;
                            if (attribute.ContainAll(ExpressionAttribute.Value | ExpressionAttribute.Array))
                            {
                                if (bracket.Valid)
                                {
                                    if (!IsIndies(bracket.tuple))
                                    {
                                        var list = new List<Type>();
                                        foreach (var _ in bracket.tuple) list.Add(manager.kernelManager.INT);
                                        bracket = bracket.Replace(AssignmentConvert(bracket.expression, new TypeSpan(list)));
                                    }
                                    var expression = expressionStack.Pop();
                                    if (bracket.tuple.Count == 1)
                                    {
                                        if (expression.tuple[0] == manager.kernelManager.STRING)
                                            expression = new StringEvaluationExpression(expression.range & bracket.range, localContext.Snapshoot, expression, bracket, manager.kernelManager);
                                        else expression = new ArrayEvaluationExpression(expression.range & bracket.range, localContext.Snapshoot, expression, bracket, ExpressionAttribute.Value | ExpressionAttribute.Assignable, manager.kernelManager);
                                        expressionStack.Push(expression);
                                        attribute = expression.attribute;
                                    }
                                    else if (bracket.tuple.Count == 2)
                                    {
                                        expression = new ArraySubExpression(expression.range & bracket.range, localContext.Snapshoot, expression, bracket, manager.kernelManager);
                                        expressionStack.Push(expression);
                                        attribute = expression.attribute;
                                    }
                                    else
                                    {
                                        collector.Add(lexical.anchor, ErrorLevel.Error, "无效的操作");
                                        expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expression, bracket));
                                        attribute = ExpressionAttribute.Invalid;
                                    }
                                }
                                else
                                {
                                    expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expressionStack.Pop(), bracket));
                                    attribute = ExpressionAttribute.Invalid;
                                }
                            }
                            else if (attribute.ContainAny(ExpressionAttribute.Tuple))
                            {
                                if (bracket.Valid)
                                {
                                    if (!IsIndies(bracket.tuple))
                                    {
                                        var list = new List<Type>();
                                        foreach (var _ in bracket.tuple) list.Add(manager.kernelManager.INT);
                                        bracket = bracket.Replace(AssignmentConvert(bracket.expression, new TypeSpan(list)));
                                    }
                                    var indices = new List<long>();
                                    if (bracket.TryEvaluateIndices(indices))
                                    {
                                        if (indices.Count > 0)
                                        {
                                            var expression = expressionStack.Pop();
                                            var tuple = new Type[indices.Count];
                                            var error = false;
                                            for (var i = 0; i < indices.Count; i++)
                                            {
                                                if (indices[i] < 0) indices[i] += expression.tuple.Count;
                                                if (indices[i] >= 0 && indices[i] < expression.tuple.Count) tuple[i] = expression.tuple[(int)indices[i]];
                                                else
                                                {
                                                    collector.Add(bracket.range, ErrorLevel.Error, $"第{i + 1}个索引超出了元组的类型数量范围");
                                                    error = true;
                                                }
                                            }
                                            if (error)
                                            {
                                                expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expression, bracket));
                                                attribute = ExpressionAttribute.Invalid;
                                            }
                                            else
                                            {
                                                expression = new TupleEvaluationExpression(expression.range & bracket.range, tuple, localContext.Snapshoot, expression, bracket, manager.kernelManager);
                                                expressionStack.Push(expression);
                                                attribute = expression.attribute;
                                            }
                                            goto label_next_lexical;
                                        }
                                        else collector.Add(bracket.range, ErrorLevel.Error, "缺少索引");
                                    }
                                    else collector.Add(bracket.range, ErrorLevel.Error, "元组的索引必须是整数常量");
                                }
                                expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expressionStack.Pop(), bracket));
                                attribute = ExpressionAttribute.Invalid;
                            }
                            else if (attribute.ContainAny(ExpressionAttribute.Task))
                            {
                                if (bracket.Valid)
                                {
                                    if (!IsIndies(bracket.tuple))
                                    {
                                        var list = new List<Type>();
                                        foreach (var _ in bracket.tuple) list.Add(manager.kernelManager.INT);
                                        bracket = bracket.Replace(AssignmentConvert(bracket.expression, new TypeSpan(list)));
                                    }
                                    var indices = new List<long>();
                                    if (bracket.TryEvaluateIndices(indices))
                                    {
                                        var expression = expressionStack.Pop();
                                        if (!manager.TryGetDeclaration(expression.tuple[0], out var declaration)) throw new Exception("类型错误");
                                        if (declaration is not AbstractTask abstractTask) throw new Exception("不是任务类型");
                                        if (indices.Count > 0)
                                        {
                                            var tuple = new Type[indices.Count];
                                            var error = false;
                                            for (var i = 0; i < indices.Count; i++)
                                            {
                                                if (indices[i] < 0) indices[i] += abstractTask.returns.Count;
                                                if (indices[i] >= 0 && indices[i] < abstractTask.returns.Count) tuple[i] = abstractTask.returns[(int)indices[i]];
                                                else
                                                {
                                                    collector.Add(bracket.range, ErrorLevel.Error, $"第{i + 1}个索引超出了任务的值类型数量范围");
                                                    error = true;
                                                }
                                            }
                                            if (error)
                                            {
                                                expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expression, bracket));
                                                attribute = ExpressionAttribute.Invalid;
                                            }
                                            else
                                            {
                                                expression = new TaskEvaluationExpression(expression.range & bracket.range, tuple, localContext.Snapshoot, expression, bracket, manager.kernelManager);
                                                expressionStack.Push(expression);
                                                attribute = expression.attribute;
                                            }
                                        }
                                        else
                                        {
                                            expression = new TaskEvaluationExpression(expression.range & bracket.range, abstractTask.returns, localContext.Snapshoot, expression, bracket, manager.kernelManager);
                                            expressionStack.Push(expression);
                                            attribute = expression.attribute;
                                        }
                                        goto label_next_lexical;
                                    }
                                    else collector.Add(bracket.range, ErrorLevel.Error, "任务的求值索引必须是整数常量");
                                }
                                expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expressionStack.Pop(), bracket));
                                attribute = ExpressionAttribute.Invalid;
                            }
                            else if (attribute.ContainAny(ExpressionAttribute.Type))
                            {
                                if (bracket.Valid)
                                {
                                    if (bracket.tuple.Count == 1)
                                    {
                                        if (bracket.tuple[0] != manager.kernelManager.INT)
                                            bracket = bracket.Replace(AssignmentConvert(bracket.expression, new TypeSpan([manager.kernelManager.INT])));
                                        var type = (TypeExpression)expressionStack.Pop();
                                        if (destructor) collector.Add(type.range, ErrorLevel.Error, "析构函数中不能创建托管对象");
                                        var expression = new ArrayCreateExpression(type.range & bracket.range, new Type(type.type, type.type.dimension + 1), localContext.Snapshoot, type, bracket);
                                        expressionStack.Push(expression);
                                        attribute = expression.attribute;
                                        goto label_next_lexical;
                                    }
                                    else collector.Add(bracket.range, ErrorLevel.Error, "只支持一维数组");
                                }
                                expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expressionStack.Pop(), bracket));
                                attribute = ExpressionAttribute.Invalid;
                            }
                            else if (attribute.ContainAny(ExpressionAttribute.Value))
                            {
                                if (bracket.Valid)
                                {
                                    var expression = expressionStack.Pop();
                                    if (expression.tuple[0].dimension == 0 && expression.tuple[0].code == TypeCode.Struct)
                                    {
                                        if (!IsIndies(bracket.tuple))
                                        {
                                            var list = new List<Type>();
                                            foreach (var _ in bracket.tuple) list.Add(manager.kernelManager.INT);
                                            bracket = bracket.Replace(AssignmentConvert(bracket.expression, new TypeSpan(list)));
                                        }
                                        var indices = new List<long>();
                                        if (bracket.TryEvaluateIndices(indices))
                                        {
                                            if (!manager.TryGetDeclaration(expression.tuple[0], out var declaration)) throw new Exception("无效的类型");
                                            if (declaration is not AbstractStruct abstractStruct) throw new Exception("不是结构体类型:" + declaration.GetType());
                                            if (indices.Count == 0)
                                                for (var i = 0; i < abstractStruct.variables.Count; i++)
                                                    indices.Add(i);
                                            var tuple = new Type[indices.Count];
                                            var error = false;
                                            for (var i = 0; i < indices.Count; i++)
                                            {
                                                if (indices[i] < 0) indices[i] += abstractStruct.variables.Count;
                                                if (indices[i] >= 0 && indices[i] < abstractStruct.variables.Count) tuple[i] = abstractStruct.variables[(int)indices[i]].type;
                                                else
                                                {
                                                    collector.Add(bracket.range, ErrorLevel.Error, $"第{i + 1}个索引超出了结构体成员字段的数量范围");
                                                    error = true;
                                                }
                                            }
                                            if (error)
                                            {
                                                expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expression, bracket));
                                                attribute = ExpressionAttribute.Invalid;
                                            }
                                            else
                                            {
                                                expression = new TupleEvaluationExpression(expression.range & bracket.range, tuple, localContext.Snapshoot, expression, bracket, manager.kernelManager);
                                                expressionStack.Push(expression);
                                                attribute = expression.attribute;
                                            }
                                            goto label_next_lexical;
                                        }
                                        else collector.Add(bracket.range, ErrorLevel.Error, "结构体解构索引必须是整数常量");
                                    }
                                    else collector.Add(expression.range, ErrorLevel.Error, "只能对结构体进行解构操作");
                                    expressionStack.Push(expression);
                                }
                                expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expressionStack.Pop(), bracket));
                                attribute = ExpressionAttribute.Invalid;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "无效的操作", bracket);
                                attribute = ExpressionAttribute.Invalid;
                            }
                            goto label_next_lexical;
                        }
                    case LexicalType.BracketLeft2:
                        {
                            var bracket = ParseBracket(lexical.anchor.start & range.end, lexical.anchor, SplitFlag.Bracket2);
                            index = bracket.range.end;
                            if (attribute.ContainAny(ExpressionAttribute.Type))
                            {
                                var type = (TypeExpression)expressionStack.Pop();
                                if (bracket.Valid)
                                {
                                    var elementTypes = new Type[bracket.tuple.Count];
                                    for (var i = 0; i < elementTypes.Length; i++) elementTypes[i] = type.type;
                                    if (bracket.tuple != elementTypes)
                                        bracket = bracket.Replace(AssignmentConvert(bracket.expression, elementTypes));
                                }
                                var expression = new ArrayInitExpression(type.range & bracket.range, new Type(type.type, type.type.dimension + 1), localContext.Snapshoot, type, bracket);
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                var expression = new BlurrySetExpression(bracket, localContext.Snapshoot);
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "无效的操作", bracket);
                                attribute = ExpressionAttribute.Invalid;
                            }
                            goto label_next_lexical;
                        }
                    case LexicalType.BracketRight0:
                    case LexicalType.BracketRight1:
                    case LexicalType.BracketRight2:
                    case LexicalType.Comma:
                    case LexicalType.Semicolon:
                    case LexicalType.Assignment: goto default;
                    case LexicalType.Equals:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Equals), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.Lambda: goto default;
                    case LexicalType.BitAnd:
                        if (attribute.ContainAny(ExpressionAttribute.Type)) PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Casting), attribute);
                        else PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.BitAnd), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.LogicAnd:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.LogicAnd), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.BitAndAssignment: goto default;
                    case LexicalType.BitOr:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.BitOr), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.LogicOr:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.LogicOr), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.BitOrAssignment: goto default;
                    case LexicalType.BitXor:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.BitXor), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.BitXorAssignment: goto default;
                    case LexicalType.Less:
                        if (attribute.ContainAny(ExpressionAttribute.Value))
                        {
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Less), attribute);
                            attribute = ExpressionAttribute.Operator;
                        }
                        else
                        {
                            var left = lexical.anchor;
                            if (Lexical.TryExtractName(range, left.end, out var names, collector))
                            {
                                var name = new QualifiedName(names);
                                index = name.name.end;
                                var dimension = Lexical.ExtractDimension(range, ref index);
                                var file = new FileType(name.Range.start & index, name, dimension);
                                var type = FileLink.GetType(context, manager, file, collector);
                                if (Lexical.TryAnalysis(range, index, out lexical, collector) && lexical.type == LexicalType.Greater)
                                {
                                    index = lexical.anchor.end;
                                    var expression = new ConstTypeExpression(left & lexical.anchor, localContext.Snapshoot, left, lexical.anchor, file, type, manager.kernelManager);
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                }
                                else
                                {
                                    var msg = new Message(index & index, ErrorLevel.Error, "缺少配对的符号");
                                    msg.related.Add(new RelatedInfo(left, "缺少配对的符号"));
                                    collector.Add(msg);
                                    var expression = new ConstTypeExpression(left.start & index, localContext.Snapshoot, left, index & index, file, type, manager.kernelManager);
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                }
                                goto label_next_lexical;
                            }
                            else
                            {
                                PushInvalidOperationExpression(expressionStack, left, attribute, "无效的操作");
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        break;
                    case LexicalType.LessEquals:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.LessEquals), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.ShiftLeft:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.ShiftLeft), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.ShiftLeftAssignment: goto default;
                    case LexicalType.Greater:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Greater), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.GreaterEquals:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.GreaterEquals), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.ShiftRight:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.ShiftRight), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.ShiftRightAssignment: goto default;
                    case LexicalType.Plus:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Plus), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.Increment:
                        if (attribute.ContainAny(ExpressionAttribute.Value))
                        {
                            var expression = expressionStack.Pop();
                            expression = CreateOperation(expression.range & lexical.anchor, "++", lexical.anchor, expression);
                            expressionStack.Push(expression);
                            attribute = expression.attribute;
                        }
                        else
                        {
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.IncrementLeft), attribute);
                            attribute = ExpressionAttribute.Operator;
                        }
                        break;
                    case LexicalType.PlusAssignment: goto default;
                    case LexicalType.Minus:
                        if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator)) PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Negative), attribute);
                        else PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Minus), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.Decrement:
                        if (attribute.ContainAny(ExpressionAttribute.Value))
                        {
                            var expression = expressionStack.Pop();
                            expression = CreateOperation(expression.range & lexical.anchor, "--", lexical.anchor, expression);
                            expressionStack.Push(expression);
                            attribute = expression.attribute;
                        }
                        else
                        {
                            PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.DecrementLeft), attribute);
                            attribute = ExpressionAttribute.Operator;
                        }
                        break;
                    case LexicalType.RealInvoker:
                        if (attribute.ContainAny(ExpressionAttribute.Value))
                        {
                            if (Lexical.TryAnalysis(range, lexical.anchor.end, out var identifier, collector))
                            {
                                index = identifier.anchor.end;
                                var expression = expressionStack.Pop();
                                if (identifier.type == LexicalType.Word)
                                {
                                    var type = expression.tuple[0];
                                    if (type.code == TypeCode.Handle || type.dimension > 0)
                                    {
                                        if (context.TryFindMember(manager, identifier.anchor, type, out List<AbstractClass.Function> functions))
                                        {
                                            expression = new MethodMemberExpression(expression.range & identifier.anchor, localContext.Snapshoot, lexical.anchor, identifier.anchor, expression, [.. functions]);
                                            expressionStack.Push(expression);
                                            attribute = expression.attribute;
                                            goto label_next_lexical;
                                        }
                                        else collector.Add(identifier.anchor, ErrorLevel.Error, $"未找到该成员函数");
                                    }
                                    else collector.Add(lexical.anchor, ErrorLevel.Error, "只有class才可以使用实调用");
                                }
                                else collector.Add(identifier.anchor, ErrorLevel.Error, "无效的标识符");
                                expressionStack.Push(new InvalidOperationExpression(expression.range & identifier.anchor, localContext.Snapshoot, lexical.anchor, expression));
                                attribute = ExpressionAttribute.Invalid;
                                goto label_next_lexical;
                            }
                            else
                            {
                                collector.Add(lexical.anchor, ErrorLevel.Error, "缺少标识符");
                                var expression = expressionStack.Pop();
                                expressionStack.Push(new InvalidOperationExpression(expression.range & lexical.anchor, localContext.Snapshoot, lexical.anchor, expression));
                            }
                        }
                        else PushInvalidOperationExpression(expressionStack, lexical.anchor, attribute, "无效的操作");
                        attribute = ExpressionAttribute.Invalid;
                        break;
                    case LexicalType.MinusAssignment: goto default;
                    case LexicalType.Mul:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Mul), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.MulAssignment: goto default;
                    case LexicalType.Div:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Div), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.DivAssignment:
                    case LexicalType.Annotation: goto default;
                    case LexicalType.Mod:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Mod), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.ModAssignment: goto default;
                    case LexicalType.Not:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Not), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.NotEquals:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.NotEquals), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.Negate:
                        PushToken(expressionStack, tokenStack, new Token(lexical, TokenType.Inverse), attribute);
                        attribute = ExpressionAttribute.Operator;
                        break;
                    case LexicalType.Dot:
                        if (attribute.ContainAny(ExpressionAttribute.Type))
                        {
                            var type = (TypeExpression)expressionStack.Pop();
                            if (type.type.code == TypeCode.Enum)
                            {
                                if (Lexical.TryAnalysis(range, lexical.anchor.end, out var identifier, collector))
                                {
                                    index = identifier.anchor.end;
                                    if (!manager.TryGetDeclaration(type.type, out var declaration)) throw new Exception("类型错误");
                                    if (declaration is not AbstractEnum abstractEnum) throw new Exception($"{declaration.GetType()}无法转换为{typeof(AbstractEnum)}");
                                    var elementName = identifier.anchor.ToString();
                                    foreach (var element in abstractEnum.elements)
                                        if (element.name == elementName)
                                        {
                                            var expression = new EnumElementExpression(type.range & identifier.anchor, localContext.Snapshoot, lexical.anchor, identifier.anchor, abstractEnum, element, type);
                                            expressionStack.Push(expression);
                                            attribute = expression.attribute;
                                            goto label_next_lexical;
                                        }
                                    collector.Add(identifier.anchor, ErrorLevel.Error, "枚举值不存在");
                                    expressionStack.Push(new InvalidOperationExpression(type.range & identifier.anchor, localContext.Snapshoot, lexical.anchor, type));
                                    attribute = ExpressionAttribute.Invalid;
                                    goto label_next_lexical;
                                }
                                else
                                {
                                    collector.Add(lexical.anchor, ErrorLevel.Error, "应输入标识符");
                                    expressionStack.Push(new InvalidOperationExpression(type.range & lexical.anchor, localContext.Snapshoot, lexical.anchor, type));
                                }
                            }
                            else
                            {
                                collector.Add(lexical.anchor, ErrorLevel.Error, "无效的操作");
                                expressionStack.Push(new InvalidOperationExpression(type.range & lexical.anchor, localContext.Snapshoot, lexical.anchor, type));
                            }
                        }
                        else if (attribute.ContainAny(ExpressionAttribute.Value))
                        {
                            var expression = expressionStack.Pop();
                            if (Lexical.TryAnalysis(range, lexical.anchor.end, out var identifier, collector))
                            {
                                index = identifier.anchor.end;
                                if (context.TryFindMember<AbstractDeclaration>(manager, identifier.anchor, expression.tuple[0], out var members))
                                {
                                    if (members[0] is AbstractStruct.Variable structVariable)
                                        expression = new VariableMemberExpression(expression.range & identifier.anchor, structVariable.type, localContext.Snapshoot, lexical.anchor, identifier.anchor, expression, members[0], manager.kernelManager);
                                    else if (members[0] is AbstractClass.Variable classVariable)
                                        expression = new VariableMemberExpression(expression.range & identifier.anchor, classVariable.type, localContext.Snapshoot, lexical.anchor, identifier.anchor, expression, members[0], manager.kernelManager);
                                    else if (members[0] is AbstractStruct.Function || members[0] is AbstractClass.Function || members[0] is AbstractInterface.Function)
                                        expression = new MethodVirtualExpression(expression.range & identifier.anchor, localContext.Snapshoot, lexical.anchor, identifier.anchor, expression, members.Select<AbstractDeclaration, AbstractCallable>());
                                    else throw new Exception("未知的成员类型" + members[0].GetType());
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                    goto label_next_lexical;
                                }
                                else if (expression.tuple[0] == manager.kernelManager.REAL2)
                                {
                                    attribute = ParseVectorMember(expressionStack, expression, lexical.anchor, identifier.anchor, 2);
                                    goto label_next_lexical;
                                }
                                else if (expression.tuple[0] == manager.kernelManager.REAL3)
                                {
                                    attribute = ParseVectorMember(expressionStack, expression, lexical.anchor, identifier.anchor, 3);
                                    goto label_next_lexical;
                                }
                                else if (expression.tuple[0] == manager.kernelManager.REAL4)
                                {
                                    attribute = ParseVectorMember(expressionStack, expression, lexical.anchor, identifier.anchor, 4);
                                    goto label_next_lexical;
                                }
                                else
                                {
                                    collector.Add(identifier.anchor, ErrorLevel.Error, "没有找到该成员");
                                    expressionStack.Push(new InvalidOperationExpression(expression.range & identifier.anchor, localContext.Snapshoot, lexical.anchor, expression));
                                }
                                attribute = ExpressionAttribute.Invalid;
                                goto label_next_lexical;
                            }
                            else
                            {
                                collector.Add(lexical.anchor, ErrorLevel.Error, "应输入标识符");
                                expressionStack.Push(new InvalidOperationExpression(expression.range & lexical.anchor, localContext.Snapshoot, lexical.anchor, expression));
                            }
                        }
                        else PushInvalidOperationExpression(expressionStack, lexical.anchor, attribute, "无效的操作");
                        attribute = ExpressionAttribute.Invalid;
                        break;
                    case LexicalType.Question: goto default;
                    case LexicalType.QuestionDot:
                        {
                            if (attribute.ContainAny(ExpressionAttribute.Value))
                            {
                                if (expressionStack.Peek().tuple[0].Managed || expressionStack.Peek().tuple[0] == manager.kernelManager.ENTITY) goto case LexicalType.Dot;
                                else PushInvalidOperationExpression(expressionStack, lexical.anchor, attribute, "可为空的类型才能使用此操作符");
                            }
                            else PushInvalidOperationExpression(expressionStack, lexical.anchor, attribute, "无效的操作");
                            attribute = ExpressionAttribute.Invalid;
                        }
                        break;
                    case LexicalType.QuestionRealInvoke:
                        {
                            if (attribute.ContainAny(ExpressionAttribute.Value))
                            {
                                if (expressionStack.Peek().tuple[0].Managed || expressionStack.Peek().tuple[0] == manager.kernelManager.ENTITY) goto case LexicalType.RealInvoker;
                                else PushInvalidOperationExpression(expressionStack, lexical.anchor, attribute, "可为空的类型才能使用此操作符");
                            }
                            else PushInvalidOperationExpression(expressionStack, lexical.anchor, attribute, "无效的操作");
                            attribute = ExpressionAttribute.Invalid;
                        }
                        break;
                    case LexicalType.QuestionInvoke:
                        {
                            var bracket = ParseBracket(lexical.anchor.start & range.end, lexical.anchor, SplitFlag.Bracket0);
                            index = bracket.range.end;
                            if (attribute.ContainAll(ExpressionAttribute.Value | ExpressionAttribute.Callable))
                            {
                                var expression = expressionStack.Pop();
                                if (!manager.TryGetDeclaration(expression.tuple[0], out var declaration)) throw new Exception("类型错误");
                                if (declaration is not AbstractDelegate abstractDelegate) throw new Exception("未知的可调用类型：" + declaration.GetType());
                                bracket = bracket.Replace(AssignmentConvert(bracket.expression, abstractDelegate.signature));
                                expression = new InvokerDelegateExpression(expression.range & bracket.range, abstractDelegate.returns, localContext.Snapshoot, expression, bracket, manager.kernelManager);
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "无效的操作", bracket);
                                attribute = ExpressionAttribute.Invalid;
                            }
                            goto label_next_lexical;
                        }
                    case LexicalType.QuestionIndex:
                        {
                            var bracket = ParseBracket(lexical.anchor.start & range.end, lexical.anchor, SplitFlag.Bracket1);
                            index = bracket.range.end;
                            if (attribute.ContainAll(ExpressionAttribute.Value | ExpressionAttribute.Array))
                            {
                                if (bracket.Valid)
                                {
                                    if (!IsIndies(bracket.tuple))
                                    {
                                        var list = new List<Type>();
                                        foreach (var _ in bracket.tuple) list.Add(manager.kernelManager.INT);
                                        bracket = bracket.Replace(AssignmentConvert(bracket.expression, new TypeSpan(list)));
                                    }
                                    var expression = expressionStack.Pop();
                                    if (bracket.tuple.Count == 1)
                                    {
                                        if (expression.tuple[0] == manager.kernelManager.STRING)
                                        {
                                            collector.Add(expression.range, ErrorLevel.Error, "字符串不是可为空的类型");
                                            expression = new StringEvaluationExpression(expression.range & bracket.range, localContext.Snapshoot, expression, bracket, manager.kernelManager);
                                        }
                                        else expression = new ArrayEvaluationExpression(expression.range & bracket.range, localContext.Snapshoot, expression, bracket, ExpressionAttribute.Value, manager.kernelManager);
                                        expressionStack.Push(expression);
                                        attribute = expression.attribute;
                                    }
                                    else if (bracket.tuple.Count == 2)
                                    {
                                        expression = new ArraySubExpression(expression.range & bracket.range, localContext.Snapshoot, expression, bracket, manager.kernelManager);
                                        expressionStack.Push(expression);
                                        attribute = expression.attribute;
                                    }
                                    else
                                    {
                                        collector.Add(lexical.anchor, ErrorLevel.Error, "无效的操作");
                                        expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expression, bracket));
                                        attribute = ExpressionAttribute.Invalid;
                                    }
                                }
                                else
                                {
                                    expressionStack.Push(new InvalidExpression(localContext.Snapshoot, expressionStack.Pop(), bracket));
                                    attribute = ExpressionAttribute.Invalid;
                                }
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "无效的操作", bracket);
                                attribute = ExpressionAttribute.Invalid;
                            }
                            goto label_next_lexical;
                        }
                    case LexicalType.QuestionNull:
                    case LexicalType.Colon: goto default;
                    case LexicalType.ConstReal:
                        {
                            var value = double.Parse(lexical.anchor.ToString().Replace("_", ""));
                            var expression = new ConstRealExpression(lexical.anchor, localContext.Snapshoot, value, manager.kernelManager);
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        break;
                    case LexicalType.ConstNumber:
                        {
                            var value = long.Parse(lexical.anchor.ToString().Replace("_", ""));
                            var expression = new ConstIntegerExpression(lexical.anchor, localContext.Snapshoot, value, false, manager.kernelManager);
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        break;
                    case LexicalType.ConstBinary:
                        {
                            long value = 0;
                            for (var i = 2; i < lexical.anchor.Count; i++)
                            {
                                var c = lexical.anchor[i];
                                if (c != '_')
                                {
                                    value <<= 1;
                                    if (c == '1') value++;
                                }
                            }
                            var expression = new ConstIntegerExpression(lexical.anchor, localContext.Snapshoot, value, true, manager.kernelManager);
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        break;
                    case LexicalType.ConstHexadecimal:
                        {
                            long value = 0;
                            for (var i = 2; i < lexical.anchor.Count; i++)
                            {
                                var c = lexical.anchor[i];
                                if (c != '_')
                                {
                                    value <<= 4;
                                    if (c.TryToHexNumber(out var number)) value += number;
                                    else throw new Exception($"{c}不是16进制字符");
                                }
                            }
                            var expression = new ConstIntegerExpression(lexical.anchor, localContext.Snapshoot, value, true, manager.kernelManager);
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        break;
                    case LexicalType.ConstChars:
                        {
                            long value = 0;
                            var anchor = lexical.anchor;
                            if (anchor[^1] == '\'') anchor = anchor[1..^1];
                            else anchor = anchor[1..];
                            var count = 0;
                            for (var i = 0; i < anchor.Count; i++, count++)
                            {
                                var c = anchor[i] & 0xff;
                                value <<= 8;
                                if (c == '\\') value += Utility.EscapeCharacter(anchor, ref i);
                                else value += c;
                            }
                            Expression expression;
                            if (count == 1) expression = new ConstCharExpression(lexical.anchor, localContext.Snapshoot, (char)value, manager.kernelManager);
                            else expression = new ConstCharsExpression(lexical.anchor, localContext.Snapshoot, value, manager.kernelManager);
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        break;
                    case LexicalType.ConstString:
                        {
                            var expression = new ConstStringExpression(lexical.anchor, localContext.Snapshoot, manager.kernelManager);
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        break;
                    case LexicalType.TemplateString:
                        {
                            var expressions = new List<Expression>();
                            var anchor = lexical.anchor;
                            if (anchor[^1] == '\"') anchor = anchor[2..^1];
                            else anchor = anchor[2..];
                            var start = lexical.anchor.start;
                            for (var i = 0; i < anchor.Count; i++)
                            {
                                var c = anchor[i];
                                if (c == '{')
                                {
                                    if (anchor[i + 1] == '{') i++;
                                    else
                                    {
                                        var position = anchor.start + i;
                                        if (position > start)
                                            expressions.Add(new ConstStringExpression(start & position, localContext.Snapshoot, manager.kernelManager));

                                        var bracket = ParseBracket(anchor, position & (position + 1), SplitFlag.Bracket2);
                                        if (!bracket.expression.attribute.ContainAny(ExpressionAttribute.Value))
                                            collector.Add(bracket.expression.range, ErrorLevel.Error, "内插字符串内的表达式必须是返回单个值");
                                        expressions.Add(bracket);

                                        i = bracket.range.end - anchor.start - 1;
                                        start = bracket.range.end;
                                    }
                                }
                                else if (c == '}')
                                {
                                    if (i + 1 < anchor.Count && anchor[i + 1] != '}')
                                    {
                                        var position = anchor.start + i;
                                        collector.Add(position & position + 1, ErrorLevel.Error, "缺少配对的符号");
                                    }
                                }
                                else if (c == '\\') i++;
                            }
                            if (start < lexical.anchor.end)
                                expressions.Add(new ConstStringExpression(start & lexical.anchor.end, localContext.Snapshoot, manager.kernelManager));
                            var expression = new ComplexStringExpression(lexical.anchor, localContext.Snapshoot, expressions, manager.kernelManager);
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        break;
                    case LexicalType.Word:
                        if (lexical.anchor == KeyWords.GLOBAL)
                        {
                            if (Lexical.TryAnalysis(range, lexical.anchor.end, out var identifier, collector))
                            {
                                if (identifier.type == LexicalType.Word)
                                {
                                    index = lexical.anchor.end;
                                    var globalContext = new Context(context, null);
                                    if (TryFindDeclaration(range, ref index, expressionStack, attribute, out var results, out var name, globalContext))
                                        PushDeclarationsExpression(range, ref index, ref attribute, expressionStack, results, lexical.anchor, name);
                                    else attribute = ExpressionAttribute.Invalid;
                                    goto label_next_lexical;
                                }
                                else PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入标识符", new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot));
                            }
                            else PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入标识符", new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot));
                            attribute = ExpressionAttribute.Invalid;
                        }
                        else if (lexical.anchor == KeyWords.BASE)
                        {
                            if (context.declaration == null || context.declaration is not AbstractClass abstractClass)
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "当前上下文中不可用", new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot));
                                attribute = ExpressionAttribute.Invalid;
                            }
                            else
                            {
                                var type = abstractClass.parent.code == TypeCode.Invalid ? manager.kernelManager.HANDLE : abstractClass.parent;
                                var expression = new VariableKeyworldLocalExpression(lexical.anchor, localContext.thisValue!.Value, type, localContext.Snapshoot, lexical.anchor, ExpressionAttribute.Value, manager.kernelManager);
                                if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                                {
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                }
                                else
                                {
                                    PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                    attribute = ExpressionAttribute.Invalid;
                                }
                            }
                        }
                        else if (lexical.anchor == KeyWords.THIS)
                        {
                            if (context.declaration == null)
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "当前上下文中不可用", new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot));
                                attribute = ExpressionAttribute.Invalid;
                            }
                            else
                            {
                                var expression = new VariableKeyworldLocalExpression(lexical.anchor, localContext.thisValue!.Value, localContext.thisValue.Value.type, localContext.Snapshoot, lexical.anchor, ExpressionAttribute.Value, manager.kernelManager);
                                if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                                {
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                }
                                else
                                {
                                    PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                    attribute = ExpressionAttribute.Invalid;
                                }
                            }
                        }
                        else if (lexical.anchor == KeyWords.TRUE)
                        {
                            var expression = new ConstBooleanKeyworldExpression(lexical.anchor, localContext.Snapshoot, true, manager.kernelManager);
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        else if (lexical.anchor == KeyWords.FALSE)
                        {
                            var expression = new ConstBooleanKeyworldExpression(lexical.anchor, localContext.Snapshoot, false, manager.kernelManager);
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        else if (lexical.anchor == KeyWords.NULL)
                        {
                            var expression = new ConstNullExpression(lexical.anchor, localContext.Snapshoot);
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        else if (lexical.anchor == KeyWords.VAR)
                        {
                            if (attribute.ContainAny(ExpressionAttribute.None))
                            {
                                if (Lexical.TryAnalysis(range, lexical.anchor.end, out var identifier, collector))
                                {
                                    index = identifier.anchor.end;
                                    var expression = new BlurryVariableDeclarationExpression(lexical.anchor & identifier.anchor, localContext.Snapshoot, lexical.anchor, identifier.anchor);
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                    goto label_next_lexical;
                                }
                            }
                            PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot));
                            attribute = ExpressionAttribute.Invalid;
                        }
                        else if (lexical.anchor == KeyWords.IS)
                        {
                            if (attribute.ContainAny(ExpressionAttribute.Value))
                            {
                                if (Lexical.TryExtractName(range, lexical.anchor.end, out var names, collector))
                                {
                                    var typeExpression = GetTypeExpression(range, names, ref index, out var type);
                                    var source = expressionStack.Pop();
                                    if (Convert(manager, source.tuple[0], type) >= 0) collector.Add(source.range & typeExpression.range, ErrorLevel.Warning, "给定的表达式始终为目标类型");
                                    else if (Convert(manager, type, source.tuple[0]) < 0) collector.Add(source.range & typeExpression.range, ErrorLevel.Warning, "给定的表达式始终无法转换为目标类型");
                                    if (Lexical.TryAnalysis(range, index, out var identifier, collector))
                                    {
                                        index = identifier.anchor.end;
                                        var local = localContext.Add(identifier.anchor, type, false);
                                        var expression = new IsCastExpression(source.range.start & index, localContext.Snapshoot, lexical.anchor, identifier.anchor, source, typeExpression, local, manager.kernelManager);
                                        expressionStack.Push(expression);
                                        attribute = expression.attribute;
                                    }
                                    else
                                    {
                                        var expression = new IsCastExpression(source.range.start & index, localContext.Snapshoot, lexical.anchor, null, source, typeExpression, null, manager.kernelManager);
                                        expressionStack.Push(expression);
                                        attribute = expression.attribute;
                                    }
                                    goto label_next_lexical;
                                }
                            }
                            PushInvalidExpression(expressionStack, lexical.anchor, attribute, "无效的操作", new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot));
                            attribute = ExpressionAttribute.Invalid;
                        }
                        else if (lexical.anchor == KeyWords.AS)
                        {
                            if (attribute.ContainAny(ExpressionAttribute.Value))
                            {
                                if (Lexical.TryExtractName(range, lexical.anchor.end, out var names, collector))
                                {
                                    var typeExpression = GetTypeExpression(range, names, ref index, out var type);
                                    if (!type.Managed) collector.Add(typeExpression.range, ErrorLevel.Error, "不是引用类型");
                                    var source = expressionStack.Pop();
                                    var expression = new AsCastExpression(source.range.start & index, localContext.Snapshoot, lexical.anchor, source, typeExpression, type);
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                    goto label_next_lexical;
                                }
                            }
                            PushInvalidExpression(expressionStack, lexical.anchor, attribute, "无效的操作", new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot));
                            attribute = ExpressionAttribute.Invalid;
                        }
                        else if (lexical.anchor == KeyWords.AND) goto case LexicalType.LogicAnd;
                        else if (lexical.anchor == KeyWords.OR) goto case LexicalType.LogicOr;
                        else if (lexical.anchor == KeyWords.START || lexical.anchor == KeyWords.NEW)
                        {
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                var expression = Parse(lexical.anchor.end & range.end);
                                index = range.end;
                                if (expression is InvokerExpression invoker)
                                {
                                    expression = new BlurryTaskExpression(lexical.anchor & expression.range, localContext.Snapshoot, lexical.anchor, invoker);
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                    if (destructor) collector.Add(expression.range, ErrorLevel.Error, "析构函数中不能创建任务对象");
                                }
                                else
                                {
                                    PushInvalidExpression(expressionStack, lexical.anchor, attribute, "无效的操作", new InvalidExpression(localContext.Snapshoot, new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot), expression));
                                    attribute = ExpressionAttribute.Invalid;
                                }
                                goto label_next_lexical;
                            }
                            PushInvalidExpression(expressionStack, lexical.anchor, attribute, "无效的操作", new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot));
                            attribute = ExpressionAttribute.Invalid;
                        }
                        else if (lexical.anchor == KeyWords.DISCARD_VARIABLE)
                        {
                            if (attribute.ContainAny(ExpressionAttribute.None))
                            {
                                index = lexical.anchor.end;
                                var expression = new DiscardVariableExpression(lexical.anchor, localContext.Snapshoot);
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                                goto label_next_lexical;
                            }
                            PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot));
                            attribute = ExpressionAttribute.Invalid;
                        }
                        else if (TryMatchBaseType(lexical.anchor, out var type))
                        {
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                index = lexical.anchor.end;
                                var dimension = Lexical.ExtractDimension(range, ref index);
                                var file = new FileType(lexical.anchor.start & index, new QualifiedName([lexical.anchor]), dimension);
                                var expression = new TypeKeyworldExpression(lexical.anchor, localContext.Snapshoot, null, file, new Type(type, dimension));
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                                goto label_next_lexical;
                            }
                            PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot));
                            attribute = ExpressionAttribute.Invalid;
                        }
                        else if (KeyWords.IsKeyWorld(lexical.anchor.ToString()))
                        {
                            PushInvalidExpression(expressionStack, lexical.anchor, attribute, "无效的操作", new InvalidKeyworldExpression(lexical.anchor, localContext.Snapshoot));
                            attribute = ExpressionAttribute.Invalid;
                        }
                        else if (attribute.ContainAny(ExpressionAttribute.Type))
                        {
                            var typeExpression = (TypeExpression)expressionStack.Pop();
                            var local = localContext.Add(lexical.anchor, typeExpression.type);
                            var expression = new VariableDeclarationLocalExpression(typeExpression.range & lexical.anchor, local, localContext.Snapshoot, lexical.anchor, typeExpression, ExpressionAttribute.Value | ExpressionAttribute.Assignable, manager.kernelManager);
                            expressionStack.Push(expression);
                            attribute = expression.attribute;
                        }
                        else if (localContext.TryGetLocal(lexical.anchor.ToString(), out var local))
                        {
                            var expression = new VariableLocalExpression(lexical.anchor, local, localContext.Snapshoot, lexical.anchor, ExpressionAttribute.Value | ExpressionAttribute.Assignable, manager.kernelManager);
                            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                            {
                                expressionStack.Push(expression);
                                attribute = expression.attribute;
                            }
                            else
                            {
                                PushInvalidExpression(expressionStack, lexical.anchor, attribute, "应输入 , 或 ;", expression);
                                attribute = ExpressionAttribute.Invalid;
                            }
                        }
                        else
                        {
                            if (TryFindDeclaration(range, ref index, expressionStack, attribute, out var results, out var name, context))
                                PushDeclarationsExpression(range, ref index, ref attribute, expressionStack, results, null, name);
                            else attribute = ExpressionAttribute.Invalid;
                            goto label_next_lexical;
                        }
                        break;
                    case LexicalType.Backslash:
                    default:
                        PushInvalidExpression(expressionStack, lexical.anchor, attribute, "意外的词条", new InvalidExpression(lexical.anchor, localContext.Snapshoot));
                        attribute = ExpressionAttribute.Invalid;
                        break;
                }
                index = lexical.anchor.end;
            label_next_lexical:;
            }
            if (attribute.ContainAny(ExpressionAttribute.Operator))
            {
                collector.Add(range.end & range.end, ErrorLevel.Error, "运算缺少参数");
                expressionStack.Push(new InvalidExpression(range.end & range.end, localContext.Snapshoot));
            }
            while (tokenStack.Count > 0) PopToken(expressionStack, tokenStack.Pop());
            if (expressionStack.Count > 1)
            {
                var expressions = new Expression[expressionStack.Count];
                while (expressionStack.Count > 0)
                {
                    var expression = expressionStack.Pop();
                    expressions[expressionStack.Count] = expression;
                }
                return TupleExpression.Create(expressions, localContext.Snapshoot, collector);
            }
            else if (expressionStack.Count > 0) return expressionStack.Pop();
            else return new TupleExpression(range, localContext.Snapshoot);
        }
        private Expression GetTypeExpression(TextRange range, List<TextRange> names, ref TextPosition index, out Type type)
        {
            if (names[^1].Count == 0)
            {
                index = names[^1].end;
                type = default;
                return new InvalidOperationExpression(names[0] & names[^1], localContext.Snapshoot, (names[^2] & names[^1]).Trim, new InvalidExpression(names[0] & names[^2], localContext.Snapshoot));
            }
            else
            {
                var name = new QualifiedName(names);
                index = name.name.end;
                var dimension = Lexical.ExtractDimension(range, ref index);
                var file = new FileType(name.Range.start & index, name, dimension);
                type = FileLink.GetType(context, manager, file, collector);
                return new TypeExpression(file.range, localContext.Snapshoot, null, file, type);
            }
        }
        private void PushDeclarationsExpression(TextRange range, ref TextPosition index, ref ExpressionAttribute attribute, Stack<Expression> expressionStack, List<AbstractDeclaration> declarations, TextRange? qualifier, QualifiedName name)
        {
            switch (declarations[0].declaration.category)
            {
                case DeclarationCategory.Invalid: throw new Exception("未知的错误");
                case DeclarationCategory.Variable:
                    {
                        CheckAmbiguity(attribute, declarations, name);
                        var expression = new VariableGlobalExpression(qualifier == null ? name.Range : qualifier.Value & name.Range, localContext.Snapshoot, qualifier, name, (AbstractVariable)declarations[0], manager.kernelManager);
                        if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                        {
                            expressionStack.Push(expression);
                            attribute = expression.attribute;
                        }
                        else
                        {
                            PushInvalidExpression(expressionStack, name.Range, attribute, "应输入 , 或 ;", expression);
                            attribute = ExpressionAttribute.Invalid;
                        }
                    }
                    break;
                case DeclarationCategory.Function:
                    PushMethodsExpression(ref attribute, expressionStack, declarations, qualifier, name);
                    break;
                case DeclarationCategory.Enum:
                    PushTypeExpression(range, ref index, ref attribute, expressionStack, declarations, qualifier, name);
                    break;
                case DeclarationCategory.EnumElement: throw new Exception("枚举内没有代码，不会走到这里");
                case DeclarationCategory.Struct:
                    PushTypeExpression(range, ref index, ref attribute, expressionStack, declarations, qualifier, name);
                    break;
                case DeclarationCategory.StructVariable:
                    {
                        CheckAmbiguity(attribute, declarations, name);
                        var expression = new VariableMemberExpression(name.Range, ((AbstractStruct.Variable)declarations[0]).type, localContext.Snapshoot, name.Range, declarations[0], manager.kernelManager);
                        if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                        {
                            expressionStack.Push(expression);
                            attribute = expression.attribute;
                        }
                        else
                        {
                            PushInvalidExpression(expressionStack, name.Range, attribute, "应输入 , 或 ;", expression);
                            attribute = ExpressionAttribute.Invalid;
                        }
                    }
                    break;
                case DeclarationCategory.StructFunction:
                    {
                        var callables = new List<AbstractCallable>();
                        var ambiguity = false;
                        foreach (var declaration in declarations)
                            if (declaration is AbstractStruct.Function function) callables.Add(function);
                            else ambiguity = true;
                        if (ambiguity) CheckAmbiguity(attribute, declarations, name);
                        var expression = new MethodMemberExpression(name.Range, localContext.Snapshoot, name.Range, callables);
                        if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                        {
                            expressionStack.Push(expression);
                            attribute = expression.attribute;
                        }
                        else
                        {
                            PushInvalidExpression(expressionStack, name.Range, attribute, "应输入 , 或 ;", expression);
                            attribute = ExpressionAttribute.Invalid;
                        }
                    }
                    break;
                case DeclarationCategory.Class:
                    PushTypeExpression(range, ref index, ref attribute, expressionStack, declarations, qualifier, name);
                    break;
                case DeclarationCategory.Constructor: throw new Exception("构造函数不参与重载决议");
                case DeclarationCategory.ClassVariable:
                    {
                        CheckAmbiguity(attribute, declarations, name);
                        var expression = new VariableMemberExpression(name.Range, ((AbstractClass.Variable)declarations[0]).type, localContext.Snapshoot, name.Range, declarations[0], manager.kernelManager);
                        if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                        {
                            expressionStack.Push(expression);
                            attribute = expression.attribute;
                        }
                        else
                        {
                            PushInvalidExpression(expressionStack, name.Range, attribute, "应输入 , 或 ;", expression);
                            attribute = ExpressionAttribute.Invalid;
                        }
                    }
                    break;
                case DeclarationCategory.ClassFunction:
                    {
                        var callables = new List<AbstractCallable>();
                        var ambiguity = false;
                        foreach (var declaration in declarations)
                            if (declaration is AbstractClass.Function function) callables.Add(function);
                            else ambiguity = true;
                        if (ambiguity) CheckAmbiguity(attribute, declarations, name);
                        var expression = new MethodVirtualExpression(name.Range, localContext.Snapshoot, name.Range, callables);
                        if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
                        {
                            expressionStack.Push(expression);
                            attribute = expression.attribute;
                        }
                        else
                        {
                            PushInvalidExpression(expressionStack, name.Range, attribute, "应输入 , 或 ;", expression);
                            attribute = ExpressionAttribute.Invalid;
                        }
                    }
                    break;
                case DeclarationCategory.Interface:
                    PushTypeExpression(range, ref index, ref attribute, expressionStack, declarations, qualifier, name);
                    break;
                case DeclarationCategory.InterfaceFunction: throw new Exception("接口内没有代码，不会走到这里");
                case DeclarationCategory.Delegate:
                case DeclarationCategory.Task:
                    PushTypeExpression(range, ref index, ref attribute, expressionStack, declarations, qualifier, name);
                    break;
                case DeclarationCategory.Native:
                    PushMethodsExpression(ref attribute, expressionStack, declarations, qualifier, name);
                    break;
            }
        }
        private void PushTypeExpression(TextRange range, ref TextPosition index, ref ExpressionAttribute attribute, Stack<Expression> expressionStack, List<AbstractDeclaration> declarations, TextRange? qualifier, QualifiedName name)
        {
            CheckAmbiguity(attribute, declarations, name);
            var dimension = Lexical.ExtractDimension(range, ref index);
            var file = new FileType(name.Range.start & index, name, dimension);
            var expression = new TypeExpression(qualifier == null ? file.range : qualifier.Value & file.range, localContext.Snapshoot, qualifier, file, new Type(declarations[0].declaration.DefineType, dimension));
            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
            {
                expressionStack.Push(expression);
                attribute = expression.attribute;
            }
            else
            {
                PushInvalidExpression(expressionStack, name.Range, attribute, "应输入 , 或 ;", expression);
                attribute = ExpressionAttribute.Invalid;
            }
        }
        private void PushMethodsExpression(ref ExpressionAttribute attribute, Stack<Expression> expressionStack, List<AbstractDeclaration> declarations, TextRange? qualifier, QualifiedName name)
        {
            var callables = new List<AbstractCallable>();
            var ambiguity = false;
            foreach (var declaration in declarations)
                if (declaration is AbstractFunction function) callables.Add(function);
                else if (declaration is AbstractNative native) callables.Add(native);
                else ambiguity = true;
            if (ambiguity) CheckAmbiguity(attribute, declarations, name);
            var expression = new MethodExpression(qualifier == null ? name.Range : qualifier.Value & name.Range, localContext.Snapshoot, qualifier, name, callables);
            if (attribute.ContainAny(ExpressionAttribute.None | ExpressionAttribute.Operator))
            {
                expressionStack.Push(expression);
                attribute = expression.attribute;
            }
            else
            {
                PushInvalidExpression(expressionStack, name.Range, attribute, "应输入 , 或 ;", expression);
                attribute = ExpressionAttribute.Invalid;
            }
        }
        private void CheckAmbiguity(ExpressionAttribute attribute, List<AbstractDeclaration> declarations, QualifiedName name)
        {
            if (declarations.Count > 1 && attribute != ExpressionAttribute.Invalid)
            {
                var msg = new Message(name.Range, ErrorLevel.Error, "有多个符合名称的声明");
                foreach (var declaration in declarations)
                    msg.related.Add(new RelatedInfo(declaration.name, declaration.GetFullName(manager)));
                collector.Add(msg);
            }
        }
        private bool TryFindDeclaration(TextRange range, ref TextPosition index, Stack<Expression> expressionStack, ExpressionAttribute attribute, [MaybeNullWhen(false)] out List<AbstractDeclaration> results, out QualifiedName name, in Context context)
        {
            if (Lexical.TryAnalysis(range, index, out var lexical, collector))
            {
                index = lexical.anchor.end;
                if (lexical.type == LexicalType.Word)
                {
                    if (context.TryFindDeclaration(manager, lexical.anchor.ToString(), out results))
                    {
                        name = new QualifiedName([], lexical.anchor);
                        return true;
                    }
                    else if (context.TryFindSpace(manager, lexical.anchor, out var space, collector))
                        return TryFindDeclaration(range, ref index, expressionStack, attribute, space, [lexical.anchor], out results, out name, context);
                    else PushInvalidExpression(expressionStack, lexical.anchor, attribute, "声明未找到", new InvalidExpression(lexical.anchor, localContext.Snapshoot));
                }
                else PushInvalidExpression(expressionStack, lexical.anchor, attribute, "意外的词条", new InvalidExpression(lexical.anchor, localContext.Snapshoot));
            }
            else collector.Add(index & index, ErrorLevel.Error, "应输入标识符");
            results = default;
            name = default;
            return false;
        }
        private bool TryFindDeclaration(TextRange range, ref TextPosition index, Stack<Expression> expressionStack, ExpressionAttribute attribute, AbstractSpace space, List<TextRange> spaceName, [MaybeNullWhen(false)] out List<AbstractDeclaration> results, out QualifiedName name, in Context context)
        {
            if (Lexical.TryAnalysis(range, index, out var lexical, collector) && lexical.type == LexicalType.Dot)
            {
                var dot = lexical.anchor;
                if (Lexical.TryAnalysis(range, dot.end, out lexical, collector) && lexical.type == LexicalType.Word)
                {
                    index = lexical.anchor.end;
                    if (space.declarations.TryGetValue(lexical.anchor.ToString(), out var declarations))
                    {
                        results = [];
                        foreach (var declaration in declarations)
                            if (context.IsVisiable(manager, declaration) && manager.TryGetDeclaration(declaration, out var result))
                                results.Add(result);
                        if (results.Count > 0)
                        {
                            name = new QualifiedName(spaceName, lexical.anchor);
                            return true;
                        }
                    }
                    else if (space.children.TryGetValue(lexical.anchor.ToString(), out var children))
                    {
                        spaceName.Add(lexical.anchor);
                        return TryFindDeclaration(range, ref index, expressionStack, attribute, children, spaceName, out results, out name, context);
                    }
                    PushInvalidExpression(expressionStack, spaceName[0] & lexical.anchor, attribute, "声明未找到", new InvalidExpression(spaceName[0] & lexical.anchor, localContext.Snapshoot));
                }
                else
                {
                    index = dot.end;
                    var parameter = new InvalidExpression(spaceName[0] & spaceName[^1], localContext.Snapshoot);
                    PushInvalidExpression(expressionStack, dot, attribute, "缺少标识符", new InvalidOperationExpression(parameter.range & dot, localContext.Snapshoot, dot, parameter));
                }
            }
            else PushInvalidExpression(expressionStack, spaceName[0] & spaceName[^1], attribute, "声明未找到", new InvalidExpression(spaceName[0] & spaceName[^1], localContext.Snapshoot));
            results = default;
            name = default;
            return false;
        }
        private bool TryMatchBaseType(TextRange anchor, out Type type)
        {
            if (anchor == KeyWords.BOOL)
            {
                type = manager.kernelManager.BOOL;
                return true;
            }
            else if (anchor == KeyWords.BYTE)
            {
                type = manager.kernelManager.BYTE;
                return true;
            }
            else if (anchor == KeyWords.CHAR)
            {
                type = manager.kernelManager.CHAR;
                return true;
            }
            else if (anchor == KeyWords.INTEGER)
            {
                type = manager.kernelManager.INT;
                return true;
            }
            else if (anchor == KeyWords.REAL)
            {
                type = manager.kernelManager.REAL;
                return true;
            }
            else if (anchor == KeyWords.REAL2)
            {
                type = manager.kernelManager.REAL2;
                return true;
            }
            else if (anchor == KeyWords.REAL3)
            {
                type = manager.kernelManager.REAL3;
                return true;
            }
            else if (anchor == KeyWords.REAL4)
            {
                type = manager.kernelManager.REAL4;
                return true;
            }
            else if (anchor == KeyWords.TYPE)
            {
                type = manager.kernelManager.TYPE;
                return true;
            }
            else if (anchor == KeyWords.STRING)
            {
                type = manager.kernelManager.STRING;
                return true;
            }
            else if (anchor == KeyWords.HANDLE)
            {
                type = manager.kernelManager.HANDLE;
                return true;
            }
            else if (anchor == KeyWords.ENTITY)
            {
                type = manager.kernelManager.ENTITY;
                return true;
            }
            else if (anchor == KeyWords.ARRAY)
            {
                type = manager.kernelManager.ARRAY;
                return true;
            }
            type = default;
            return false;
        }
        private void PushInvalidOperationExpression(Stack<Expression> expressionStack, TextRange symbol, ExpressionAttribute attribute, string message)
        {
            if (attribute != ExpressionAttribute.Invalid) collector.Add(symbol, ErrorLevel.Error, message);
            if (expressionStack.TryPop(out var expression))
                expressionStack.Push(new InvalidOperationExpression(expression.range & symbol, localContext.Snapshoot, symbol, expression));
            else
                expressionStack.Push(new InvalidOperationExpression(symbol, localContext.Snapshoot, symbol));
        }
        private void PushInvalidExpression(Stack<Expression> expressionStack, TextRange anchor, ExpressionAttribute attribute, string message, Expression expression)
        {
            if (attribute != ExpressionAttribute.Invalid) collector.Add(anchor, ErrorLevel.Error, message);
            if (expressionStack.TryPop(out var prevExpression))
                expressionStack.Push(new InvalidExpression(localContext.Snapshoot, prevExpression, expression));
            else
                expressionStack.Push(expression);
        }
        private void PushToken(Stack<Expression> expressionStack, Stack<Token> tokenStack, Token token, ExpressionAttribute attribute)
        {
            while (tokenStack.Count > 0 && token.Priority <= tokenStack.Peek().Priority) attribute = PopToken(expressionStack, tokenStack.Pop());

            if (attribute != ExpressionAttribute.Invalid && !attribute.ContainAny(token.Precondition))
            {
                collector.Add(token.lexical.anchor, ErrorLevel.Error, "无效的操作");
                if (token.Precondition.ContainAny(ExpressionAttribute.Value | ExpressionAttribute.Type))
                    expressionStack.Push(new InvalidExpression(token.lexical.anchor, localContext.Snapshoot));
            }
            else tokenStack.Push(token);
        }
        private ExpressionAttribute PopToken(Stack<Expression> expressionStack, Token token)
        {
            switch (token.type)
            {
                case TokenType.Invalid:
                case TokenType.LogicOperationPriority: break;
                case TokenType.LogicAnd:
                case TokenType.LogicOr:
                    {
                        if (expressionStack.Count < 2) return Operator(expressionStack, token.lexical.anchor, 2);
                        var right = expressionStack.Pop();
                        var left = expressionStack.Pop();
                        left = AssignmentConvert(left, manager.kernelManager.BOOL);
                        right = AssignmentConvert(right, manager.kernelManager.BOOL);
                        expressionStack.Push(new LogicExpression(left.range & right.range, localContext.Snapshoot, token.lexical.anchor, left, right, manager.kernelManager));
                        return expressionStack.Peek().attribute;
                    }
                case TokenType.CompareOperationPriority: break;
                case TokenType.Less:
                case TokenType.Greater:
                case TokenType.LessEquals:
                case TokenType.GreaterEquals:
                case TokenType.Equals:
                case TokenType.NotEquals: return Operator(expressionStack, token.lexical.anchor, 2);
                case TokenType.BitOperationPriority: break;
                case TokenType.BitAnd:
                case TokenType.BitOr:
                case TokenType.BitXor:
                case TokenType.ShiftLeft:
                case TokenType.ShiftRight: return Operator(expressionStack, token.lexical.anchor, 2);
                case TokenType.ElementaryOperationPriority: break;
                case TokenType.Plus:
                case TokenType.Minus: return Operator(expressionStack, token.lexical.anchor, 2);
                case TokenType.IntermediateOperationPriority: break;
                case TokenType.Mul:
                case TokenType.Div:
                case TokenType.Mod: return Operator(expressionStack, token.lexical.anchor, 2);
                case TokenType.SymbolicOperationPriority: break;
                case TokenType.Casting:
                    {
                        if (expressionStack.Count < 2) return Operator(expressionStack, token.lexical.anchor, 2);
                        var right = expressionStack.Pop();
                        var left = expressionStack.Pop();
                        if (left is TypeExpression type)
                        {
                            if (!right.attribute.ContainAny(ExpressionAttribute.Value)) collector.Add(right.range, ErrorLevel.Error, "只能对数值进行类型强转操作");
                            else
                            {
                                right = InferRightValueType(right, type.type);
                                if (Convert(manager, type.type, right.tuple[0]) < 0 && Convert(manager, right.tuple[0], type.type) < 0) collector.Add(left.range & right.range, ErrorLevel.Error, "无法进行类型转换");
                            }
                            expressionStack.Push(new CastExpression(left.range & right.range, type, localContext.Snapshoot, token.lexical.anchor, right, manager.kernelManager));
                        }
                        else expressionStack.Push(new InvalidExpression(localContext.Snapshoot, left, right));
                        return expressionStack.Peek().attribute;
                    }
                case TokenType.Not:
                case TokenType.Inverse:
                case TokenType.Positive:
                case TokenType.Negative:
                case TokenType.IncrementLeft:
                case TokenType.DecrementLeft: return Operator(expressionStack, token.lexical.anchor, 1);
            }
            throw new Exception("无效的token类型");
        }
        private ExpressionAttribute Operator(Stack<Expression> expressionStack, TextRange name, int count)
        {
            if (expressionStack.Count < count)
            {
                count = expressionStack.Count;
                var parameters = new Expression[count];
                while (count-- > 0) parameters[count] = expressionStack.Pop();
                var range = parameters.Length > 0 ? TextRange.Union(parameters[0].range & parameters[^1].range, name) : name;
                expressionStack.Push(new InvalidExpression(range, localContext.Snapshoot, parameters));
                return ExpressionAttribute.Invalid;
            }
            else
            {
                var parameters = new Expression[count];
                while (count-- > 0) parameters[count] = expressionStack.Pop();
                var parameterRange = parameters[0].range & parameters[^1].range;
                var result = CreateOperation(TextRange.Union(parameterRange, name), name.ToString(), name, parameters);
                expressionStack.Push(result);
                return result.attribute;
            }
        }
        private Expression ConvertVectorParameter(Expression parameter, int count)
        {
            if (!parameter.Valid) return parameter;
            var parameterTypes = new List<Type>();
            foreach (var type in parameter.tuple)
                if (type == manager.kernelManager.REAL || type == manager.kernelManager.REAL2 || type == manager.kernelManager.REAL3 || type == manager.kernelManager.REAL4) parameterTypes.Add(type);
                else parameterTypes.Add(manager.kernelManager.REAL);
            parameter = AssignmentConvert(parameter, new TypeSpan(parameterTypes));
            foreach (var type in parameterTypes)
                if (type == manager.kernelManager.REAL) count--;
                else if (type == manager.kernelManager.REAL2) count -= 2;
                else if (type == manager.kernelManager.REAL3) count -= 3;
                else if (type == manager.kernelManager.REAL4) count -= 4;
            if (count != 0) collector.Add(parameter.range, ErrorLevel.Error, "参数数量不对");
            return parameter;
        }
        private QuestionNullExpression ParseQuestionNull(TextRange left, TextRange symbol, TextRange right)
        {
            var leftExpression = Parse(left);
            var rightExpression = Parse(right);
            if (!leftExpression.attribute.ContainAll(ExpressionAttribute.Value))
            {
                collector.Add(left, ErrorLevel.Error, "表达式不是个值");
                leftExpression = leftExpression.ToInvalid();
            }
            else if (leftExpression.tuple[0] != manager.kernelManager.ENTITY && !leftExpression.tuple[0].Managed)
            {
                collector.Add(left, ErrorLevel.Error, "不是可以为空的类型");
                rightExpression = rightExpression.ToInvalid();
            }
            rightExpression = AssignmentConvert(rightExpression, leftExpression.tuple);
            return new QuestionNullExpression(symbol, leftExpression, rightExpression, localContext.Snapshoot);
        }
        private AssignmentExpression ParseAssignment(TextRange left, Lexical symbol, TextRange right)
        {
            left = left.Trim;
            right = right.Trim;
            var leftExpression = Parse(left);
            var rightExpression = Parse(right);
            if (!leftExpression.attribute.ContainAny(ExpressionAttribute.Assignable))
                collector.Add(left, ErrorLevel.Error, "表达式不可赋值");
            switch (symbol.type)
            {
                case LexicalType.Unknow:
                case LexicalType.BracketLeft0:
                case LexicalType.BracketLeft1:
                case LexicalType.BracketLeft2:
                case LexicalType.BracketRight0:
                case LexicalType.BracketRight1:
                case LexicalType.BracketRight2:
                case LexicalType.Comma:
                case LexicalType.Semicolon: goto default;
                case LexicalType.Assignment:
                    leftExpression = InferLeftValueType(leftExpression, rightExpression.tuple);
                    rightExpression = AssignmentConvert(rightExpression, leftExpression.tuple);
                    return new AssignmentExpression(left & right, localContext.Snapshoot, symbol.anchor, leftExpression, rightExpression);
                case LexicalType.Equals:
                case LexicalType.Lambda:
                case LexicalType.BitAnd:
                case LexicalType.LogicAnd: goto default;
                case LexicalType.BitAndAssignment:
                    rightExpression = CreateOperation(left & right, "&", symbol.anchor, leftExpression, rightExpression);
                    goto case LexicalType.Assignment;
                case LexicalType.BitOr:
                case LexicalType.LogicOr: goto default;
                case LexicalType.BitOrAssignment:
                    rightExpression = CreateOperation(left & right, "|", symbol.anchor, leftExpression, rightExpression);
                    goto case LexicalType.Assignment;
                case LexicalType.BitXor: goto default;
                case LexicalType.BitXorAssignment:
                    rightExpression = CreateOperation(left & right, "^", symbol.anchor, leftExpression, rightExpression);
                    goto case LexicalType.Assignment;
                case LexicalType.Less:
                case LexicalType.LessEquals:
                case LexicalType.ShiftLeft: goto default;
                case LexicalType.ShiftLeftAssignment:
                    rightExpression = CreateOperation(left & right, "<<", symbol.anchor, leftExpression, rightExpression);
                    goto case LexicalType.Assignment;
                case LexicalType.Greater:
                case LexicalType.GreaterEquals:
                case LexicalType.ShiftRight: goto default;
                case LexicalType.ShiftRightAssignment:
                    rightExpression = CreateOperation(left & right, ">>", symbol.anchor, leftExpression, rightExpression);
                    goto case LexicalType.Assignment;
                case LexicalType.Plus:
                case LexicalType.Increment: goto default;
                case LexicalType.PlusAssignment:
                    rightExpression = CreateOperation(left & right, "+", symbol.anchor, leftExpression, rightExpression);
                    goto case LexicalType.Assignment;
                case LexicalType.Minus:
                case LexicalType.Decrement:
                case LexicalType.RealInvoker: goto default;
                case LexicalType.MinusAssignment:
                    rightExpression = CreateOperation(left & right, "-", symbol.anchor, leftExpression, rightExpression);
                    goto case LexicalType.Assignment;
                case LexicalType.Mul: goto default;
                case LexicalType.MulAssignment:
                    rightExpression = CreateOperation(left & right, "*", symbol.anchor, leftExpression, rightExpression);
                    goto case LexicalType.Assignment;
                case LexicalType.Div: goto default;
                case LexicalType.DivAssignment:
                    rightExpression = CreateOperation(left & right, "/", symbol.anchor, leftExpression, rightExpression);
                    goto case LexicalType.Assignment;
                case LexicalType.Annotation:
                case LexicalType.Mod: goto default;
                case LexicalType.ModAssignment:
                    rightExpression = CreateOperation(left & right, "%", symbol.anchor, leftExpression, rightExpression);
                    goto case LexicalType.Assignment;
                case LexicalType.Not:
                case LexicalType.NotEquals:
                case LexicalType.Negate:
                case LexicalType.Dot:
                case LexicalType.Question:
                case LexicalType.QuestionDot:
                case LexicalType.QuestionRealInvoke:
                case LexicalType.QuestionInvoke:
                case LexicalType.QuestionIndex:
                case LexicalType.QuestionNull:
                case LexicalType.Colon:
                case LexicalType.ConstReal:
                case LexicalType.ConstNumber:
                case LexicalType.ConstBinary:
                case LexicalType.ConstHexadecimal:
                case LexicalType.ConstChars:
                case LexicalType.ConstString:
                case LexicalType.TemplateString:
                case LexicalType.Word:
                case LexicalType.Backslash:
                default: throw new Exception("语法类型错误");
            }
        }
        public Expression InferLeftValueType(Expression expression, TypeSpan span)
        {
            if (!expression.Valid)
            {
                if ((TypeSpan)expression.tuple != span)
                    return new InvalidExpression(expression, span, localContext.Snapshoot);
                return expression;
            }
            if (expression.tuple.Count != span.Count)
            {
                collector.Add(expression.range, ErrorLevel.Error, "类型数量不一致");
                return new InvalidExpression(expression, span, localContext.Snapshoot);
            }
            if (TryInferLeftValueType(ref expression, span[0])) return expression;
            else if (expression is TupleExpression tuple)
            {
                var expressions = new List<Expression>();
                var index = 0;
                foreach (var item in tuple.expressions)
                {
                    expressions.Add(InferLeftValueType(item, span[index..(index + item.tuple.Count)]));
                    index += item.tuple.Count;
                }
                return TupleExpression.Create(expressions, localContext.Snapshoot, collector);
            }
            else if (ContainBlurry(expression.tuple))
            {
                if (expression is BracketExpression bracket)
                    return bracket.Replace(InferLeftValueType(bracket.expression, span));
                throw new Exception("表达式类型错误");
            }
            return expression;
        }
        private bool CheckInferLeftValueType(ref Expression expression, Type type)
        {
            if (type == Expression.BLURRY || type == Expression.NULL)
            {
                collector.Add(expression.range, ErrorLevel.Error, "表达式类型不明确");
                expression = new InvalidExpression(expression, type, localContext.Snapshoot);
                return true;
            }
            return false;
        }
        public bool TryInferLeftValueType(ref Expression expression, Type type)
        {
            if (expression is BlurryVariableDeclarationExpression blurry)
            {
                if (CheckInferLeftValueType(ref expression, type)) return true;
                var typeExpression = new TypeKeyworldExpression(blurry.declaration, localContext.Snapshoot, null, new FileType(blurry.declaration, new QualifiedName([blurry.declaration]), type.dimension), type);
                var local = localContext.Add(blurry.identifier, type);
                expression = new VariableDeclarationLocalExpression(blurry.range, local, localContext.Snapshoot, blurry.identifier, typeExpression, ExpressionAttribute.Assignable | ExpressionAttribute.Value, manager.kernelManager);
                return true;
            }
            else if (expression is DiscardVariableExpression discard)
            {
                if (CheckInferLeftValueType(ref expression, type)) return true;
                var local = localContext.Add(discard.range, type);
                expression = new VariableKeyworldLocalExpression(discard.range, local, type, localContext.Snapshoot, discard.range, ExpressionAttribute.Assignable, manager.kernelManager);
                return true;
            }
            return false;
        }
        private Expression CreateOperation(TextRange range, string operation, TextRange symbol, params Expression[] expressions)
        {
            var parameters = TupleExpression.Create(expressions, localContext.Snapshoot, collector);
            if (TryGetFunction(symbol, context.FindOperation(manager, operation), parameters, out var callable))
            {
                if ((operation == "++" || operation == "--") && callable.declaration.library == Manager.LIBRARY_KERNEL)
                    foreach (var expression in expressions)
                        if (!expression.attribute.ContainAny(ExpressionAttribute.Assignable))
                            collector.Add(expression.range, ErrorLevel.Error, "表达式不是可赋值的");
                parameters = AssignmentConvert(parameters, callable.signature);
                return new OperationExpression(range, localContext.Snapshoot, symbol, callable, parameters, manager.kernelManager);
            }
            else if (parameters.Valid) collector.Add(symbol, ErrorLevel.Error, "操作未找到");
            return new InvalidOperationExpression(range, localContext.Snapshoot, symbol, parameters);
        }
        public bool TryGetFunction(TextRange range, List<AbstractCallable> callbales, Expression parameters, [MaybeNullWhen(false)] out AbstractCallable result)
        {
            if (!parameters.Valid)
            {
                result = null;
                return false;
            }
            if (callbales.Count == 1)
            {
                result = callbales[0];
                return true;
            }
            var results = new List<AbstractCallable>();
            var min = 0;
            var types = new List<Type>();
            foreach (var callable in callbales)
                if (callable.signature.Count == parameters.tuple.Count)
                {
                    if (TryExplicitTypes(parameters, callable.signature, types))
                    {
                        var measure = Convert(new TypeSpan(types), callable.signature);
                        if (measure >= 0)
                            if (results.Count == 0 || measure < min)
                            {
                                results.Clear();
                                min = measure;
                                results.Add(callable);
                            }
                            else if (measure == min) results.Add(callable);
                    }
                    types.Clear();
                }
            result = null;
            if (results.Count == 1) result = results[0];
            else if (results.Count > 1)
            {
                var msg = new Message(range, ErrorLevel.Error, "语义不明确");
                foreach (var callable in results)
                {
                    result = callable;
                    msg.related.Add(new RelatedInfo(callable.name, "符合条件的函数"));
                }
                collector.Add(msg);
            }
            return result != null;
        }
        private bool TryExplicitTypes(Expression expression, TypeSpan target, List<Type> result)
        {
            if (!expression.Valid) return false;
            if (expression is TupleExpression tuple)
            {
                var index = 0;
                foreach (var item in tuple.expressions)
                {
                    if (!TryExplicitTypes(item, target[index..(index + item.tuple.Count)], result)) return false;
                    index += item.tuple.Count;
                }
                return true;
            }
            else if (expression is BracketExpression bracket) return TryExplicitTypes(bracket.expression, target, result);
            else if (expression.tuple.Count == 1) return TryExplicitTypes(expression, target[0], result);
            result.AddRange(expression.tuple);
            return true;
        }
        private bool TryExplicitTypes(Expression expression, Type target, List<Type> result)
        {
            if (!expression.Valid) return false;
            if (expression.tuple[0] == Expression.NULL)
            {
                if (target != manager.kernelManager.ENTITY && !target.Managed) return false;
                result.Add(target);
            }
            else if (expression.tuple[0] != Expression.BLURRY) result.Add(expression.tuple[0]);
            else
            {
                if (expression is BlurrySetExpression blurrySet)
                {
                    if (target.dimension == 0) return false;
                    var elementTypes = new Type[blurrySet.expression.tuple.Count];
                    var elementType = new Type(target, target.dimension - 1);
                    for (var i = 0; i < elementTypes.Length; i++) elementTypes[i] = elementType;
                    if (TryExplicitTypes(blurrySet.expression, elementTypes, result)) result.RemoveRange(result.Count - elementTypes.Length, elementTypes.Length);
                    else return false;
                }
                else if (expression is MethodExpression method)
                {
                    if (target.dimension > 0) return false;
                    if (!manager.TryGetDeclaration(target, out var declaration) || declaration is not AbstractDelegate abstractDelegate) return false;
                    if (method.callables.Find(item => item.signature == abstractDelegate.signature) == null) return false;
                }
                else if (expression is MethodMemberExpression methodMember)
                {
                    if (target.dimension > 0) return false;
                    if (!manager.TryGetDeclaration(target, out var declaration) || declaration is not AbstractDelegate abstractDelegate) return false;
                    if (methodMember.callables.Find(item => item.signature == abstractDelegate.signature) == null) return false;
                }
                else if (expression is BlurryTaskExpression blurryTask)
                {
                    if (target.dimension > 0) return false;
                    if (!manager.TryGetDeclaration(target, out var declaration) || declaration is not AbstractTask abstractTask) return false;
                    if (blurryTask.invoker.tuple != abstractTask.returns) return false;
                }
                else if (expression is BlurryLambdaExpression blurryLambda)
                {
                    if (target.dimension > 0) return false;
                    if (!manager.TryGetDeclaration(target, out var declaration) || declaration is not AbstractDelegate abstractDelegate) return false;
                    localContext.PushBlock();
                    for (var i = 0; i < abstractDelegate.parameters.Count; i++)
                        localContext.Add(blurryLambda.parameters[i], abstractDelegate.signature[i], true);
                    var body = Parse(blurryLambda.body);
                    localContext.PopBlock();
                    if (!body.Valid) return false;
                    else if (abstractDelegate.returns.Count > 0)
                    {
                        if (abstractDelegate.returns != body.tuple)
                        {
                            body = AssignmentConvert(body, abstractDelegate.returns);
                            if (!body.Valid) return false;
                        }
                    }
                    else if (ContainBlurry(body)) return false;
                }
                result.Add(target);
            }
            return true;
        }
        public Expression AssignmentConvert(Expression source, Type type)
        {
            if (!source.Valid) return source;
            if (source.tuple.Count == 1)
            {
                source = InferRightValueType(source, type);
                if (source.Valid)
                    if (Convert(manager, source.tuple[0], type) < 0)
                        collector.Add(source.range, ErrorLevel.Error, "当前表达式无法转换为目标类型");
                return source;
            }
            else collector.Add(source.range, ErrorLevel.Error, "类型数量不一致");
            return new TupleCastExpression(source, type, localContext.Snapshoot, manager.kernelManager);
        }
        public Expression AssignmentConvert(Expression source, TypeSpan span)
        {
            if (!source.Valid) return source;
            if (source.tuple.Count == span.Count)
            {
                source = InferRightValueType(source, span);
                if (source.Valid)
                    for (var i = 0; i < span.Count; i++)
                        if (Convert(manager, source.tuple[i], span[i]) < 0)
                            collector.Add(source.range, ErrorLevel.Error, $"当前表达式第{i + 1}个类型无法转换为目标类型");
                return source;
            }
            else collector.Add(source.range, ErrorLevel.Error, "类型数量不一致");
            return new TupleCastExpression(source, span, localContext.Snapshoot, manager.kernelManager);
        }
        private Expression InferRightValueType(Expression source, TypeSpan span)
        {
            if (!source.Valid) return source;
            if (source is TupleExpression tuple)
            {
                if (tuple.expressions.Count == 0) return source;
                var expressions = new List<Expression>();
                var index = 0;
                foreach (var item in tuple.expressions)
                {
                    expressions.Add(InferRightValueType(item, span[index..(index + item.tuple.Count)]));
                    index += item.tuple.Count;
                }
                return TupleExpression.Create(expressions, localContext.Snapshoot, collector);
            }
            else if (source.tuple.Count == 1) return InferRightValueType(source, span[0]);
            else if (ContainBlurry(source.tuple))
            {
                if (source is BracketExpression bracket)
                    return bracket.Replace(InferRightValueType(bracket.expression, span));
                throw new Exception("表达式类型错误");
            }
            return source;
        }
        private Expression InferRightValueType(Expression expression, Type type)
        {
            if (expression.tuple.Count == 1 && expression.tuple[0] == type) return expression;
            else if (!expression.Valid) return new InvalidExpression(expression, type, localContext.Snapshoot);
            else if (type == Expression.BLURRY) collector.Add(expression.range, ErrorLevel.Error, "表达式类型名不明确");
            else if (expression is ConstNullExpression)
            {
                if (type == manager.kernelManager.ENTITY) return new ConstEntityNullExpression(expression.range, localContext.Snapshoot, manager.kernelManager);
                else if (type.Managed) return new ConstHandleNullExpression(expression.range, type, localContext.Snapshoot);
                collector.Add(expression.range, ErrorLevel.Error, "类型不匹配");
            }
            else if (expression is BlurrySetExpression blurrySet)
            {
                if (type.dimension > 0)
                {
                    var elementType = new Type(type, type.dimension - 1);
                    var elementTypes = new Type[blurrySet.expression.tuple.Count];
                    elementTypes.Fill(elementType);
                    var elements = blurrySet.expression.Replace(AssignmentConvert(blurrySet.expression.expression, elementTypes));
                    return new ArrayInitExpression(blurrySet.range, type, localContext.Snapshoot, null, elements);
                }
                else collector.Add(expression.range, ErrorLevel.Error, "类型不匹配");
            }
            else if (expression is MethodExpression method)
            {
                if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstractDelegate abstractDelegate)
                {
                    var callable = method.callables.Find(item => item.signature == abstractDelegate.signature);
                    if (callable != null)
                    {
                        if (callable.returns != abstractDelegate.returns)
                            collector.Add(method.name.name, ErrorLevel.Error, "返回值类型不一致");
                        return new FunctionDelegateCreateExpression(method.range, method.qualifier, method.name, type, localContext.Snapshoot, callable, manager.kernelManager);
                    }
                }
                collector.Add(method.name.name, ErrorLevel.Error, "无法转换为目标类型");
            }
            else if (expression is MethodMemberExpression methodMember)
            {
                if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstractDelegate abstractDelegate)
                {
                    var callable = methodMember.callables.Find(item => item.signature == abstractDelegate.signature);
                    if (callable != null)
                    {
                        if (callable.returns != abstractDelegate.returns)
                            collector.Add(methodMember.member, ErrorLevel.Error, "返回值类型不一致");
                        if (methodMember is MethodVirtualExpression)
                            return new VirtualFunctionDelegateCreateExpression(expression.range, type, localContext.Snapshoot, callable, manager.kernelManager, methodMember.target, methodMember.symbol, methodMember.member);
                        else
                            return new MemberFunctionDelegateCreateExpression(expression.range, type, localContext.Snapshoot, callable, manager.kernelManager, methodMember.target, methodMember.symbol, methodMember.member);
                    }
                }
                collector.Add(methodMember.member, ErrorLevel.Error, "无法转换为目标类型");
            }
            else if (expression is BlurryTaskExpression blurryTask)
            {
                if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstractTask abstractTask)
                {
                    if (blurryTask.invoker.tuple != abstractTask.returns)
                        collector.Add(expression.range, ErrorLevel.Error, "返回值类型不匹配");
                    return new TaskCreateExpression(expression.range, type, localContext.Snapshoot, blurryTask.symbol, blurryTask.invoker, manager.kernelManager);
                }
                collector.Add(expression.range, ErrorLevel.Error, "无法转换为目标类型");
            }
            else if (expression is BlurryLambdaExpression blurryLambda)
            {
                if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstractDelegate abstractDelegate)
                {
                    if (blurryLambda.parameters.Count != abstractDelegate.parameters.Count)
                    {
                        collector.Add(expression.range, ErrorLevel.Error, "参数数量与委托类型参数数量不一致");
                        return expression;
                    }
                    localContext.PushBlock();
                    var parameters = new List<Local>();
                    for (var i = 0; i < blurryLambda.parameters.Count; i++)
                        parameters.Add(localContext.Add(blurryLambda.parameters[i], abstractDelegate.signature[i], true));
                    var body = Parse(blurryLambda.body);
                    localContext.PopBlock();
                    if (body.Valid && abstractDelegate.returns.Count > 0 && body.tuple != abstractDelegate.returns)
                        body = AssignmentConvert(body, abstractDelegate.returns);
                    if (destructor) collector.Add(expression.range, ErrorLevel.Error, "析构函数中不能创建委托对象");
                    return new LambdaDelegateCreateExpression(expression.range, type, localContext.Snapshoot, abstractDelegate, manager.kernelManager, parameters, blurryLambda.symbol, body);
                }
                collector.Add(expression.range, ErrorLevel.Error, "无法转换为目标类型");
            }
            else if (expression is ConstExpression constExpression)
            {
                if (type == manager.kernelManager.REAL)
                {
                    if (constExpression is not ConstRealExpression && constExpression.TryEvaluate(out double value))
                        return new ConstRealTransformExpression(constExpression, localContext.Snapshoot, value, manager.kernelManager);
                }
                else if (type == manager.kernelManager.INT)
                {
                    if (constExpression is not ConstIntegerExpression && constExpression.TryEvaluate(out long value))
                        return new ConstIntegerExpression(expression.range, localContext.Snapshoot, value, true, manager.kernelManager);
                }
                else if (type == manager.kernelManager.CHAR)
                {
                    if (constExpression is not ConstCharExpression && constExpression.TryEvaluate(out char value))
                        return new ConstCharExpression(expression.range, localContext.Snapshoot, value, manager.kernelManager);
                }
            }
            else if (expression is BracketExpression bracketExpression) return bracketExpression.Replace(InferRightValueType(bracketExpression.expression, type));
            return expression;
        }
        private ExpressionAttribute ParseVectorMember(Stack<Expression> expressionStack, Expression target, TextRange symbol, TextRange member, int dimension)
        {
            if (CheckVectorMemberValid(member, dimension))
            {
                Type type = default;
                if (member.Count == 1) type = manager.kernelManager.REAL;
                else if (member.Count == 2) type = manager.kernelManager.REAL2;
                else if (member.Count == 3) type = manager.kernelManager.REAL3;
                else if (member.Count == 4) type = manager.kernelManager.REAL4;
                var expression = new VectorMemberExpression(target.range & member, type, localContext.Snapshoot, target, symbol, member);
                expressionStack.Push(expression);
                return expression.attribute;
            }
            else
            {
                var expression = new InvalidOperationExpression(symbol & member, localContext.Snapshoot, symbol, new InvalidExpression(member, localContext.Snapshoot));
                if (expressionStack.TryPop(out var prevExpression))
                    expressionStack.Push(new InvalidExpression(localContext.Snapshoot, prevExpression, expression));
                else
                    expressionStack.Push(expression);
                return ExpressionAttribute.Invalid;
            }
        }
        private bool CheckVectorMemberValid(TextRange member, int dimension)
        {
            if (member.Count > 4)
            {
                collector.Add(member, ErrorLevel.Error, "最多支持4维向量");
                return false;
            }
            for (int i = 0; i < member.Count; i++)
                switch (member[i])
                {
                    case 'x':
                    case 'y':
                        break;
                    case 'z':
                    case 'r':
                    case 'g':
                    case 'b':
                        if (dimension < 3)
                        {
                            collector.Add(member[i..(i + 1)], ErrorLevel.Error, "至少是3维向量才支持该成员");
                            return false;
                        }
                        break;
                    case 'w':
                    case 'a':
                        if (dimension < 4)
                        {
                            collector.Add(member[i..(i + 1)], ErrorLevel.Error, "至少是4维向量才支持该成员");
                            return false;
                        }
                        break;
                    default:
                        collector.Add(member, ErrorLevel.Error, "没有找到该成员");
                        return false;
                }
            return true;
        }
        private QuestionExpression ParseQuestion(TextRange condition, TextRange symbol, TextRange value)
        {
            condition = condition.Trim;
            var conditionExpression = Parse(condition);
            if (!conditionExpression.attribute.ContainAny(ExpressionAttribute.Value))
            {
                collector.Add(condition, ErrorLevel.Error, "不是个值");
                conditionExpression = conditionExpression.ToInvalid();
            }
            var lexical = ExpressionSplit.Split(value, SplitFlag.Colon, out var left, out var right, collector);
            if (lexical.type != LexicalType.Unknow)
                return new QuestionExpression(condition & value, localContext.Snapshoot, symbol, lexical.anchor, conditionExpression, Parse(left), Parse(right));
            else
                return new QuestionExpression(condition & value, localContext.Snapshoot, symbol, null, conditionExpression, Parse(value), null);
        }
        private BlurryLambdaExpression ParseLambda(TextRange parametersRange, TextRange symbol, TextRange body)
        {
            var parameters = parametersRange = parametersRange.Trim;
            while (ExpressionSplit.Split(parameters, SplitFlag.Bracket0, out var left, out var right, collector).type == LexicalType.BracketRight0 && left.start == parameters.start && right.end == parameters.end)
                parameters = (left.end & right.start).Trim;
            var list = new List<TextRange>();
            while (ExpressionSplit.Split(parameters, SplitFlag.Comma | SplitFlag.Semicolon, out var left, out var right, collector).type != LexicalType.Unknow)
            {
                if (TryParseLambdaParameter(left, out left)) list.Add(left);
                parameters = right.Trim;
            }
            if (TryParseLambdaParameter(parameters, out parameters)) list.Add(parameters);
            return new BlurryLambdaExpression(parametersRange & body, localContext.Snapshoot, list, symbol, body);
        }
        private bool TryParseLambdaParameter(TextRange range, out TextRange parameter)
        {
            if (Lexical.TryAnalysis(range, 0, out var lexical, collector))
            {
                parameter = lexical.anchor;
                if (lexical.type == LexicalType.Word)
                {
                    if (Lexical.TryAnalysis(range, lexical.anchor.end, out lexical, collector))
                        collector.Add(lexical.anchor, ErrorLevel.Error, "意外的词条");
                }
                else collector.Add(range, ErrorLevel.Error, "无效的词条");
                return true;
            }
            parameter = default;
            return false;
        }
        private bool TryParseTuple(SplitFlag flag, LexicalType type, TextRange range, [MaybeNullWhen(false)] out Expression expression)
        {
            if (ExpressionSplit.Split(range, flag, out var left, out var right, collector).type == type)
            {
                TextRange remainder;
                var expressions = new List<Expression>();
                do
                {
                    remainder = right;
                    expressions.Add(Parse(left));
                }
                while (ExpressionSplit.Split(remainder, flag, out left, out right, collector).type == type);
                if (remainder.Valid) expressions.Add(Parse(remainder));
                expression = TupleExpression.Create(expressions, localContext.Snapshoot, collector);
                return true;
            }
            expression = default;
            return false;
        }
        private BracketExpression ParseBracket(TextRange range, TextRange bracketLeft, SplitFlag flag)
        {
            if (ExpressionSplit.Split(bracketLeft.start & range.end, flag, out var left, out var right, collector).type != LexicalType.Unknow)
                return new BracketExpression(left, right, Parse(left.end & right.start), localContext.Snapshoot);
            collector.Add(bracketLeft, ErrorLevel.Error, "缺少配对的符号");
            return new BracketExpression(bracketLeft, range.end & range.end, Parse(bracketLeft.end & range.end), localContext.Snapshoot);
        }
        private bool TryParseBracket(TextRange range, [MaybeNullWhen(false)] out Expression expression)
        {
            expression = default;
            if (!Lexical.TryAnalysis(range, 0, out var lexical, null) || lexical.type != LexicalType.BracketLeft0) return false;
            lexical = ExpressionSplit.Split(range, SplitFlag.Bracket0, out var left, out var right, collector);
            if (lexical.type == LexicalType.BracketRight0 && right.end == range.end)
            {
                expression = new BracketExpression(left, right, Parse(left.end & right.start), localContext.Snapshoot);
                return true;
            }
            return false;
        }
        private bool IsIndies(Tuple tuple)
        {
            foreach (var type in tuple)
                if (type != manager.kernelManager.INT) return false;
            return true;
        }
        public static bool ContainBlurry(Expression expression)
        {
            if (expression is TupleExpression tuple)
            {
                foreach (var item in tuple.expressions)
                    if (ContainBlurry(item)) return true;
            }
            else if (expression is BlurryTaskExpression) return false;
            return ContainBlurry(expression.tuple);
        }
        private static bool ContainBlurry(Tuple tuple)
        {
            foreach (var type in tuple)
                if (type == Expression.BLURRY || type == Expression.NULL)
                    return true;
            return false;
        }
        private int Convert(TypeSpan source, TypeSpan target)
        {
            if (source.Count != target.Count) return -1;
            var result = 0;
            for (var i = 0; i < source.Count; i++)
            {
                var index = Convert(manager, source[i], target[i]);
                if (index < 0) return -1;
                result += index;
            }
            return result;
        }
        public static int Convert(Manager manager, Type source, Type target)
        {
            if (source == Expression.BLURRY || target == Expression.NULL) return -1;
            else if (source == Expression.NULL)
            {
                if (target == manager.kernelManager.ENTITY || target.Managed) return 0;
            }
            else if (target == Expression.BLURRY)
            {
                if (source != Expression.NULL) return 0;
            }
            else if (source == target) return 0;
            else if (target == manager.kernelManager.CHAR)
            {
                if (source == manager.kernelManager.BYTE) return 0xf;
            }
            else if (target == manager.kernelManager.INT)
            {
                if (source == manager.kernelManager.BYTE) return 0xff;
                else if (source == manager.kernelManager.CHAR) return 0xf;
                else if (source.dimension == 0 && source.code == TypeCode.Enum) return 0xffff;
            }
            else if (target == manager.kernelManager.REAL)
            {
                if (source == manager.kernelManager.BYTE) return 0xfff;
                else if (source == manager.kernelManager.CHAR) return 0xff;
                else if (source == manager.kernelManager.INT) return 0xf;
                else if (source.dimension == 0 && source.code == TypeCode.Enum) return 0xfffff;
            }
            else if (target == manager.kernelManager.REAL2)
            {
                if (source == manager.kernelManager.REAL3) return 0xff;
                else if (source == manager.kernelManager.REAL4) return 0xfff;
            }
            else if (target == manager.kernelManager.REAL3)
            {
                if (source == manager.kernelManager.REAL2) return 0xffff;
                else if (source == manager.kernelManager.REAL4) return 0xff;
            }
            else if (target == manager.kernelManager.REAL4)
            {
                if (source == manager.kernelManager.REAL2) return 0xfffff;
                else if (source == manager.kernelManager.REAL3) return 0xffff;
            }
            else
            {
                var deep = GetInheritDeep(manager, target, source);
                if (deep >= 0) return deep;
                else if (target == manager.kernelManager.HANDLE) return 0xffffff;
            }
            return -1;
        }
        private static int GetInheritDeep(Manager manager, Type baseType, Type subType)
        {
            if (baseType.dimension > 0) return -1;
            if (subType.dimension > 0)
            {
                if (baseType == manager.kernelManager.ARRAY) return 1;
                else if (baseType == manager.kernelManager.HANDLE) return 2;
            }
            else if (subType.code == TypeCode.Delegate)
            {
                if (baseType == manager.kernelManager.DELEGATE) return 1;
                else if (baseType == manager.kernelManager.HANDLE) return 2;
            }
            else if (subType.code == TypeCode.Task)
            {
                if (baseType == manager.kernelManager.TASK) return 1;
                else if (baseType == manager.kernelManager.HANDLE) return 2;
            }
            else if (subType.code == TypeCode.Interface)
            {
                if (baseType == manager.kernelManager.HANDLE) return 2;
                else if (baseType.code == TypeCode.Interface) return manager.GetInterfaceInheritDeep(baseType, subType);
            }
            else if (subType.code == TypeCode.Handle && (baseType.code == TypeCode.Handle || baseType.code == TypeCode.Interface))
            {
                var depth = 0;
                var min = -1;
                if (manager.TryGetDeclaration(subType, out var declaration) && declaration is AbstractClass abstractClass)
                    foreach (var index in manager.GetInheritIterator(abstractClass))
                    {
                        if (baseType.code == TypeCode.Interface)
                            foreach (var inherit in index.inherits)
                            {
                                var deep = manager.GetInterfaceInheritDeep(baseType, inherit);
                                if (deep >= 0)
                                    if (min < 0 || depth + deep < min)
                                        min = depth + deep;
                            }
                        depth++;
                        if (index.parent == baseType) return depth;
                    }
                return min;
            }
            return -1;
        }
    }
}
