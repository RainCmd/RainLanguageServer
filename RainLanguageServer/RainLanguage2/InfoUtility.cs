using LanguageServer.Parameters.TextDocument;
using RainLanguageServer.RainLanguage2.GrammaticalAnalysis;
using System.Text;
using System.Xml.Linq;

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
        private static string FieldInfo(AbstractDeclaration declaration, Type type, Manager manager, AbstractSpace? space)
        {
            if (!manager.TryGetDefineDeclaration(declaration.declaration, out var abstractDeclaration)) throw new InvalidOperationException();
            var sb = new StringBuilder();
            sb.Append("(字段)");
            sb.Append(type.Info(manager, false, space));
            sb.Append(' ');
            if (GetQualifier(declaration.declaration.library, declaration.space, space, sb)) sb.Append('.');
            sb.Append(abstractDeclaration.name);
            sb.Append('.');
            sb.Append(declaration.name);
            return sb.ToString();
        }
        public static string Info(this AbstractDeclaration declaration, Manager manager, AbstractSpace? space = null)
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
            else if (declaration is AbstractFunction abstractFunction) return abstractFunction.Info(manager, null, space);
            else if (declaration is AbstractEnum)
            {
                var sb = new StringBuilder();
                sb.Append(KeyWords.ENUM);
                sb.Append(' ');
                if (GetQualifier(declaration.declaration.library, declaration.space, space, sb)) sb.Append('.');
                sb.Append(declaration.name);
                return sb.ToString();
            }
            else if (declaration is AbstractEnum.Element element)
            {
                if (!manager.TryGetDefineDeclaration(element.declaration, out var abstractDeclaration)) throw new InvalidOperationException();
                var sb = new StringBuilder();
                if (GetQualifier(declaration.declaration.library, declaration.space, space, sb)) sb.Append('.');
                sb.Append(abstractDeclaration.name);
                sb.Append('.');
                sb.Append(element.name);
                if (element.expression != null)
                {
                    sb.Append(" = ");
                    sb.Append(element.expression.range.ToString());
                }
                else if (element.calculated)
                {
                    sb.Append(" = ");
                    sb.Append(element.value);
                }
                return sb.ToString();
            }
            else if (declaration is AbstractStruct)
            {
                var sb = new StringBuilder();
                sb.Append(KeyWords.STRUCT);
                sb.Append(' ');
                if (GetQualifier(declaration.declaration.library, declaration.space, space, sb)) sb.Append('.');
                sb.Append(declaration.name);
                return sb.ToString();
            }
            else if (declaration is AbstractStruct.Variable abstractStructVariable) return FieldInfo(declaration, abstractStructVariable.type, manager, space);
            else if (declaration is AbstractStruct.Function abstractStructFunction)
            {
                if (!manager.TryGetDefineDeclaration(abstractStructFunction.declaration, out var abstractDeclaration)) throw new InvalidOperationException();
                return abstractStructFunction.Info(manager, abstractDeclaration, space);
            }
            else if (declaration is AbstractInterface)
            {
                var sb = new StringBuilder();
                sb.Append(KeyWords.INTERFACE);
                sb.Append(' ');
                if (GetQualifier(declaration.declaration.library, declaration.space, space, sb)) sb.Append('.');
                sb.Append(declaration.name);
                return sb.ToString();
            }
            else if (declaration is AbstractInterface.Function abstractInterfaceFunction)
            {
                if (!manager.TryGetDefineDeclaration(abstractInterfaceFunction.declaration, out var abstractDeclaration)) throw new InvalidOperationException();
                return abstractInterfaceFunction.Info(manager, abstractDeclaration, space);
            }
            else if (declaration is AbstractClass)
            {
                var sb = new StringBuilder();
                sb.Append(KeyWords.CLASS);
                sb.Append(' ');
                if (GetQualifier(declaration.declaration.library, declaration.space, space, sb)) sb.Append('.');
                sb.Append(declaration.name);
                return sb.ToString();
            }
            else if (declaration is AbstractClass.Variable abstractClassVariable) return FieldInfo(declaration, abstractClassVariable.type, manager, space);
            else if (declaration is AbstractClass.Constructor abstractClassConstructor)
            {
                if (!manager.TryGetDefineDeclaration(abstractClassConstructor.declaration, out var abstractDeclaration)) throw new InvalidOperationException();
                return abstractClassConstructor.Info(manager, abstractDeclaration, space);
            }
            else if (declaration is AbstractClass.Function abstractClassFunction)
            {
                if (!manager.TryGetDefineDeclaration(abstractClassFunction.declaration, out var abstractDeclaration)) throw new InvalidOperationException();
                return abstractClassFunction.Info(manager, abstractDeclaration, space);
            }
            else if (declaration is AbstractDelegate abstractDelegate) return abstractDelegate.Info(manager, null, space);
            else if (declaration is AbstractTask abstractTask)
            {
                var sb = new StringBuilder();
                sb.Append(KeyWords.TASK);
                sb.Append(' ');
                for (var i = 0; i < abstractTask.returns.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(abstractTask.returns[i].Info(manager, false, space));
                }
                if (abstractTask.returns.Count > 0) sb.Append(' ');
                if (GetQualifier(declaration.declaration.library, space, space, sb)) sb.Append('.');
                sb.Append(declaration.name);
                return sb.ToString();
            }
            else if (declaration is AbstractNative abstractNative) return abstractNative.Info(manager, null, space);
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
                if (OnHover(fileType.name.qualify, position, out info)) return true;
                if (fileType.name.name.Contain(position))
                {
                    info = new HoverInfo(fileType.name.name, type.Info(manager, true, space).MakedownCode(), true);
                    return true;
                }
            }
            info = default;
            return false;
        }
        public static HoverInfo Hover(this Local local, Manager manager, TextPosition position)
        {
            var sb = new StringBuilder();
            if (local.parameter) sb.Append("(参数)");
            else sb.Append("(局部变量)");
            sb.Append(local.type.Info(manager, false, ManagerOperator.GetSpace(manager, position)));
            sb.Append(' ');
            sb.Append(local.range.ToString());
            return new HoverInfo(local.range, sb.ToString().MakedownCode(), true);
        }
        public static bool OnHover(List<TextRange> qualify, TextPosition position, out HoverInfo info)
        {
            foreach (var range in qualify)
                if (range.Contain(position))
                {
                    var sb = new StringBuilder();
                    sb.Append(KeyWords.NAMESPACE);
                    sb.Append(' ');
                    sb.Append(range);
                    info = new HoverInfo(range, sb.ToString().MakedownCode(), true);
                    return true;
                }
            info = default;
            return false;
        }
        public static bool OnHighlight(this FileType fileType, Manager manager, TextPosition position, Type type, List<HighlightInfo> infos)
        {
            if (manager.TryGetDeclaration(type, out var declaration))
            {
                if (fileType.name.name.Contain(position)) return declaration.OnHighlight(manager, position, infos);
                if (OnHighlight(fileType.name.qualify, position, declaration.space, infos)) return true;
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
        private static bool QualifyAction(List<TextRange> qualify, TextPosition position, AbstractSpace? space, Action<AbstractSpace> action)
        {
            for (var i = 0; i < qualify.Count; i++)
            {
                var range = qualify[^(i + 1)];
                if (range.Contain(position))
                {
                    var index = space;
                    while (index != null && i > 0)
                    {
                        i--;
                        index = index.parent;
                    }
                    if (index != null) action(index);
                    return true;
                }
            }
            return false;
        }
        public static bool OnHighlight(List<TextRange> qualify, TextPosition position, AbstractSpace? space, List<HighlightInfo> infos)
        {
            return QualifyAction(qualify, position, space, value =>
            {
                foreach (var reference in value.references)
                    infos.Add(new HighlightInfo(reference, DocumentHighlightKind.Text));
            });
        }
        public static void Highlight(AbstractDeclaration declaration, List<HighlightInfo> infos)
        {
            infos.Add(new HighlightInfo(declaration.name, DocumentHighlightKind.Text));
            foreach (var reference in declaration.references)
                infos.Add(new HighlightInfo(reference, DocumentHighlightKind.Text));
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
                if (FindReferences(fileType.name.qualify, position, declaration.space, references)) return true;
            }
            return false;
        }
        public static bool FindReferences(List<TextRange> qualify, TextPosition position, AbstractSpace? space, List<TextRange> references)
        {
            return QualifyAction(qualify, position, space, value => references.AddRange(value.references));
        }
    }
}
