namespace RainLanguageServer.RainLanguage
{
    internal static class Utility
    {
        public static bool TryToHexNumber(this char value, out int number)
        {
            if (value >= '0' && value <= '9')
            {
                number = value - '0';
                return true;
            }
            else
            {
                number = value | 0x20;
                if (number is >= 'a' and <= 'f')
                {
                    number -= 'a' - 10;
                    return true;
                }
            }
            number = 0;
            return false;
        }
        public static char EscapeCharacter(TextRange range, ref int index)
        {
            if (range[index] != '\\') throw new Exception("需要保留转义字符");
            var c = range[++index];
            if (c == 'a') c = '\a';
            else if (c == 'b') c = '\b';
            else if (c == 'f') c = '\f';
            else if (c == 'n') c = '\n';
            else if (c == 'r') c = '\r';
            else if (c == 't') c = '\t';
            else if (c == 'v') c = '\v';
            else if (c == '0') c = '\0';
            else if (c == 'x')
            {
                if (range[index + 1].TryToHexNumber(out var hNum) && range[index + 2].TryToHexNumber(out var lNum))
                {
                    index += 2;
                    return (char)(hNum * 16 + lNum);
                }
            }
            else if (c == 'u')
            {
                if (index + 4 < range.Count)
                {
                    var value = 0;
                    for (int i = 0;i<4;i++)
                    {
                        value <<= 4;
                        if (range[index + i + 1].TryToHexNumber(out var number)) value += number;
                        else return c;
                    }
                    index += 4;
                    return (char)value;
                }
            }
            return c;
        }
        public static List<R> Select<T, R>(this List<T> list) where R : T
        {
            var results = new List<R>();
            foreach (var item in list)
                if (item is R value)
                    results.Add(value);
            return results;
        }
        public static void Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++) array[i] = value;
        }
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> values)
        {
            foreach (var value in values) set.Add(value);
        }
        public static void Add<TKey, TElement>(this Dictionary<TKey, List<TElement>> dictionary, TKey key, TElement element) where TKey : notnull
        {
            if (!dictionary.TryGetValue(key, out var list))
            {
                list = [];
                dictionary[key] = list;
            }
            list.Add(element);
        }
        public static T RemoveAt<T>(this List<T> list, Index index)
        {
            var offset = index.GetOffset(list.Count);
            var result = list[offset];
            list.RemoveAt(offset);
            return result;
        }
        public static string Format(this string text, params object[] parameters) => string.Format(text, parameters);
        public static bool IsReloadable(this LexicalType type)
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
        public static bool IsValidName(TextRange name, bool allowKeyword, bool operatorReloadable, MessageCollector collector)
        {
            if (name == KeyWords.DISCARD_VARIABLE)
            {
                collector.Add(name, ErrorLevel.Error, "不能已单个下划线作为名称");
                return false;
            }
            if (Lexical.TryAnalysis(name, 0, out var lexical, null))
            {
                if (lexical.type == LexicalType.Word)
                {
                    if (allowKeyword && !KeyWords.IsKeyWorld(name.ToString())) return true;
                    collector.Add(name, ErrorLevel.Error, "关键字不能作为名称");
                    return false;
                }
                else if (operatorReloadable && lexical.type.IsReloadable()) return true;
            }
            collector.Add(name, ErrorLevel.Error, "名称不是合法的标识符");
            return false;
        }
    }
}
