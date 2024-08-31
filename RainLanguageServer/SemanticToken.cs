using LanguageServer.Parameters;

namespace RainLanguageServer
{
    //https://code.visualstudio.com/api/language-extensions/semantic-highlight-guide
    public enum SemanticTokenType
    {
        /// <summary>
        /// 对于声明或引用命名空间、模块或包的标识符。
        /// </summary>
        Namespace,
        /// <summary>
        /// 对于声明或引用类类型的标识符。
        /// </summary>
        Class,
        /// <summary>
        /// 对于声明或引用枚举类型的标识符。
        /// </summary>
        Enum,
        /// <summary>
        /// 对于声明或引用接口类型的标识符。
        /// </summary>
        Interface,
        /// <summary>
        /// 对于声明或引用 struct 类型的标识符。
        /// </summary>
        Struct,
        /// <summary>
        /// 对于声明或引用类型参数的标识符。
        /// </summary>
        TypeParameter,
        /// <summary>
        /// 对于声明或引用上面未涵盖的类型的标识符。
        /// </summary>
        Type,
        /// <summary>
        /// 对于声明或引用函数或方法参数的标识符。
        /// </summary>
        Parameter,
        /// <summary>
        /// 对于声明或引用局部或全局变量的标识符。
        /// </summary>
        Variable,
        /// <summary>
        /// 对于声明或引用成员属性、成员字段或成员变量的标识符。
        /// </summary>
        Property,
        /// <summary>
        /// 对于声明或引用枚举属性、常量或成员的标识符。
        /// </summary>
        EnumMember,
        /// <summary>
        /// 对于声明或引用装饰器和注解的标识符。
        /// </summary>
        Decorator,
        /// <summary>
        /// 对于声明事件属性的标识符。
        /// </summary>
        Event,
        /// <summary>
        /// 对于声明函数的标识符。
        /// </summary>
        Function,
        /// <summary>
        /// 对于声明成员函数或方法的标识符。
        /// </summary>
        Method,
        /// <summary>
        /// 对于声明宏的标识符。
        /// </summary>
        Macro,
        /// <summary>
        /// 对于声明标签的标识符。
        /// </summary>
        Label,
        /// <summary>
        /// 对于表示注释的标记。
        /// </summary>
        Comment,
        /// <summary>
        /// 对于表示字符串文本的标记。
        /// </summary>
        String,
        /// <summary>
        /// 对于表示 language 关键字的标记。
        /// </summary>
        Keyword,
        /// <summary>
        /// 对于表示数字文本的标记。
        /// </summary>
        Number,
        /// <summary>
        /// 对于表示正则表达式文本的标记。
        /// </summary>
        Regexp,
        /// <summary>
        /// 对于表示运算符的令牌。
        /// </summary>
        Operator,

        LENGTH
    }
    public enum SemanticTokenModifier
    {
        /// <summary>
        /// 对于符号的声明。
        /// </summary>
        Declaration,
        /// <summary>
        /// 对于元件的定义，例如，在头文件中。
        /// </summary>
        Definition,
        /// <summary>
        /// 对于 readonly 变量和成员字段（常量）。
        /// </summary>
        Readonly,
        /// <summary>
        /// 对于类成员 （静态成员）。
        /// </summary>
        Static,
        /// <summary>
        /// 对于不应再使用的元件。
        /// </summary>
        Deprecated,
        /// <summary>
        /// 对于抽象的类型和成员函数。
        /// </summary>
        Abstract,
        /// <summary>
        /// 对于标记为 async.
        /// </summary>
        Async,
        /// <summary>
        /// 对于变量引用，变量被分配到其中。
        /// </summary>
        Modification,
        /// <summary>
        /// 对于文档中出现的符号。
        /// </summary>
        Documentation,
        /// <summary>
        /// 对于属于标准库的元件。
        /// </summary>
        DefaultLibrary,

        LENGTH
    }
    public struct SemanticTokenRange(int line, int index, int length)
    {
        public int line = line;
        public int index = index;
        public int length = length;
    }
    public class SemanticToken(int type, int modifier, SemanticTokenRange[] ranges)
    {
        public int type = type;
        public int modifier = modifier;
        public SemanticTokenRange[] ranges = ranges;
    }
    public class SemanticTokenParam(DocumentUri uri)
    {
        public DocumentUri uri = uri;
    }
    public class SemanticTokenCollector
    {
        private readonly List<SemanticTokenRange>?[,] ranges = new List<SemanticTokenRange>[(int)SemanticTokenType.LENGTH, (int)SemanticTokenModifier.LENGTH];
        internal void AddRange(SemanticTokenType type, SemanticTokenModifier modifier, TextRange range)
        {
            var line = range.start.Line;
            (ranges[(int)type, (int)modifier] ??= []).Add(new SemanticTokenRange(line.line, range.start - line.start, range.Count));
        }
        public SemanticToken[] GetResult()
        {
            var results = new List<SemanticToken>();
            for (int x = 0; x < (int)SemanticTokenType.LENGTH; x++)
                for (int y = 0; y < (int)SemanticTokenModifier.LENGTH; y++)
                    if (ranges[x, y] != null)
                        results.Add(new SemanticToken(x, y, [.. ranges[x, y]]));
            return [.. results];
        }
    }
}
