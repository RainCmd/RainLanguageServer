using LanguageServer.Parameters.TextDocument;
using RainLanguageServer.RainLanguage2.GrammaticalAnalysis;
using System.Text;

namespace RainLanguageServer.RainLanguage2
{
    internal static class InfoUtility
    {
        public static string MakedownCode(this string code)
        {
            return $"```\n{code}\n```";
        }
        private static bool GetQualifier(AbstractSpace? space, AbstractSpace? root, StringBuilder builder)
        {
            if (space != root && space != null)
            {
                if (GetQualifier(space.parent, root, builder)) builder.Append('.');
                builder.Append(space.name.ToString());
                return true;
            }
            return false;
        }
        public static bool GetQualifier(int library, AbstractSpace? space, AbstractSpace? root, StringBuilder builder)
        {
            if (library != Manager.LIBRARY_KERNEL)
            {
                while (root != null && !root.Contain(space))
                    root = root.parent;
            }
            return GetQualifier(space, root, builder);
        }
        public static string Info(this Type type, Manager manager, bool addCode, AbstractSpace? space = null)
        {
            if (manager.TryGetDeclaration(type, out var declaration))
            {
                var sb = new StringBuilder();
                if (GetQualifier(type.library, declaration.space, space, sb)) sb.Append('.');
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
        public static string Info(this AbstractDeclaration declaration, Manager manager, AbstractSpace? space = null, AbstractDeclaration? context = null)
        {
            if (declaration is AbstractVariable abstractVariable)
            {
                var sb = new StringBuilder();
                if (abstractVariable.isReadonly) sb.Append("(常量)");
                else sb.Append("(全局变量)");
                sb.Append(abstractVariable.type.Info(manager, false, space));
                sb.Append(' ');
                if (GetQualifier(declaration.declaration.library, declaration.space, space, sb)) sb.Append('.');
                sb.Append(abstractVariable.name.ToString());
                return sb.ToString();
            }
            else if (declaration is AbstractFunction abstractFunction)
            {
                var sb = new StringBuilder();
                sb.Append("(全局函数)");

            }
            throw new Exception("无效的声明");
        }
        public static string Info(this AbstractCallable callable, Manager manager, AbstractDeclaration? declaration = null, AbstractSpace? space = null)
        {
            var sb = new StringBuilder();
            if (callable is AbstractDelegate)
            {
                sb.Append(KeyWords.DELEGATE);
                sb.Append(' ');
            }
            else if (callable is AbstractNative)
            {
                sb.Append(KeyWords.NATIVE);
                sb.Append(' ');
            }
            for (var i = 0; i < callable.returns.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(callable.returns[i].Info(manager, false, space));
            }
            if (callable.returns.Count > 0) sb.Append(' ');
            if (declaration != null)
            {
                if (GetQualifier(declaration.declaration.library, declaration.space, space, sb)) sb.Append('.');
                sb.Append(declaration.name.ToString());
                sb.Append('.');
            }
            else if (GetQualifier(callable.declaration.library, callable.space, space, sb)) sb.Append('.');
            sb.Append(callable.name);
            sb.Append('(');
            for (var i = 0; i < callable.parameters.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var parameter = callable.parameters[i];
                sb.Append(parameter.type.Info(manager, false, space));
                if (parameter.name != null)
                {
                    sb.Append(' ');
                    sb.Append(parameter.name.ToString());
                }
            }
            sb.Append(')');
            if (declaration == null)
            {
                var count = callable.space.declarations[callable.name.ToString()].Count;
                if (count > 1) sb.Append($" +{count} 个重载");
            }
            else
            {
                var name = callable.name.ToString();
                var count = 0;
                if (declaration is AbstractStruct abstractStruct)
                {
                    foreach (var function in abstractStruct.functions)
                        if (name == function.name)
                            count++;
                }
                else if (declaration is AbstractClass abstractClass)
                {
                    var set = new HashSet<Tuple>();
                    foreach (var index in manager.GetInheritIterator(abstractClass))
                        foreach (var function in abstractClass.functions)
                            if (name == function.name && set.Add(function.signature))
                                count++;
                }
                else if (declaration is AbstractInterface abstractInterface)
                {
                    var set = new HashSet<Tuple>();
                    foreach (var index in manager.GetInheritIterator(abstractInterface))
                        foreach (var function in abstractInterface.functions)
                            if (name == function.name && set.Add(function.signature))
                                count++;
                }
                if (count > 1) sb.Append($" +{count} 个重载");
            }
            return sb.ToString();
        }
        public static bool OnHover(this FileType fileType, Manager manager, TextPosition position, Type type, AbstractSpace? space, out HoverInfo info)
        {
            if (fileType.range.Contain(position))
            {
                foreach (var qualify in fileType.name.qualify)
                    if (qualify.Contain(position))
                    {
                        var sb = new StringBuilder();
                        sb.Append(KeyWords.NAMESPACE);
                        sb.Append(' ');
                        sb.Append(space);
                        info = new HoverInfo(qualify, sb.ToString().MakedownCode(), true);
                        return true;
                    }
                if (fileType.name.name.Contain(position))
                {
                    info = new HoverInfo(fileType.name.name, type.Info(manager, true, space).MakedownCode(), true);
                    return true;
                }
            }
            info = default;
            return false;
        }
        public static bool OnHighlight(this FileType fileType, Manager manager, TextPosition position, Type type, List<HighlightInfo> infos)
        {
            if (manager.TryGetDeclaration(type, out var declaration))
            {
                if (fileType.name.name.Contain(position)) return declaration.OnHighlight(manager, position, infos);
                for (var i = 0; i < fileType.name.qualify.Count; i++)
                {
                    var range = fileType.name.qualify[^(i + 1)];
                    if (range.Contain(position))
                    {
                        infos.Add(new HighlightInfo(range, DocumentHighlightKind.Text));
                        var index = declaration.space;
                        while (index != null && i > 0)
                        {
                            i--;
                            index = index.parent;
                        }
                        if (index != null)
                            foreach (var reference in index.references)
                                infos.Add(new HighlightInfo(reference, DocumentHighlightKind.Text));
                        return true;
                    }
                }
            }
            else if (fileType.name.name.Contain(position))
            {
                infos.Add(new HighlightInfo(fileType.name.name, DocumentHighlightKind.Text));
                return true;
            }
            return fileType.range.Contain(position);
        }
        public static void OnHighlight(this Local local, List<HighlightInfo> infos)
        {
            infos.Add(new HighlightInfo(local.range, DocumentHighlightKind.Text));
            foreach (var range in local.read)
                infos.Add(new HighlightInfo(range, DocumentHighlightKind.Read));
            foreach (var range in local.write)
                infos.Add(new HighlightInfo(range, DocumentHighlightKind.Write));
        }
        public static void FindReferences(this Local local, List<TextRange> references)
        {
            references.AddRange(local.read);
            references.AddRange(local.write);
        }
        public static bool TryGetDefinition(this FileType fileType, Manager manager, TextPosition position, Type type, out TextRange definition)
        {
            if (fileType.name.name.Contain(position))
            {
                if (manager.TryGetDeclaration(type, out var declaration)) definition = declaration.name;
                else definition = fileType.name.name;
                return true;
            }
            definition = default;
            return false;
        }
        public static bool FindReferences(this FileType fileType, Manager manager, TextPosition position, Type type, List<TextRange> references)
        {
            if (manager.TryGetDeclaration(type, out var declaration))
            {
                if (fileType.name.name.Contain(position)) return declaration.FindReferences(manager, position, references);
                for (var i = 0; i < fileType.name.qualify.Count; i++)
                {
                    var range = fileType.name.qualify[^(i + 1)];
                    if (range.Contain(position))
                    {
                        var index = declaration.space;
                        while (index != null && i > 0)
                        {
                            i--;
                            index = index.parent;
                        }
                        if (index != null) references.AddRange(index.references);
                        return true;
                    }
                }
            }
            return fileType.range.Contain(position);
        }
    }
}
