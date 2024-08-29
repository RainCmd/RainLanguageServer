using System.Collections;

namespace RainLanguageServer.RainLanguage
{
    /// <summary>
    /// 编译错误等级
    /// </summary>
    internal enum ErrorLevel
    {
        Error,
        Warning,
        Info,
    }
    internal readonly struct RelatedInfo(TextRange range, string message)
    {
        public readonly TextRange range = range;
        public readonly string message = message;
    }
    internal readonly struct Message(TextRange range, ErrorLevel level, string message)
    {
        public readonly TextRange range = range;
        public readonly ErrorLevel level = level;
        public readonly string message = message;
        public readonly List<RelatedInfo> related = [];
        public Message(IList<TextRange> ranges, ErrorLevel level, string message) : this(new TextRange(ranges[0].start, ranges[^1].end), level, message) { }
        public void AddRelated(TextRange range, string message) => related.Add(new RelatedInfo(range, message));
    }
    internal class MessageCollector : IEnumerable<Message>
    {
        private readonly List<Message> messages = [];
        public void Add(Message message) => messages.Add(message);
        public void Add(TextRange range, ErrorLevel level, string message) => Add(new Message(range, level, message));
        public void Add(IList<TextRange> ranges, ErrorLevel level, string message) => Add(ranges[0].start & ranges[^1].end, level, message);
        public void Clear() => messages.Clear();

        public IEnumerator<Message> GetEnumerator()
        {
            return messages.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return GetEnumerator();
        }
    }
}
