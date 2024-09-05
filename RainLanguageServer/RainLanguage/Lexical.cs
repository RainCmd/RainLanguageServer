namespace RainLanguageServer.RainLanguage
{
    internal enum LexicalType
    {
        Unknow,

        BracketLeft0,           // (
        BracketLeft1,           // [
        BracketLeft2,           // {
        BracketRight0,          // )
        BracketRight1,          // ]
        BracketRight2,          // }
        Comma,                  // ,
        Semicolon,              // ;
        Assignment,             // =
        Equals,                 // ==
        Lambda,                 // =>
        BitAnd,                 // &
        LogicAnd,               // &&
        BitAndAssignment,       // &=
        BitOr,                  // |
        LogicOr,                // ||
        BitOrAssignment,        // |=
        BitXor,                 // ^
        BitXorAssignment,       // ^=
        Less,                   // <
        LessEquals,             // <=
        ShiftLeft,              // <<
        ShiftLeftAssignment,    // <<=
        Greater,                // >
        GreaterEquals,          // >=
        ShiftRight,             // >>
        ShiftRightAssignment,   // >>=
        Plus,                   // +
        Increment,              // ++
        PlusAssignment,         // +=
        Minus,                  // -
        Decrement,              // --
        RealInvoker,            // ->
        MinusAssignment,        // -=
        Mul,                    // *
        MulAssignment,          // *=
        Div,                    // /
        DivAssignment,          // /=
        Annotation,             // 注释
        Mod,                    // %
        ModAssignment,          // %=
        Not,                    // !
        NotEquals,              // !=
        Negate,                 // ~
        Dot,                    // .
        Question,               // ?
        QuestionDot,            // ?.
        QuestionRealInvoke,     // ?->
        QuestionInvoke,         // ?(
        QuestionIndex,          // ?[
        QuestionNull,           // ??
        Colon,                  // :
        ConstReal,              // 数字(实数)
        ConstNumber,            // 数字(整数)
        ConstBinary,            // 数字(二进制)
        ConstHexadecimal,       // 数字(十六进制)
        ConstChars,             // 数字(单引号字符串)
        ConstString,            // 字符串
        TemplateString,         // 模板字符串
        Word,                   // 单词
        Backslash,              // 反斜杠
    }
    internal readonly struct Lexical(TextRange anchor, LexicalType type)
    {
        public readonly TextRange anchor = anchor;
        public readonly LexicalType type = type;
        public bool IsReloadable
        {
            get
            {
                switch (type)
                {
                    case LexicalType.Unknow:
                    case LexicalType.BracketLeft0:
                    case LexicalType.BracketLeft1:
                    case LexicalType.BracketLeft2:
                    case LexicalType.BracketRight0:
                    case LexicalType.BracketRight1:
                    case LexicalType.BracketRight2:
                    case LexicalType.Comma:
                    case LexicalType.Semicolon:
                    case LexicalType.Assignment: break;
                    case LexicalType.Equals: return true;
                    case LexicalType.Lambda: break;
                    case LexicalType.BitAnd: return true;
                    case LexicalType.LogicAnd:
                    case LexicalType.BitAndAssignment: break;
                    case LexicalType.BitOr: return true;
                    case LexicalType.LogicOr:
                    case LexicalType.BitOrAssignment: break;
                    case LexicalType.BitXor: return true;
                    case LexicalType.BitXorAssignment: break;
                    case LexicalType.Less:
                    case LexicalType.LessEquals:
                    case LexicalType.ShiftLeft: return true;
                    case LexicalType.ShiftLeftAssignment: break;
                    case LexicalType.Greater:
                    case LexicalType.GreaterEquals:
                    case LexicalType.ShiftRight: return true;
                    case LexicalType.ShiftRightAssignment: break;
                    case LexicalType.Plus:
                    case LexicalType.Increment: return true;
                    case LexicalType.PlusAssignment: break;
                    case LexicalType.Minus:
                    case LexicalType.Decrement: return true;
                    case LexicalType.RealInvoker:
                    case LexicalType.MinusAssignment: break;
                    case LexicalType.Mul: return true;
                    case LexicalType.MulAssignment: break;
                    case LexicalType.Div: return true;
                    case LexicalType.DivAssignment:
                    case LexicalType.Annotation: break;
                    case LexicalType.Mod: return true;
                    case LexicalType.ModAssignment: break;
                    case LexicalType.Not:
                    case LexicalType.NotEquals:
                    case LexicalType.Negate: return true;
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
                    default:
                        break;
                }
                return false;
            }
        }
        private static bool IsLetter(int ch)
        {
            if (ch == '_' || ch > 128) return true;
            ch |= 0x20;
            return ch >= 'a' && ch <= 'z';
        }
        public static void MatchBlock(TextRange segment, LexicalType leftType, LexicalType rightType, out TextRange left, out TextRange right, MessageCollector? collector)
        {
            if (!TryAnalysis(segment, 0, out var lexical, collector) || lexical.type != leftType) throw new Exception("必须保留左括号");
            left = lexical.anchor;
            var index = lexical.anchor.end;
            var deep = 1;
            while (TryAnalysis(segment, index, out lexical, collector))
            {
                index = lexical.anchor.end;
                if (lexical.type == leftType) deep++;
                else if (lexical.type == rightType)
                    if (--deep == 0)
                    {
                        right = lexical.anchor;
                        return;
                    }
            }
            collector?.Add(left, ErrorLevel.Error, "缺少配对的符号");
            right = segment.end & segment.end;
        }

        public static bool TryAnalysis(TextRange segment, TextPosition index, out Lexical lexical, MessageCollector? collector) => TryAnalysis(segment, index - segment.start, out lexical, collector);
        public static bool TryAnalysis(TextRange segment, int index, out Lexical lexical, MessageCollector? collector)
        {
            while (index < segment.Count && char.IsWhiteSpace(segment[index])) index++;
            if (index < segment.Count)
            {
                segment = segment[index..];
                switch (segment[0])
                {
                    case '(':
                        lexical = new Lexical(segment[..1], LexicalType.BracketLeft0);
                        return true;
                    case '[':
                        lexical = new Lexical(segment[..1], LexicalType.BracketLeft1);
                        return true;
                    case '{':
                        lexical = new Lexical(segment[..1], LexicalType.BracketLeft2);
                        return true;
                    case ')':
                        lexical = new Lexical(segment[..1], LexicalType.BracketRight0);
                        return true;
                    case ']':
                        lexical = new Lexical(segment[..1], LexicalType.BracketRight1);
                        return true;
                    case '}':
                        lexical = new Lexical(segment[..1], LexicalType.BracketRight2);
                        return true;
                    case ',':
                        lexical = new Lexical(segment[..1], LexicalType.Comma);
                        return true;
                    case ';':
                        lexical = new Lexical(segment[..1], LexicalType.Semicolon);
                        return true;
                    case '=':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.Equals);
                        else if (segment[1] == '>') lexical = new Lexical(segment[..2], LexicalType.Lambda);
                        else lexical = new Lexical(segment[..1], LexicalType.Assignment);
                        return true;
                    case '&':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.BitAndAssignment);
                        else if (segment[1] == '&') lexical = new Lexical(segment[..2], LexicalType.LogicAnd);
                        else lexical = new Lexical(segment[..1], LexicalType.BitAnd);
                        return true;
                    case '|':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.BitOrAssignment);
                        else if (segment[1] == '|') lexical = new Lexical(segment[..2], LexicalType.LogicOr);
                        else lexical = new Lexical(segment[..1], LexicalType.BitOr);
                        return true;
                    case '^':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.BitXorAssignment);
                        else lexical = new Lexical(segment[..1], LexicalType.BitXor);
                        return true;
                    case '<':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.LessEquals);
                        else if (segment[1] == '<')
                        {
                            if (segment[2] == '=') lexical = new Lexical(segment[..3], LexicalType.ShiftLeftAssignment);
                            else lexical = new Lexical(segment[..2], LexicalType.ShiftLeft);
                        }
                        else lexical = new Lexical(segment[..1], LexicalType.Less);
                        return true;
                    case '>':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.GreaterEquals);
                        else if (segment[1] == '>')
                        {
                            if (segment[1] == '=') lexical = new Lexical(segment[..3], LexicalType.ShiftRightAssignment);
                            else lexical = new Lexical(segment[..2], LexicalType.ShiftRight);
                        }
                        else lexical = new Lexical(segment[..1], LexicalType.Greater);
                        return true;
                    case '+':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.PlusAssignment);
                        else if (segment[1] == '+') lexical = new Lexical(segment[..2], LexicalType.Increment);
                        else lexical = new Lexical(segment[..1], LexicalType.Plus);
                        return true;
                    case '-':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.MinusAssignment);
                        else if (segment[1] == '-') lexical = new Lexical(segment[..2], LexicalType.Decrement);
                        else if (segment[1] == '>') lexical = new Lexical(segment[..2], LexicalType.RealInvoker);
                        else lexical = new Lexical(segment[..1], LexicalType.Minus);
                        return true;
                    case '*':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.MulAssignment);
                        else lexical = new Lexical(segment[..1], LexicalType.Mul);
                        return true;
                    case '/':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.DivAssignment);
                        else if (segment[1] == '/')
                        {
                            lexical = new Lexical(segment, LexicalType.Annotation);
                            return false;
                        }
                        else lexical = new Lexical(segment[..1], LexicalType.Div);
                        return true;
                    case '%':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.ModAssignment);
                        else lexical = new Lexical(segment[..1], LexicalType.Mod);
                        return true;
                    case '!':
                        if (segment[1] == '=') lexical = new Lexical(segment[..2], LexicalType.NotEquals);
                        else lexical = new Lexical(segment[..1], LexicalType.Not);
                        return true;
                    case '~':
                        lexical = new Lexical(segment[..1], LexicalType.Negate);
                        return true;
                    case '.':
                        if (char.IsDigit(segment[1]))
                        {
                            index = 2;
                            while (segment[index] == '_' || char.IsDigit(segment[index])) index++;
                            lexical = new Lexical(segment[..index], LexicalType.ConstReal);
                            return true;
                        }
                        else lexical = new Lexical(segment[..1], LexicalType.Dot);
                        return true;
                    case '?':
                        if (segment[1] == '.') lexical = new Lexical(segment[..2], LexicalType.QuestionDot);
                        else if (segment[1] == '(') lexical = new Lexical(segment[..2], LexicalType.QuestionInvoke);
                        else if (segment[1] == '[') lexical = new Lexical(segment[..2], LexicalType.QuestionIndex);
                        else if (segment[1] == '?') lexical = new Lexical(segment[..2], LexicalType.QuestionNull);
                        else if (segment[1] == '-' && segment[2] == '>') lexical = new Lexical(segment[..3], LexicalType.QuestionRealInvoke);
                        else lexical = new Lexical(segment[..1], LexicalType.Question);
                        return true;
                    case ':':
                        lexical = new Lexical(segment[..1], LexicalType.Colon);
                        return true;
                    case '\'':
                        index = 1;
                        while (index < segment.Count)
                        {
                            if (segment[index] == '\'')
                            {
                                lexical = new Lexical(segment[..(index + 1)], LexicalType.ConstChars);
                                return true;
                            }
                            else if (segment[index] == '\\')
                            {
                                index++;
                                if (index >= segment.Count) break;
                            }
                            index++;
                        }
                        lexical = new Lexical(segment[..index], LexicalType.ConstChars);
                        collector?.Add(segment[..index], ErrorLevel.Error, "缺少配对的符号");
                        return true;
                    case '\"':
                        index = 1;
                        while (index < segment.Count)
                        {
                            if (segment[index] == '\"')
                            {
                                lexical = new Lexical(segment[..(index + 1)], LexicalType.ConstString);
                                return true;
                            }
                            else if (segment[index] == '\\')
                            {
                                index++;
                                if (index >= segment.Count) break;
                            }
                            index++;
                        }
                        lexical = new Lexical(segment[..index], LexicalType.ConstString);
                        collector?.Add(segment[..index], ErrorLevel.Error, "缺少配对的符号");
                        return true;
                    case '$':
                        if (segment[1] == '\"')
                        {
                            index = 2;
                            while (index < segment.Count)
                            {
                                if (segment[index] == '\"')
                                {
                                    lexical = new Lexical(segment[..(index + 1)], LexicalType.TemplateString);
                                    return true;
                                }
                                else if (segment[index] == '\\')
                                {
                                    index++;
                                    if (index >= segment.Count) break;
                                    index++;
                                }
                                else if (segment[index] == '{')
                                {
                                    if (index + 1 > segment.Count)
                                    {
                                        index++;
                                        break;
                                    }
                                    else if (segment[index + 1] == '{') index += 2;
                                    else
                                    {
                                        MatchBlock(segment[index..], LexicalType.BracketLeft2, LexicalType.BracketRight2, out _, out var end, collector);
                                        index = end.end - segment.start;
                                    }
                                }
                                else index++;
                            }
                            lexical = new Lexical(segment[..index], LexicalType.TemplateString);
                            collector?.Add(segment[..index], ErrorLevel.Error, "缺少配对的符号");
                        }
                        else lexical = new Lexical(segment[..1], LexicalType.Unknow);
                        return true;
                    case '\\':
                        lexical = new Lexical(segment[..1], LexicalType.Backslash);
                        return true;
                    default:
                        if (char.IsDigit(segment[0]))
                        {
                            if (segment[0] == '0')
                            {
                                char sign = (char)(segment[1] | 0x20);
                                if (sign == 'b')
                                {
                                    index = 2;
                                    while (char.IsDigit(segment[index]) || segment[index] == '_') index++;
                                    lexical = new Lexical(segment[..index], LexicalType.ConstBinary);
                                    return true;
                                }
                                else if (sign == 'x')
                                {
                                    index = 2;
                                    while (char.IsAsciiHexDigit(segment[index]) || segment[index] == '_') index++;
                                    lexical = new Lexical(segment[..index], LexicalType.ConstHexadecimal);
                                    return true;
                                }
                            }
                            var dot = false;
                            index = 1;
                            while (true)
                            {
                                var symbol = segment[index++];
                                if (char.IsDigit(symbol) || symbol == '_') continue;
                                else if (symbol == '.')
                                {
                                    if (dot)
                                    {
                                        lexical = new Lexical(segment[..(index - 1)], LexicalType.ConstReal);
                                        return true;
                                    }
                                    else if (!char.IsDigit(segment[index]))
                                    {
                                        lexical = new Lexical(segment[..(index - 1)], LexicalType.ConstNumber);
                                        return true;
                                    }
                                    dot = true;
                                }
                                else
                                {
                                    lexical = new Lexical(segment[..(index - 1)], dot ? LexicalType.ConstReal : LexicalType.ConstNumber);
                                    return true;
                                }
                            }
                        }
                        else if (IsLetter(segment[0]))
                        {
                            index = 1;
                            while (IsLetter(segment[index]) || char.IsDigit(segment[index])) index++;
                            lexical = new Lexical(segment[..index], LexicalType.Word);
                            return true;
                        }
                        else
                        {
                            index = 1;
                            while (!char.IsWhiteSpace(segment[index])) index++;
                            lexical = new Lexical(segment[..index], LexicalType.Unknow);
                            return true;
                        }
                }
            }
            lexical = default;
            return false;
        }

        public static bool TryExtractName(TextRange segment, TextPosition index, out List<TextRange> names, MessageCollector? collector) => TryExtractName(segment, index - segment.start, out names, collector);
        public static bool TryExtractName(TextRange segment, int index, out List<TextRange> names, MessageCollector? collector)
        {
            names = [];
            while (TryAnalysis(segment, index, out var lexical, collector))
            {
                if (lexical.type == LexicalType.Word) names.Add(lexical.anchor);
                else break;
                index = lexical.anchor.end - segment.start;
                if (TryAnalysis(segment, index, out lexical, collector) && lexical.type == LexicalType.Dot)
                    index = lexical.anchor.end - segment.start;
                else return true;
            }
            return false;
        }

        public static int ExtractDimension(TextRange segment, ref int index)
        {
            var result = 0;
            while (true)
            {
                if (!TryAnalysis(segment, index, out var lexical, null) || lexical.type != LexicalType.BracketLeft1) break;
                if (!TryAnalysis(segment, lexical.anchor.end, out lexical, null) || lexical.type != LexicalType.BracketRight1) break;
                index = lexical.anchor.end - segment.start;
                result++;
            }
            return result;
        }
        public static int ExtractDimension(TextRange segment, ref TextPosition index)
        {
            var position = index - segment.start;
            var result = ExtractDimension(segment, ref position);
            index = segment.start + position;
            return result;
        }
    }
}
