namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis
{
    internal enum SplitFlag
    {
        //                      left					right
        Bracket0 = 0x001,       //(						)
        Bracket1 = 0x002,       //[						]
        Bracket2 = 0x004,       //{						}
        Comma = 0x008,          //分隔符左边内容		分隔符右边内容
        Semicolon = 0x010,      //分隔符左边内容		分隔符右边内容
        Assignment = 0x020,     //分隔符左边内容		分隔符右边内容
        Question = 0x040,       //分隔符左边内容		分隔符右边内容
        Colon = 0x080,          //分隔符左边内容		分隔符右边内容
        Lambda = 0x100,         //分隔符左边内容		分隔符右边内容
        QuestionNull = 0x200,	//分隔符左边内容		分隔符右边内容
    }
    internal static class ExpressionSplit
    {
        public static bool ContainAny(this SplitFlag flag, SplitFlag value)
        {
            return (flag & value) != 0;
        }
        public static Lexical Split(TextRange range, SplitFlag flag, out TextRange left, out TextRange right, MessageCollector collector)
        {
            var stack = new Stack<Lexical>();
            for (var index = 0; Lexical.TryAnalysis(range, index, out var lexical, collector); index = lexical.anchor.end - range.start)
                switch (lexical.type)
                {
                    case LexicalType.Unknow: break;
                    case LexicalType.BracketLeft0:
                    case LexicalType.BracketLeft1:
                    case LexicalType.BracketLeft2:
                        stack.Push(lexical);
                        break;
                    case LexicalType.BracketRight0:
                        {
                            var matched = false;
                            while (stack.Count > 0)
                            {
                                var bracket = stack.Pop();
                                if (bracket.type == LexicalType.BracketLeft0 || lexical.type == LexicalType.QuestionInvoke)
                                {
                                    if (stack.Count == 0 && flag.ContainAny(SplitFlag.Bracket0))
                                    {
                                        left = bracket.anchor;
                                        right = lexical.anchor;
                                        return lexical;
                                    }
                                    matched = true;
                                    break;
                                }
                                else collector.Add(bracket.anchor, ErrorLevel.Error, "缺少配对的符号");
                            }
                            if (!matched) collector.Add(lexical.anchor, ErrorLevel.Error, "缺少配对的符号");
                        }
                        break;
                    case LexicalType.BracketRight1:
                        {
                            var matched = false;
                            while (stack.Count > 0)
                            {
                                var bracket = stack.Pop();
                                if (bracket.type == LexicalType.BracketLeft1 || lexical.type == LexicalType.QuestionIndex)
                                {
                                    if (stack.Count == 0 && flag.ContainAny(SplitFlag.Bracket1))
                                    {
                                        left = bracket.anchor;
                                        right = lexical.anchor;
                                        return lexical;
                                    }
                                    matched = true;
                                    break;
                                }
                                else collector.Add(bracket.anchor, ErrorLevel.Error, "缺少配对的符号");
                            }
                            if (!matched) collector.Add(lexical.anchor, ErrorLevel.Error, "缺少配对的符号");
                        }
                        break;
                    case LexicalType.BracketRight2:
                        {
                            var matched = false;
                            while (stack.Count > 0)
                            {
                                var bracket = stack.Pop();
                                if (bracket.type == LexicalType.BracketLeft2)
                                {
                                    if (stack.Count == 0 && flag.ContainAny(SplitFlag.Bracket2))
                                    {
                                        left = bracket.anchor;
                                        right = lexical.anchor;
                                        return lexical;
                                    }
                                    matched = true;
                                    break;
                                }
                                else collector.Add(bracket.anchor, ErrorLevel.Error, "缺少配对的符号");
                            }
                            if (!matched) collector.Add(lexical.anchor, ErrorLevel.Error, "缺少配对的符号");
                        }
                        break;
                    case LexicalType.Comma:
                        if (stack.Count == 0 && flag.ContainAny(SplitFlag.Comma))
                        {
                            left = range[..index];
                            right = range[lexical.anchor.end.charactor..];
                            return lexical;
                        }
                        break;
                    case LexicalType.Semicolon:
                        if (stack.Count == 0 && flag.ContainAny(SplitFlag.Semicolon))
                        {
                            left = range[..index];
                            right = range[lexical.anchor.end.charactor..];
                            return lexical;
                        }
                        break;
                    case LexicalType.Assignment:
                        if (stack.Count == 0 && flag.ContainAny(SplitFlag.Assignment))
                        {
                            left = range[..index];
                            right = range[lexical.anchor.end.charactor..];
                            return lexical;
                        }
                        break;
                    case LexicalType.Equals: break;
                    case LexicalType.Lambda:
                        if (stack.Count == 0 && flag.ContainAny(SplitFlag.Lambda))
                        {
                            left = range[..index];
                            right = range[lexical.anchor.end.charactor..];
                            return lexical;
                        }
                        break;
                    case LexicalType.BitAnd:
                    case LexicalType.LogicAnd: break;
                    case LexicalType.BitAndAssignment: goto case LexicalType.Assignment;
                    case LexicalType.BitOr:
                    case LexicalType.LogicOr: break;
                    case LexicalType.BitOrAssignment: goto case LexicalType.Assignment;
                    case LexicalType.BitXor: break;
                    case LexicalType.BitXorAssignment: goto case LexicalType.Assignment;
                    case LexicalType.Less:
                    case LexicalType.LessEquals:
                    case LexicalType.ShiftLeft: break;
                    case LexicalType.ShiftLeftAssignment: goto case LexicalType.Assignment;
                    case LexicalType.Greater:
                    case LexicalType.GreaterEquals:
                    case LexicalType.ShiftRight: break;
                    case LexicalType.ShiftRightAssignment: goto case LexicalType.Assignment;
                    case LexicalType.Plus:
                    case LexicalType.Increment: break;
                    case LexicalType.PlusAssignment: goto case LexicalType.Assignment;
                    case LexicalType.Minus:
                    case LexicalType.Decrement:
                    case LexicalType.RealInvoker: break;
                    case LexicalType.MinusAssignment: goto case LexicalType.Assignment;
                    case LexicalType.Mul: break;
                    case LexicalType.MulAssignment: goto case LexicalType.Assignment;
                    case LexicalType.Div: break;
                    case LexicalType.DivAssignment: goto case LexicalType.Assignment;
                    case LexicalType.Annotation:
                    case LexicalType.Mod: break;
                    case LexicalType.ModAssignment: goto case LexicalType.Assignment;
                    case LexicalType.Not:
                    case LexicalType.NotEquals:
                    case LexicalType.Negate:
                    case LexicalType.Dot: break;
                    case LexicalType.Question:
                        if (stack.Count == 0 && flag.ContainAny(SplitFlag.Question))
                        {
                            left = range[..index];
                            right = range[lexical.anchor.end.charactor..];
                            return lexical;
                        }
                        break;
                    case LexicalType.QuestionDot:
                    case LexicalType.QuestionRealInvoke: break;
                    case LexicalType.QuestionInvoke:
                    case LexicalType.QuestionIndex:
                        stack.Push(lexical);
                        break;
                    case LexicalType.QuestionNull:
                        if (stack.Count == 0 && flag.ContainAny(SplitFlag.QuestionNull))
                        {
                            left = range[..index];
                            right = range[lexical.anchor.end.charactor..];
                            return lexical;
                        }
                        break;
                    case LexicalType.Colon:
                        if (stack.Count > 0)
                        {
                            if (stack.Pop().type == LexicalType.Question) break;
                        }
                        else if (flag.ContainAny(SplitFlag.Colon))
                        {
                            left = range[..index];
                            right = range[lexical.anchor.end.charactor..];
                            return lexical;
                        }
                        break;
                    case LexicalType.ConstReal:
                    case LexicalType.ConstNumber:
                    case LexicalType.ConstBinary:
                    case LexicalType.ConstHexadecimal:
                    case LexicalType.ConstChars:
                    case LexicalType.ConstString:
                    case LexicalType.TemplateString:
                    case LexicalType.Word:
                    case LexicalType.Backslash: break;
                }
            left = right = default;
            return default;
        }
    }
}
