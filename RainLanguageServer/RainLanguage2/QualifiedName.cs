using System.Text;

namespace RainLanguageServer.RainLanguage2
{
    internal readonly struct QualifiedName(List<TextRange> qualify, TextRange name)
    {
        public readonly List<TextRange> qualify = qualify;
        public readonly TextRange name = name;
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
