using LanguageServer.Parameters;

namespace RainLanguageServer
{
    public enum SemanticTokenType
    {
        Namespace,
        Type,
        Enum,
        EnumMember,
        Struct,
        Class,
        Interface,
        Function,
        Method,
        Const,
        Variable,
        Parameter,
        Operator,
        Number,
        Keyword,
        Label,

        LENGTH
    }
    public struct SemanticTokenRange(int line, int index, int length)
    {
        public int line = line;
        public int index = index;
        public int length = length;
    }
    public class SemanticToken(int type, SemanticTokenRange[] ranges)
    {
        public int type = type;
        public SemanticTokenRange[] ranges = ranges;
    }
    public class SemanticTokenParam(DocumentUri uri)
    {
        public DocumentUri uri = uri;
    }
    public class SemanticTokenCollector
    {
        public readonly List<SemanticTokenRange>?[] ranges = new List<SemanticTokenRange>[(int)SemanticTokenType.LENGTH];
        public void AddRange(SemanticTokenType type, SemanticTokenRange range) => (ranges[(int)type] ?? (ranges[(int)type] = [])).Add(range);
        internal void AddRange(SemanticTokenType type, TextRange range)
        {
            var line = range.start.Line;
            AddRange(type, new SemanticTokenRange(line.line, range.start - line.start, range.Count));
        }
        public SemanticToken[] GetResult()
        {
            var results = new List<SemanticToken>();
            for (int i = 0; i < ranges.Length; i++)
                if (ranges[i] != null)
                    results.Add(new SemanticToken(i, [.. ranges[i]]));
            return [.. results];
        }
    }
}
