
using RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis
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
            if (range.Count == 0) return new TupleExpression(range);
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
                            if (attribute.ContainAny(ExpressionAttribute.Method))
                            {
                                var expression = expressionStack.Pop();
                                if (expression is MethodExpression method)
                                {
                                    if (bracket.Valid)
                                    {
                                        if (TryGetFunction(method.range, method.callables, bracket.tuple, out var callable))
                                        {
                                            bracket = new BracketExpression(bracket.left, bracket.right, AssignmentConvert(bracket.expression, callable.signature));
                                            expression = new InvokerFunctionExpression(method.range & bracket.range, callable.returns, method.range, callable, bracket, manager.kernelManager);
                                            expressionStack.Push(expression);
                                            attribute = expression.attribute;
                                            goto label_next_lexical;
                                        }
                                        else collector.Add(method.range, ErrorLevel.Error, "未找到匹配的函数");
                                    }
                                }
                                else if (expression is MethodMemberExpression methodMember)
                                {
                                    if (bracket.Valid)
                                    {
                                        if (TryGetFunction(methodMember.range, methodMember.callables, bracket.tuple, out var callable))
                                        {
                                            bracket = new BracketExpression(bracket.left, bracket.right, AssignmentConvert(bracket.expression, callable.signature));
                                            expression = new InvokerMemberExpression(methodMember.range & bracket.range, callable.returns, methodMember.target, methodMember.symbol, methodMember.range, callable, bracket, manager.kernelManager);
                                            expressionStack.Push(expression);
                                            attribute = expression.attribute;
                                            goto label_next_lexical;
                                        }
                                        else collector.Add(methodMember.range, ErrorLevel.Error, "未找到匹配的函数");
                                    }
                                }
                                else if (expression is MethodVirtualExpression methodVirtual)
                                {
                                    if (bracket.Valid)
                                    {
                                        if (TryGetFunction(methodVirtual.range, methodVirtual.callables, bracket.tuple, out var callable))
                                        {
                                            bracket = new BracketExpression(bracket.left, bracket.right, AssignmentConvert(bracket.expression, callable.signature));
                                            expression = new InvokerVirtualExpression(methodVirtual.range & bracket.range, callable.returns, methodVirtual.target, methodVirtual.symbol, methodVirtual.range, callable, bracket, manager.kernelManager);
                                            expressionStack.Push(expression);
                                            attribute = expression.attribute;
                                            goto label_next_lexical;
                                        }
                                        else collector.Add(methodVirtual.range, ErrorLevel.Error, "未找到匹配的函数");
                                    }
                                }
                                else throw new Exception("未知的函数表达式：" + expression.GetType());
                                expressionStack.Push(new InvalidExpression(expression, bracket));
                                attribute = ExpressionAttribute.Invalid;
                            }
                            else if (attribute.ContainAny(ExpressionAttribute.Callable))
                            {
                                var expression = expressionStack.Pop();
                                if (!manager.TryGetDeclaration(expression.tuple[0], out var declaration)) throw new Exception("类型错误");
                                if (declaration is not AbstructDelegate abstructDelegate) throw new Exception("未知的可调用类型：" + declaration.GetType());
                                bracket = new BracketExpression(bracket.left, bracket.right, AssignmentConvert(bracket.expression, abstructDelegate.signature));
                                expression = new InvokerDelegateExpression(expression.range & bracket.range, abstructDelegate.returns, expression, bracket, manager.kernelManager);
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
                                        bracket = new BracketExpression(bracket.left, bracket.right, ConvertVectorParameter(bracket.expression, 2));
                                    expression = new VectorConstructorExpression(expression.range & bracket.range, type, bracket);
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                }
                                else if (type.type == manager.kernelManager.REAL3)
                                {
                                    if (bracket.tuple.Count > 0)
                                        bracket = new BracketExpression(bracket.left, bracket.right, ConvertVectorParameter(bracket.expression, 3));
                                    expression = new VectorConstructorExpression(expression.range & bracket.range, type, bracket);
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                }
                                else if (type.type == manager.kernelManager.REAL4)
                                {
                                    if (bracket.tuple.Count > 0)
                                        bracket = new BracketExpression(bracket.left, bracket.right, ConvertVectorParameter(bracket.expression, 4));
                                    expression = new VectorConstructorExpression(expression.range & bracket.range, type, bracket);
                                    expressionStack.Push(expression);
                                    attribute = expression.attribute;
                                }
                                else if (type.type.dimension == 0)
                                {
                                    if (type.type.code == TypeCode.Struct)
                                    {
                                        //todo 
                                    }
                                    else if (type.type.code == TypeCode.Handle)
                                    {

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
                                    expressionStack.Push(new InvalidExpression(expression, bracket));
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
                                collector.Add(lexical.anchor, ErrorLevel.Error, "无效的操作");
                                if (attribute == ExpressionAttribute.Invalid || attribute.ContainAny(ExpressionAttribute.Value | ExpressionAttribute.Tuple | ExpressionAttribute.Type))
                                    expressionStack.Push(new InvalidExpression(expressionStack.Pop(), bracket));
                                else
                                    expressionStack.Push(bracket.ToInvalid());
                                attribute = ExpressionAttribute.Invalid;
                            }
                            index = bracket.range.end;
                            goto label_next_lexical;
                        }
                    case LexicalType.BracketLeft1:
                        break;
                    case LexicalType.BracketLeft2:
                        break;
                    case LexicalType.BracketRight0:
                        break;
                    case LexicalType.BracketRight1:
                        break;
                    case LexicalType.BracketRight2:
                        break;
                    case LexicalType.Comma:
                        break;
                    case LexicalType.Semicolon:
                        break;
                    case LexicalType.Assignment:
                        break;
                    case LexicalType.Equals:
                        break;
                    case LexicalType.Lambda:
                        break;
                    case LexicalType.BitAnd:
                        break;
                    case LexicalType.LogicAnd:
                        break;
                    case LexicalType.BitAndAssignment:
                        break;
                    case LexicalType.BitOr:
                        break;
                    case LexicalType.LogicOr:
                        break;
                    case LexicalType.BitOrAssignment:
                        break;
                    case LexicalType.BitXor:
                        break;
                    case LexicalType.BitXorAssignment:
                        break;
                    case LexicalType.Less:
                        break;
                    case LexicalType.LessEquals:
                        break;
                    case LexicalType.ShiftLeft:
                        break;
                    case LexicalType.ShiftLeftAssignment:
                        break;
                    case LexicalType.Greater:
                        break;
                    case LexicalType.GreaterEquals:
                        break;
                    case LexicalType.ShiftRight:
                        break;
                    case LexicalType.ShiftRightAssignment:
                        break;
                    case LexicalType.Plus:
                        break;
                    case LexicalType.Increment:
                        break;
                    case LexicalType.PlusAssignment:
                        break;
                    case LexicalType.Minus:
                        break;
                    case LexicalType.Decrement:
                        break;
                    case LexicalType.RealInvoker:
                        break;
                    case LexicalType.MinusAssignment:
                        break;
                    case LexicalType.Mul:
                        break;
                    case LexicalType.MulAssignment:
                        break;
                    case LexicalType.Div:
                        break;
                    case LexicalType.DivAssignment:
                        break;
                    case LexicalType.Annotation:
                        break;
                    case LexicalType.Mod:
                        break;
                    case LexicalType.ModAssignment:
                        break;
                    case LexicalType.Not:
                        break;
                    case LexicalType.NotEquals:
                        break;
                    case LexicalType.Negate:
                        break;
                    case LexicalType.Dot:
                        break;
                    case LexicalType.Question:
                        break;
                    case LexicalType.QuestionDot:
                        break;
                    case LexicalType.QuestionRealInvoke:
                        break;
                    case LexicalType.QuestionInvoke:
                        break;
                    case LexicalType.QuestionIndex:
                        break;
                    case LexicalType.QuestionNull:
                        break;
                    case LexicalType.Colon:
                        break;
                    case LexicalType.ConstReal:
                        break;
                    case LexicalType.ConstNumber:
                        break;
                    case LexicalType.ConstBinary:
                        break;
                    case LexicalType.ConstHexadecimal:
                        break;
                    case LexicalType.ConstChars:
                        break;
                    case LexicalType.ConstString:
                        break;
                    case LexicalType.TemplateString:
                        break;
                    case LexicalType.Word:
                        break;
                    case LexicalType.Backslash:
                        break;
                    default:
                        break;
                }
                index = lexical.anchor.end;
            label_next_lexical:;
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
            return new QuestionNullExpression(symbol, leftExpression, rightExpression);
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
                    return new AssignmentExpression(left & right, symbol.anchor, leftExpression, rightExpression);
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
        private Expression InferLeftValueType(Expression expression, TypeSpan span)
        {
            if (!expression.Valid) return new InvalidExpression(expression, span);
            if (expression.tuple.Count != span.Count)
            {
                collector.Add(expression.range, ErrorLevel.Error, "类型数量不一致");
                return new InvalidExpression(expression, span);
            }
            else if (expression is BlurryVariableDeclarationExpression blurryVariable) return InferLeftValueType(blurryVariable, span[0]);
            else if (expression is TupleExpression tuple)
            {
                var expressions = new List<Expression>();
                var index = 0;
                foreach (var item in tuple.expressions)
                {
                    expressions.Add(InferLeftValueType(item, span[index..(index + item.tuple.Count)]));
                    index += item.tuple.Count;
                }
                return TupleExpression.Create(expressions, collector);
            }
            else if (ContainBlurry(expression.tuple))
            {
                if (expression is BracketExpression bracket)
                    return new BracketExpression(bracket.left, bracket.right, InferLeftValueType(bracket.expression, span));
                throw new Exception("表达式类型错误");
            }
            return expression;
        }
        private Expression InferLeftValueType(BlurryVariableDeclarationExpression blurry, Type type)
        {
            if (type == Expression.BLURRY || type == Expression.NULL)
            {
                collector.Add(blurry.range, ErrorLevel.Error, "表达式类型不明确");
                return new InvalidExpression(blurry, type);
            }
            return new VariableLocalExpression(blurry.range, localContext.Add(blurry.identifier, type), blurry.declaration, blurry.identifier, ExpressionAttribute.Assignable | ExpressionAttribute.Value, manager.kernelManager);
        }
        private Expression CreateOperation(TextRange range, string operation, TextRange symbol, params Expression[] expressions)
        {
            var parameters = TupleExpression.Create(expressions, collector);
            if (TryGetFunction(symbol, context.FindOperation(manager, operation), parameters, out var callable))
            {
                parameters = AssignmentConvert(parameters, callable.signature);
                return new OperationExpression(range, symbol, callable, parameters, manager.kernelManager);
            }
            else if (parameters.Valid) collector.Add(symbol, ErrorLevel.Error, "操作未找到");
            return new InvalidOperatorExpression(range, symbol, parameters);
        }
        private bool TryGetFunction(TextRange range, List<AbstractDeclaration> declarations, Expression parameters, [MaybeNullWhen(false)] out AbstractCallable result)
        {
            if (!parameters.Valid)
            {
                result = null;
                return false;
            }
            var results = new List<AbstractCallable>();
            var min = 0;
            var types = new List<Type>();
            foreach (var declaration in declarations)
                if (declaration is AbstractCallable callable && callable.signature.Count == parameters.tuple.Count)
                {
                    if (TryExplicitTypes(parameters, callable.signature, types))
                    {
                        var measure = Convert(parameters.tuple, new TypeSpan(types));
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
                    if (!manager.TryGetDeclaration(target, out var declaration) || declaration is not AbstructDelegate abstructDelegate) return false;
                    if (!TryGetFunction(method.range, method.callables, abstructDelegate.signature, out _)) return false;
                }
                else if (expression is MethodMemberExpression methodMember)
                {
                    if (target.dimension > 0) return false;
                    if (!manager.TryGetDeclaration(target, out var declaration) || declaration is not AbstructDelegate abstructDelegate) return false;
                    if (!TryGetFunction(methodMember.range, methodMember.callables, abstructDelegate.signature, out _)) return false;
                }
                else if (expression is MethodVirtualExpression methodVirtual)
                {
                    if (target.dimension > 0) return false;
                    if (!manager.TryGetDeclaration(target, out var declaration) || declaration is not AbstructDelegate abstructDelegate) return false;
                    if (!TryGetFunction(methodVirtual.range, methodVirtual.callables, abstructDelegate.signature, out _)) return false;
                }
                else if (expression is BlurryTaskExpression blurryTask)
                {
                    if (target.dimension > 0) return false;
                    if (!manager.TryGetDeclaration(target, out var declaration) || declaration is not AbstructTask abstructTask) return false;
                    if (blurryTask.invoker.tuple != abstructTask.returns) return false;
                }
                else if (expression is BlurryLambdaExpression blurryLambda)
                {
                    if (target.dimension > 0) return false;
                    if (!manager.TryGetDeclaration(target, out var declaration) || declaration is not AbstructDelegate abstructDelegate) return false;
                    localContext.PushBlock();
                    for (var i = 0; i < abstructDelegate.parameters.Count; i++)
                        localContext.Add(blurryLambda.parameters[i], abstructDelegate.signature[i], true);
                    var body = Parse(blurryLambda.body);
                    localContext.PopBlock();
                    if (!body.Valid) return false;
                    else if (abstructDelegate.returns.Count > 0)
                    {
                        if (abstructDelegate.returns != body.tuple)
                        {
                            body = AssignmentConvert(body, abstructDelegate.returns);
                            if (!body.Valid) return false;
                        }
                    }
                    else if (ContainBlurry(body)) return false;
                }
                result.Add(target);
            }
            return true;
        }
        private Expression AssignmentConvert(Expression source, TypeSpan span)
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
            return new TupleCastExpression(source, span, manager.kernelManager);
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
                }
                return TupleExpression.Create(expressions, collector);
            }
            else if (source.tuple.Count == 1) return InferRightValueType(source, span[0]);
            else if (ContainBlurry(source.tuple))
            {
                if (source is BracketExpression bracket)
                    return new BracketExpression(bracket.left, bracket.right, InferRightValueType(bracket.expression, span));
                throw new Exception("表达式类型错误");
            }
            return source;
        }
        private Expression InferRightValueType(Expression expression, Type type)
        {
            if (expression.tuple.Count == 1 && expression.tuple[0] == type) return expression;
            else if (!expression.Valid) return new InvalidExpression(expression, type);
            else if (type == Expression.BLURRY) collector.Add(expression.range, ErrorLevel.Error, "表达式类型名不明确");
            else if (expression is ConstNullExpression)
            {
                if (type == manager.kernelManager.ENTITY) return new ConstEntityNullExpression(expression.range, manager.kernelManager);
                else if (type.Managed) return new ConstHandleNullExpression(expression.range, type);
                collector.Add(expression.range, ErrorLevel.Error, "类型不匹配");
            }
            else if (expression is BlurrySetExpression blurrySet)
            {
                if (type.dimension > 0)
                {
                    var elementType = new Type(type, type.dimension - 1);
                    var elementTypes = new Type[blurrySet.expression.tuple.Count];
                    elementTypes.Fill(elementType);
                    var elements = AssignmentConvert(blurrySet.expression, elementTypes);
                    return new ArrayInitExpression(blurrySet.range, elements, type);
                }
                else collector.Add(expression.range, ErrorLevel.Error, "类型不匹配");
            }
            else if (expression is MethodExpression method)
            {
                if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstructDelegate abstructDelegate)
                {
                    if (TryGetFunction(expression.range, method.callables, abstructDelegate.signature, out var callable))
                    {
                        if (callable.returns != abstructDelegate.returns)
                            collector.Add(expression.range, ErrorLevel.Error, "返回值类型不一致");
                        return new FunctionDelegateCreateExpression(expression.range, type, callable, manager.kernelManager);
                    }
                }
                collector.Add(expression.range, ErrorLevel.Error, "无法转换为目标类型");
            }
            else if (expression is MethodMemberExpression methodMember)
            {
                if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstructDelegate abstructDelegate)
                {
                    if (TryGetFunction(expression.range, methodMember.callables, abstructDelegate.signature, out var callable))
                    {
                        if (callable.returns != abstructDelegate.returns)
                            collector.Add(expression.range, ErrorLevel.Error, "返回值类型不一致");
                        if (methodMember is MethodVirtualExpression)
                            return new VirtualFunctionDelegateCreateExpression(expression.range, type, callable, manager.kernelManager, methodMember.target, methodMember.symbol, methodMember.member);
                        else
                            return new MemberFunctionDelegateCreateExpression(expression.range, type, callable, manager.kernelManager, methodMember.target, methodMember.symbol, methodMember.member);
                    }
                }
                collector.Add(expression.range, ErrorLevel.Error, "无法转换为目标类型");
            }
            else if (expression is BlurryTaskExpression blurryTask)
            {
                if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstructTask abstructTask)
                {
                    if (blurryTask.invoker.tuple != abstructTask.returns)
                        collector.Add(expression.range, ErrorLevel.Error, "返回值类型不匹配");
                    return new TaskCreateExpression(expression.range, type, blurryTask.symbol, blurryTask.invoker, manager.kernelManager);
                }
                collector.Add(expression.range, ErrorLevel.Error, "无法转换为目标类型");
            }
            else if (expression is BlurryLambdaExpression blurryLambda)
            {
                if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstructDelegate abstructDelegate)
                {
                    if (blurryLambda.parameters.Count != abstructDelegate.parameters.Count)
                    {
                        collector.Add(expression.range, ErrorLevel.Error, "参数数量与委托类型参数数量不一致");
                        return expression;
                    }
                    localContext.PushBlock();
                    var parameters = new List<Local>();
                    for (var i = 0; i < blurryLambda.parameters.Count; i++)
                        parameters.Add(localContext.Add(blurryLambda.parameters[i], abstructDelegate.signature[i], true));
                    var body = Parse(blurryLambda.body);
                    localContext.PopBlock();
                    if (body.Valid && abstructDelegate.returns.Count > 0 && body.tuple != abstructDelegate.returns)
                        body = AssignmentConvert(body, abstructDelegate.returns);
                    return new LambdaDelegateCreateExpression(expression.range, type, abstructDelegate, manager.kernelManager, parameters, blurryLambda.symbol, body);
                }
                collector.Add(expression.range, ErrorLevel.Error, "无法转换为目标类型");
            }
            else if (expression is ConstExpression constExpression)
            {
                if (type == manager.kernelManager.REAL)
                {
                    if (constExpression is not ConstRealExpression && constExpression.TryEvaluate(out double value))
                        return new ConstRealExpression(expression.range, value, manager.kernelManager);
                }
                else if (type == manager.kernelManager.INT)
                {
                    if (constExpression is not ConstIntegerExpression && constExpression.TryEvaluate(out long value))
                        return new ConstIntegerExpression(expression.range, value, manager.kernelManager);
                }
                else if (type == manager.kernelManager.CHAR)
                {
                    if (constExpression is not ConstCharExpression && constExpression.TryEvaluate(out char value))
                        return new ConstCharExpression(expression.range, value, manager.kernelManager);
                }
            }
            return expression;
        }
        private bool TryGetFunction(TextRange range, List<AbstractCallable> callables, Tuple tuple, [MaybeNullWhen(false)] out AbstractCallable callable)
        {
            var results = new List<AbstractCallable>();
            var min = 0;
            foreach (var item in callables)
            {
                var measure = Convert(tuple, item.signature);
                if (measure >= 0)
                    if (results.Count == 0 || measure < min)
                    {
                        results.Clear();
                        min = measure;
                        results.Add(item);
                    }
                    else if (measure == min) results.Add(item);
            }
            callable = null;
            if (results.Count == 1) callable = results[0];
            else if (results.Count > 1)
            {
                var msg = new Message(range, ErrorLevel.Error, "语义不明确");
                foreach (var item in results)
                {
                    callable = item;
                    msg.related.Add(new RelatedInfo(item.name, "符合条件的函数"));
                }
                collector.Add(msg);
            }
            return callable != null;
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
                return new QuestionExpression(condition & value, symbol, lexical.anchor, conditionExpression, Parse(left), Parse(right));
            else
                return new QuestionExpression(condition & value, symbol, null, conditionExpression, Parse(value), null);
        }
        private BlurryLambdaExpression ParseLambda(TextRange parameters, TextRange symbol, TextRange body)
        {
            parameters = parameters.Trim;
            while (ExpressionSplit.Split(parameters, SplitFlag.Bracket0, out var left, out var right, collector).type == LexicalType.BracketRight0 && left.start == parameters.start && right.end == parameters.end)
                parameters = (left.end & right.start).Trim;
            var list = new List<TextRange>();
            while (ExpressionSplit.Split(parameters, SplitFlag.Comma | SplitFlag.Semicolon, out var left, out var right, collector).type != LexicalType.Unknow)
            {
                if (TryParseLambdaParameter(left, out left)) list.Add(left);
                parameters = right.Trim;
            }
            if (TryParseLambdaParameter(parameters, out parameters)) list.Add(parameters);
            return new BlurryLambdaExpression(parameters & body, list, symbol, body);
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
                expression = TupleExpression.Create(expressions, collector);
                return true;
            }
            expression = default;
            return false;
        }
        private BracketExpression ParseBracket(TextRange range, TextRange bracketLeft, SplitFlag flag)
        {
            if (ExpressionSplit.Split(range, flag, out var left, out var right, collector).type != LexicalType.Unknow)
                return new BracketExpression(left, right, Parse(left.end & right.start));
            collector.Add(bracketLeft, ErrorLevel.Error, "缺少配对的符号");
            return new BracketExpression(bracketLeft, range.end & range.end, Parse(bracketLeft.end & range.end));
        }
        private bool TryParseBracket(TextRange range, [MaybeNullWhen(false)] out Expression expression)
        {
            expression = default;
            if (range.Count == 0) return false;
            var lexical = ExpressionSplit.Split(range, SplitFlag.Bracket0, out var left, out var right, collector);
            if (lexical.type == LexicalType.BracketRight0 && left.start == range.start && left.end == range.end)
            {
                expression = new BracketExpression(left, right, Parse(left.end & right.start));
                return true;
            }
            return false;
        }
        private static bool ContainBlurry(Expression expression)
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
                if (source == manager.kernelManager.BYTE) return 0xff;
            }
            else if (target == manager.kernelManager.INT)
            {
                if (source == manager.kernelManager.BYTE || source == manager.kernelManager.CHAR) return 0xff;
                else if (source.dimension == 0 && source.code == TypeCode.Enum) return 0xfff;
            }
            else if (target == manager.kernelManager.REAL)
            {
                if (source == manager.kernelManager.BYTE || source == manager.kernelManager.CHAR || source == manager.kernelManager.INT) return 0xff;
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
                if (baseType == manager.kernelManager.INTERFACE) return 1;
                else if (baseType == manager.kernelManager.HANDLE) return 2;
                else if (baseType.code == TypeCode.Interface) return GetInterfaceInheritDeep(manager, baseType, subType);
            }
            else if (subType.code == TypeCode.Handle && (baseType.code == TypeCode.Handle || baseType.code == TypeCode.Interface))
            {
                var index = subType;
                var depth = 0;
                var min = -1;
                while (manager.TryGetDeclaration(index, out var declaration) && declaration is AbstractClass abstractClass)
                {
                    if (baseType.code == TypeCode.Interface)
                        foreach (var inherit in abstractClass.inherits)
                        {
                            var deep = GetInterfaceInheritDeep(manager, baseType, inherit);
                            if (deep >= 0)
                                if (min < 0 || depth + deep < min)
                                    min = depth + deep;
                        }
                    depth++;
                    index = abstractClass.parent;
                    if (index == baseType) return depth;
                }
                return min;
            }
            return -1;
        }
        private static int GetInterfaceInheritDeep(Manager manager, Type baseType, Type subType)
        {
            if (baseType == subType) return 0;
            if (manager.TryGetDeclaration(subType, out var declaration) && declaration is AbstractInterface abstractInterface)
            {
                var min = -1;
                foreach (var inherit in abstractInterface.inherits)
                {
                    var deep = GetInterfaceInheritDeep(manager, inherit, subType);
                    if (deep >= 0 && (deep < min || min < 0)) min = deep;
                }
                if (min >= 0) min++;
                return min;
            }
            return -1;
        }
    }
}
