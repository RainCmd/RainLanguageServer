namespace RainLanguageServer.RainLanguage
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
        public TextLine GetLastValidLine()
        {
            for (var i = line; i > 0; i--)
            {
                var result = document[i - 1];
                if (result.indent >= 0) return result;
            }
            return document[0];
        }
    }
}
