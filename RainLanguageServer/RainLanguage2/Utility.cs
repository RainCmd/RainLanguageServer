namespace RainLanguageServer.RainLanguage2
{
    internal static class Utility
    {
        public static T RemoveAt<T>(this List<T> list, Index index)
        {
            var offset = index.GetOffset(list.Count);
            var result = list[offset];
            list.RemoveAt(offset);
            return result;
        }
        public static string Format(this string text, params object[] parameters) => string.Format(text, parameters);
    }
}
