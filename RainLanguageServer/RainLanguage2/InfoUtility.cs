using System.Text;

namespace RainLanguageServer.RainLanguage2
{
    internal static class InfoUtility
    {
        public static string MakedownCode(this string code)
        {
            return $"```\n{code}\n```";
        }
        private static void GetQualifier(AbstractSpace? space, AbstractSpace? root, StringBuilder sb)
        {
            if (space != root && space != null)
            {
                GetQualifier(space.parent, root, sb);
                if (sb.Length > 0) sb.Append('.');
                sb.Append(space.name.ToString());
            }
        }
        public static string Info(this Type type, Manager manager, bool addCode, AbstractSpace? space = null)
        {
            if (manager.TryGetDeclaration(type, out var declaration))
            {
                var sb = new StringBuilder();
                if (type.library != Manager.LIBRARY_KERNEL)
                {
                    while (space != null && !space.Contain(declaration.space))
                        space = space.parent;
                }
                GetQualifier(declaration.space, space, sb);
                if (sb.Length > 0) sb.Append('.');
                sb.Append(declaration.name.ToString());
                if (addCode)
                {
                    switch (type.code)
                    {
                        case TypeCode.Invalid: break;
                        case TypeCode.Struct: return $"struct {sb}";
                        case TypeCode.Enum: return $"enum {sb}";
                        case TypeCode.Handle: return $"handle {sb}";
                        case TypeCode.Interface: return $"interface {sb}";
                        case TypeCode.Delegate: return $"delegate {sb}";
                        case TypeCode.Task: return $"task {sb}";
                    }
                }
                else
                {
                    for (var i = 0; i < type.dimension; i++) sb.Append("[]");
                    return sb.ToString();
                }
            }
            return "无效的类型";
        }
    }
}
