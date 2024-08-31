using System.Text;

namespace RainLanguageServer.RainLanguage
{
    internal readonly struct QualifiedName
    {
        public readonly List<TextRange> qualify;
        public readonly TextRange name;

        public QualifiedName(List<TextRange> qualify, TextRange name)
        {
            this.qualify = qualify;
            this.name = name;
        }
        public QualifiedName(List<TextRange> names)
        {
            qualify = names;
            name = names.RemoveAt(^1);
        }
        public TextRange Range
        {
            get
            {
                if (qualify.Count > 0) return qualify[0] & name;
                return name;
            }
        }
        public override string ToString()
        {
            if (qualify.Count == 0) return name.ToString();
            var sb = new StringBuilder();
            foreach (var item in qualify)
            {
                if (sb.Length > 0) sb.Append('.');
                sb.Append(item.ToString());
            }
            sb.Append('.');
            sb.Append(name.ToString());
            return sb.ToString();
        }
        public IEnumerator<TextRange> GetEnumerator()
        {
            foreach (var item in qualify)
                yield return item;
            yield return name;
        }
    }
}
