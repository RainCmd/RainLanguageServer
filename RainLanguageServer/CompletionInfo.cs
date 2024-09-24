using LanguageServer.Parameters.TextDocument;

namespace RainLanguageServer
{
    internal readonly struct CompletionInfo(string lable, CompletionItemKind kind, string documentation)
    {
        public readonly string lable = lable;
        public readonly CompletionItemKind kind = kind;
        public readonly string documentation = documentation;
    }
}
