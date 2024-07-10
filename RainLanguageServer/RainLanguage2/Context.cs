namespace RainLanguageServer.RainLanguage2
{
    internal readonly struct Context(TextDocument document, AbstractSpace space, HashSet<AbstractSpace> relies, AbstractDeclaration? declaration)
    {
        public readonly TextDocument document = document;
        public readonly AbstractSpace space = space;
        public readonly HashSet<AbstractSpace> relies = relies;
        public readonly AbstractDeclaration? declaration = declaration;
    }
}
