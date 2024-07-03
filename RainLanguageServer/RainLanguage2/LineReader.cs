namespace RainLanguageServer.RainLanguage2
{
    internal class LineReader(TextDocument document)
    {
        public readonly TextDocument document = document;
        private int line = 0;
        public TextLine CurrentLine => document[line];
        public bool ReadLine(out TextLine line)
        {
            if (this.line++ < document.LineCount)
            {
                line = document[this.line - 1];
                return true;
            }
            line = default;
            return false;
        }
        public void Rollback() => line--;
    }
}
