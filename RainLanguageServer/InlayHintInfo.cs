using LanguageServer.Parameters.TextDocument;

namespace RainLanguageServer
{
    internal readonly struct InlayHintInfo(string label, TextPosition position, InlayHintInfo.Kind? kind = null, Info? tooltip = null)
    {
        public enum Kind : long
        {
            Type = InlayHintKind.Type,
            Paramter = InlayHintKind.Parameter,
        }
        public readonly string label = label;
        public readonly Kind? kind = kind;
        public readonly TextPosition position = position;
        public readonly Info? tooltip = tooltip;
    }
}
