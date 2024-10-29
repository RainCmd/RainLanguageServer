namespace RainLanguageServer
{
    internal readonly struct CodeActionInfo
    {
        public readonly string title;
        /// <summary>
        /// <see cref="LanguageServer.Parameters.CodeActionKind"/>
        /// </summary>
        public readonly string? kind;
        public readonly Dictionary<TextRange, string>? changes;
    }
}
