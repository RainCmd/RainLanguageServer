namespace RainLanguageServer.RainLanguage2
{
    internal static class Utility
    {
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
        public static void Add<T>(this List<T> list, int index, T value)
        {
            if (index < list.Count) list[index] = value;
            else if (index == list.Count) list.Add(value);
            else throw new InvalidOperationException();
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
        public static Declaration GetDeclaration(Manager manager, AbstractLibrary library, Visibility visibility, DeclarationCategory category)
        {
            return new Declaration(library.library, visibility, category, manager.indexManager.GetIndex(library, category));
        }
    }
}
