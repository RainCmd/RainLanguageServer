namespace RainLanguageServer
{
    internal readonly struct CodeActionInfo(string title, string? kind = null, Dictionary<TextRange, string>? changes = null)
    {
        public readonly string title = title;

        /// <summary>
        /// <see cref="LanguageServer.Parameters.CodeActionKind"/>
        /// </summary>
        public readonly string? kind = kind;
        public readonly Dictionary<TextRange, string>? changes = changes;
    }
}
