namespace RainLanguageServer
{
    internal readonly struct CodeLenInfo(TextRange range, string title, string command = "", dynamic[]? arguments = null)
    {
        public readonly TextRange range = range;
        public readonly string title = title;
        public readonly string command = command;
        public readonly dynamic[]? arguments = arguments;
    }
}
