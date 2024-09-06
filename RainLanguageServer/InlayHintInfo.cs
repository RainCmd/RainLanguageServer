namespace RainLanguageServer
{
    internal readonly struct InlayHintInfo(string label, TextPosition position, Info? tooltip = null)
    {
        public readonly string label = label;
        public readonly TextPosition position = position;
        public readonly Info? tooltip = tooltip;
    }
}
