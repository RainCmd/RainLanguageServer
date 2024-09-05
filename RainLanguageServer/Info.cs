using LanguageServer.Parameters;
using LanguageServer.Parameters.TextDocument;

namespace RainLanguageServer
{
    internal readonly struct Info(string info, bool markdown)
    {
        public readonly string info = info;
        public readonly bool markdown = markdown;
        public Documentation GetDocumentation()
        {
            if (markdown) return new MarkupContent(MarkupKind.Markdown, info);
            else return info;
        }
    }
}
