namespace RainLanguageServer.RainLanguage
{
    internal enum Visibility
    {
        None,
        Public = 0x1,
        Internal = 0x2,
        Space = 0x4,
        Protected = 0x8,
        Private = 0x10,
    }
    internal static class VisibilityExtend
    {
        public static bool ContainAny(this Visibility visibility, Visibility target)
        {
            return (visibility & target) != 0;
        }

        public static bool CanAccess(Visibility visibility, bool space, bool child)
        {
            if (space)
            {
                if (child && visibility.ContainAny(Visibility.Protected)) return true;
                else return !visibility.ContainAny(Visibility.Protected | Visibility.Private);
            }
            else
            {
                if (visibility.ContainAny(Visibility.Space)) return false;
                else if (child && visibility.ContainAny(Visibility.Protected)) return true;
                else return !visibility.ContainAny(Visibility.Protected | Visibility.Private);
            }
        }
    }
}
