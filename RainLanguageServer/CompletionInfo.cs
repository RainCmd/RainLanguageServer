using LanguageServer.Parameters.TextDocument;

namespace RainLanguageServer
{
    internal readonly struct CompletionInfo(string lable, CompletionItemKind kind, string data)
    {
        public readonly string lable = lable;
        public readonly CompletionItemKind kind = kind;
        public readonly string data = data;
    }
}
