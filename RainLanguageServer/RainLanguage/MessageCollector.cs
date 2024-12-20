﻿using System.Collections;

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
        Hint
    }
    internal readonly struct RelatedInfo(TextRange range, string message)
    {
        public readonly TextRange range = range;
        public readonly string message = message;
    }
    internal readonly struct Message(TextRange range, ErrorLevel level, string message, bool unnecessary = false)
    {
        public readonly TextRange range = range;
        public readonly ErrorLevel level = level;
        public readonly string message = message;
        public readonly bool unnecessary = unnecessary;
        public readonly List<RelatedInfo> related = [];
        public Message(IList<TextRange> ranges, ErrorLevel level, string message) : this(new TextRange(ranges[0].start, ranges[^1].end), level, message) { }
        public void AddRelated(TextRange range, string message) => related.Add(new RelatedInfo(range, message));
        public override string ToString() => $"[{level}]{range}: {message}";
    }
    internal class MessageCollector : IEnumerable<Message>
    {
        private readonly List<Message> messages = [];
        public void Add(Message message) => messages.Add(message);
        public void Add(TextRange range, ErrorLevel level, string message, bool unnecessary = false) => Add(new Message(range, level, message, unnecessary));
        public void Clear() => messages.Clear();

        public IEnumerator<Message> GetEnumerator() => messages.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
