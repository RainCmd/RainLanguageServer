using LanguageServer.Parameters.TextDocument;

namespace RainLanguageServer
{
    internal readonly struct CompletionInfo(string lable, CompletionItemKind kind, string documentation, string? filterText = null)
    {
        public readonly string lable = lable;
        public readonly CompletionItemKind kind = kind;
        public readonly string documentation = documentation;
        public readonly string? filterText = filterText;
    }
}
