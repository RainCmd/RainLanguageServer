using LanguageServer.Parameters.TextDocument;
using RainLanguageServer.RainLanguage.GrammaticalAnalysis;
using System.Text;

namespace RainLanguageServer.RainLanguage
{
    internal enum DetailTokenType
    {
        GlobalVariable,
        GlobalFunction,
        NativeFunction,

        TypeEnum,
        TypeStruct,
        TypeInterface,
        TypeHandle,
        TypeDelegate,
        TypeTask,

        MemberElement,
        MemberField,
        MemberFunction,
        MemberConstructor,

        Constant,
        Parameter,
        Local,
        DeprecatedLocal,

        KeywordCtrl,
        KeywordType,
        KeywordVariable,// this base global
        KeywordConst,//true false null

        Numeric,
        String,

        Namespace,
        Label,

        Operator,
    }
    internal enum CompletionFilter
    {
        Interface = 0x1,
        Class = 0x2,
        Define = 0x4 | Interface | Class,
        All = 0x8 | Define,
    }
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
                builder.Append(space.name);
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
        public static string GetFullName(this AbstractDeclaration declaration, Manager manager)
        {
            var sb = new StringBuilder();
            if (GetQualifier(declaration.declaration.library, declaration.space, null, sb)) sb.Append('.');
            if (manager.TryGetDefineDeclaration(declaration.declaration, out var define) && define != declaration)
            {
                sb.Append(define.name.ToString());
                sb.Append('.');
            }
            sb.Append(declaration.name.ToString());
            return sb.ToString();
        }
        private static TextRange AnnotationTrim(TextRange line)
        {
            var index = 0;
            while (index < line.Count)
            {
                if (char.IsWhiteSpace(line[index])) index++;
                else if (line[index] == '/')
                {
                    index += 2;
                    break;
                }
                else throw new Exception("目前只支持 // 开头的单行注释");
            }
            return (line.start + index) & line.end;
        }
        private static string GetAnnotation(AbstractDeclaration declaration)
        {
            if (declaration.file.annotation.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var annotation in declaration.file.annotation)
                sb.AppendLine(AnnotationTrim(annotation).Trim.ToString());
            return sb.ToString();
        }
        private static bool AppendTuple(StringBuilder builder, Manager manager, AbstractSpace? space, Tuple tuple)
        {
            for (var i = 0; i < tuple.Count; i++)
            {
                if (i > 0) builder.Append(", ");
                builder.Append(tuple[i].Info(manager, space));
            }
            return tuple.Count > 0;
        }
        private static void AppendParameters(StringBuilder builder, Manager manager, AbstractSpace? space, List<AbstractCallable.Parameter> parameters)
        {
            builder.Append('(');
            for (var i = 0; i < parameters.Count; i++)
            {
                if (i > 0) builder.Append(", ");
                var parameter = parameters[i];
                builder.Append(parameter.type.Info(manager, space));
                if (parameter.name.Count > 0)
                {
                    builder.Append(' ');
                    builder.Append(parameter.name.ToString());
                }
            }
            builder.Append(')');
        }
        public static string CodeInfo(this Type type, Manager manager, AbstractSpace? space = null)
        {
            if (manager.TryGetDeclaration(type, out var declaration))
            {
                var sb = new StringBuilder();
                switch (type.code)
                {
                    case TypeCode.Invalid: break;
                    case TypeCode.Struct:
                        sb.Append(KeyWords.STRUCT);
                        break;
                    case TypeCode.Enum:
                        sb.Append(KeyWords.ENUM);
                        break;
                    case TypeCode.Handle:
                        sb.Append(KeyWords.CLASS);
                        break;
                    case TypeCode.Interface:
                        sb.Append(KeyWords.INTERFACE);
                        break;
                    case TypeCode.Delegate:
                        sb.Append(KeyWords.DELEGATE);
                        break;
                    case TypeCode.Task:
                        sb.Append(KeyWords.TASK);
                        break;
                }
                sb.Append(' ');
                var abstractDelegate = declaration as AbstractDelegate;
                if (abstractDelegate != null)
                {
                    if (AppendTuple(sb, manager, space, abstractDelegate.returns)) sb.Append(' ');
                }
                else if (declaration is AbstractTask abstractTask)
                    if (AppendTuple(sb, manager, space, abstractTask.returns)) sb.Append(' ');
                if (GetQualifier(type.library, declaration.space, space, sb)) sb.Append('.');
                sb.Append(declaration.name.ToString());
                if (abstractDelegate != null) AppendParameters(sb, manager, space, abstractDelegate.parameters);
                return GetAnnotation(declaration) + sb.ToString().MakedownCode();
            }
            return "无效的类型";
        }
        public static string Info(this Type type, Manager manager, AbstractSpace? space = null)
        {
            if (manager.TryGetDeclaration(type, out var declaration))
            {
                var sb = new StringBuilder();
                if (type.library != Manager.LIBRARY_KERNEL)
                    if (GetQualifier(type.library, declaration.space, space, sb))
                        sb.Append('.');
                sb.Append(declaration.name.ToString());
                for (var i = 0; i < type.dimension; i++) sb.Append("[]");
                return sb.ToString();
            }
            return "无效的类型";
        }
        private static string FieldInfo(AbstractDeclaration declaration, Type type, Manager manager, AbstractSpace? space)
        {
            if (!manager.TryGetDefineDeclaration(declaration.declaration, out var abstractDeclaration)) throw new InvalidOperationException();
            var sb = new StringBuilder();
            sb.Append("(字段)");
            sb.Append(type.Info(manager, space));
            sb.Append(' ');
            if (GetQualifier(declaration.declaration.library, declaration.space, space, sb)) sb.Append('.');
            sb.Append(abstractDeclaration.name);
            sb.Append('.');
            sb.Append(declaration.name);
            return sb.ToString();
        }
        private static string InternalInfo(AbstractDeclaration declaration, Manager manager, AbstractSpace? space)
        {
            if (declaration is AbstractVariable abstractVariable)
            {
                var sb = new StringBuilder();
                if (abstractVariable.isReadonly) sb.Append("(常量)");
                else sb.Append("(全局变量)");
                sb.Append(abstractVariable.type.Info(manager, space));
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
                    sb.Append(abstractTask.returns[i].Info(manager, space));
                }
                if (abstractTask.returns.Count > 0) sb.Append(' ');
                if (GetQualifier(declaration.declaration.library, space, space, sb)) sb.Append('.');
                sb.Append(declaration.name);
                return sb.ToString();
            }
            else if (declaration is AbstractNative abstractNative) return abstractNative.Info(manager, null, space);
            throw new Exception("无效的声明");
        }
        public static string CodeInfo(this AbstractDeclaration declaration, Manager manager, AbstractSpace? space = null)
        {
            var sb = new StringBuilder();
            sb.Append(GetAnnotation(declaration));
            sb.Append(InternalInfo(declaration, manager, space).MakedownCode());
            return sb.ToString();
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
            if (AppendTuple(sb, manager, space, callable.returns)) sb.Append(' ');
            if (declaration != null)
            {
                if (GetQualifier(declaration.declaration.library, declaration.space, space, sb)) sb.Append('.');
                sb.Append(declaration.name.ToString());
                sb.Append('.');
            }
            else if (GetQualifier(callable.declaration.library, callable.space, space, sb)) sb.Append('.');
            sb.Append(callable.name);
            AppendParameters(sb, manager, space, callable.parameters);
            if (declaration == null)
            {
                if (callable.space.declarations.TryGetValue(callable.name.ToString(), out var declarations))
                {
                    var count = callable.space.declarations[callable.name.ToString()].Count;
                    if (count > 1) sb.Append($" (+{count}个重载)");
                }
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
                    if (callable is AbstractClass.Constructor) count = abstractClass.constructors.Count;
                    else
                    {
                        var set = new HashSet<Tuple>();
                        foreach (var index in manager.GetInheritIterator(abstractClass))
                            foreach (var function in abstractClass.functions)
                                if (name == function.name && set.Add(function.signature))
                                    count++;
                    }
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
        public static SignatureInfo GetSignatureInfo(this AbstractCallable callable, Manager manager, AbstractDeclaration? declaration = null, AbstractSpace? space = null)
        {
            var annotation = GetAnnotation(callable);
            Info? info = string.IsNullOrEmpty(annotation) ? null : new Info(annotation, false);
            var parameters = new SignatureInfo.ParameterInfo[callable.signature.Count];
            var sb = new StringBuilder();
            if (AppendTuple(sb, manager, space, callable.returns)) sb.Append(' ');
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
                var parameterInfo = parameter.type.Info(manager, space);
                if (parameter.name.Count > 0) parameterInfo = $"{parameterInfo} {parameter.name}";
                sb.Append(parameterInfo);
                parameters[i] = new SignatureInfo.ParameterInfo(parameterInfo, null);
            }
            sb.Append(')');

            var result = new SignatureInfo(sb.ToString(), info, parameters);
            return result;
        }
        public static List<SignatureInfo> GetStructConstructorSignatureInfos(Manager manager, AbstractStruct abstractStruct, AbstractSpace? space)
        {
            var infos = new List<SignatureInfo>();
            var annotation = GetAnnotation(abstractStruct);
            Info? info = string.IsNullOrEmpty(annotation) ? null : new Info(annotation, false);
            var sb = new StringBuilder();
            if (GetQualifier(abstractStruct.declaration.library, abstractStruct.space, space, sb)) sb.Append('.');
            sb.Append(abstractStruct.name.ToString());
            infos.Add(new SignatureInfo($"{sb}()", info, []));
            if (abstractStruct.variables.Count > 0)
            {
                var parameters = new SignatureInfo.ParameterInfo[abstractStruct.variables.Count];
                sb.Append('(');
                for (var i = 0; i < abstractStruct.variables.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    var parameter = abstractStruct.variables[i];
                    var parameterInfo = $"{parameter.type.Info(manager, space)} {parameter.name}";
                    sb.Append(parameterInfo);
                    parameters[i] = new SignatureInfo.ParameterInfo(parameterInfo, null);
                }
                sb.Append(')');
                infos.Add(new SignatureInfo(sb.ToString(), info, parameters));
            }
            return infos;
        }
        public static bool OnHover(this FileType fileType, Manager manager, TextPosition position, Type type, AbstractSpace? space, out HoverInfo info)
        {
            if (fileType.range.Contain(position))
            {
                if (OnHover(fileType.name.qualify, position, out info)) return true;
                if (fileType.name.name.Contain(position))
                {
                    info = new HoverInfo(fileType.name.name, type.CodeInfo(manager, space), true);
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
            sb.Append(local.type.Info(manager, ManagerOperator.GetSpace(manager, position)));
            sb.Append(' ');
            sb.Append(local.name);
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
                if (fileType.name.name.Contain(position))
                {
                    Highlight(declaration, infos);
                    return true;
                }
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
        public static void HighlightGroup(List<TextRange> group, List<HighlightInfo> infos)
        {
            foreach (var range in group)
                infos.Add(new HighlightInfo(range, DocumentHighlightKind.Text));
        }
        private static void Highlight(HashSet<TextRange> ranges, List<HighlightInfo> infos, DocumentHighlightKind kind)
        {
            foreach (var range in ranges)
                infos.Add(new HighlightInfo(range, kind));
        }
        public static void Highlight(AbstractDeclaration declaration, List<HighlightInfo> infos)
        {
            infos.Add(new HighlightInfo(declaration.name, DocumentHighlightKind.Text));
            if (declaration is AbstractVariable variable)
            {
                Highlight(variable.references, infos, DocumentHighlightKind.Read);
                Highlight(variable.write, infos, DocumentHighlightKind.Write);
            }
            else if (declaration is AbstractStruct.Variable structMember)
            {
                Highlight(structMember.references, infos, DocumentHighlightKind.Read);
                Highlight(structMember.write, infos, DocumentHighlightKind.Write);
            }
            else if (declaration is AbstractClass.Variable classMember)
            {
                Highlight(classMember.references, infos, DocumentHighlightKind.Read);
                Highlight(classMember.write, infos, DocumentHighlightKind.Write);
            }
            else Highlight(declaration.references, infos, DocumentHighlightKind.Text);
        }
        public static void FindReferences(this Local local, List<TextRange> references)
        {
            references.AddRange(local.read);
            references.AddRange(local.write);
        }
        public static bool TryGetDefinition(this FileType fileType, Manager manager, TextPosition position, Type type, out TextRange definition)
        {
            foreach (var space in fileType.name.qualify)
                if (space.Contain(position))
                {
                    definition = space;
                    return true;
                }
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

        public static void Rename(List<TextRange> qualify, TextPosition position, AbstractSpace? space, HashSet<TextRange> ranges) => QualifyAction(qualify, position, space, value => ranges.AddRange(value.references));
        public static bool Rename(this FileType fileType, Manager manager, TextPosition position, Type type, HashSet<TextRange> ranges)
        {
            if (fileType.range.Contain(position))
            {
                if (type.library == Manager.LIBRARY_SELF)
                    if (manager.TryGetDeclaration(type, out var declaration))
                    {
                        if (fileType.name.name.Contain(position)) Rename(declaration, ranges);
                        else Rename(fileType.name.qualify, position, declaration.space, ranges);
                    }
                return true;
            }
            return false;
        }
        public static void Rename(AbstractDeclaration declaration, HashSet<TextRange> ranges)
        {
            if (declaration.declaration.library != Manager.LIBRARY_SELF) return;
            ranges.Add(declaration.name);
            ranges.AddRange(declaration.references);
            if (declaration is AbstractVariable variable) ranges.AddRange(variable.write);
            else if (declaration is AbstractStruct.Variable structMember) ranges.AddRange(structMember.write);
            else if (declaration is AbstractClass.Variable classMember) ranges.AddRange(classMember.write);
            var name = declaration.name.ToString();
            ranges.RemoveAll(value => value != name);
        }
        public static void Rename(this Local local, HashSet<TextRange> ranges)
        {
            if (local.name == local.range)
            {
                ranges.Add(local.range);
                ranges.AddRange(local.read);
                ranges.AddRange(local.write);
            }
        }

        public static void CollectAccessKeyword(List<CompletionInfo> infos)
        {
            infos.Add(new CompletionInfo(KeyWords.PUBLIC, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.INTERNAL, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.SPACE, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.PROTECTED, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.PRIVATE, CompletionItemKind.Keyword, "关键字"));
        }
        public static void CollectDefineKeyword(List<CompletionInfo> infos)
        {
            infos.Add(new CompletionInfo(KeyWords.CONST, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.ENUM, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.STRUCT, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.INTERFACE, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.CLASS, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.DELEGATE, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.TASK, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.NATIVE, CompletionItemKind.Keyword, "关键字"));
        }
        private static void AddBaseType(List<CompletionInfo> infos, string type)
        {
            if (infos.FindIndex(value => value.lable == type) < 0)
                infos.Add(new CompletionInfo(type, CompletionItemKind.Keyword, "关键字"));
        }
        public static void CollectBaseType(List<CompletionInfo> infos)
        {
            AddBaseType(infos, KeyWords.BOOL);
            AddBaseType(infos, KeyWords.BYTE);
            AddBaseType(infos, KeyWords.CHAR);
            AddBaseType(infos, KeyWords.INTEGER);
            AddBaseType(infos, KeyWords.REAL);
            AddBaseType(infos, KeyWords.REAL2);
            AddBaseType(infos, KeyWords.REAL3);
            AddBaseType(infos, KeyWords.REAL4);
            AddBaseType(infos, KeyWords.TYPE);
            AddBaseType(infos, KeyWords.STRING);
            AddBaseType(infos, KeyWords.HANDLE);
            AddBaseType(infos, KeyWords.ENTITY);
            AddBaseType(infos, "Delegate");
            AddBaseType(infos, "Task");
            AddBaseType(infos, KeyWords.ARRAY);
        }
        public static void CollectCtrlKeyword(List<CompletionInfo> infos)
        {
            infos.Add(new CompletionInfo(KeyWords.IF, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.ELSEIF, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.ELSE, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.WHILE, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.FOR, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.BREAK, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.CONTINUE, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.RETURN, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.WAIT, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.EXIT, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.TRY, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.CATCH, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.FINALLY, CompletionItemKind.Keyword, "关键字"));
        }
        public static void CollectValueKeyword(Context context, List<CompletionInfo> infos)
        {
            if (context.declaration != null)
            {
                infos.Add(new CompletionInfo(KeyWords.THIS, CompletionItemKind.Keyword, "关键字"));
                if (context.declaration is AbstractClass)
                    infos.Add(new CompletionInfo(KeyWords.BASE, CompletionItemKind.Keyword, "关键字"));
            }
            infos.Add(new CompletionInfo(KeyWords.TRUE, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.FALSE, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.NULL, CompletionItemKind.Keyword, "关键字"));
        }
        public static void CollectRelationKeyword(List<CompletionInfo> infos)
        {
            infos.Add(new CompletionInfo(KeyWords.GLOBAL, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.VAR, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.IS, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.AS, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.AND, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.OR, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.START, CompletionItemKind.Keyword, "关键字"));
            infos.Add(new CompletionInfo(KeyWords.NEW, CompletionItemKind.Keyword, "关键字"));

        }
        public static void CollectChildrenSpaces(List<CompletionInfo> infos, AbstractSpace space)
        {
            foreach (var item in space.children.Values)
                infos.Add(new CompletionInfo(item.name.ToString(), CompletionItemKind.Module, "命名空间"));
        }
        public static void CollectSpaces(Manager manager, List<CompletionInfo> infos, AbstractSpace space, HashSet<AbstractSpace> relies)
        {
            foreach (var rely in relies)
                CollectChildrenSpaces(infos, rely);
            for (var index = space; index != null; index = index.parent)
                CollectChildrenSpaces(infos, index);
            foreach (var item in manager.imports)
                infos.Add(new CompletionInfo(item, CompletionItemKind.Module, "命名空间"));
            if (infos.FindIndex(value => value.lable == "kernel") < 0)
                infos.Add(new CompletionInfo("kernel", CompletionItemKind.Module, "核心库"));
        }
        public static void CollectMember(Manager manager, Type type, Context context, List<CompletionInfo> infos)
        {
            if (type.dimension > 0) type = manager.kernelManager.ARRAY;
            else if (type.code == TypeCode.Delegate) type = manager.kernelManager.DELEGATE;
            else if (type.code == TypeCode.Task) type = manager.kernelManager.TASK;
            if (manager.TryGetDeclaration(type, out var declaration))
            {
                if (declaration is AbstractEnum abstractEnum)
                {
                    foreach (var element in abstractEnum.elements)
                        infos.Add(new CompletionInfo(element.name.ToString(), CompletionItemKind.EnumMember, element.CodeInfo(manager, context.space)));
                    CollectMember(manager, manager.kernelManager.ENUM, context, infos);
                }
                else if (declaration is AbstractStruct abstractStruct)
                {
                    foreach (var member in abstractStruct.variables)
                        infos.Add(new CompletionInfo(member.name.ToString(), CompletionItemKind.Field, member.CodeInfo(manager, context.space)));
                    foreach (var member in abstractStruct.functions)
                        if (context.IsVisiable(manager, member.declaration))
                            infos.Add(new CompletionInfo(member.name.ToString(), CompletionItemKind.Method, member.CodeInfo(manager, context.space)));
                }
                else if (declaration is AbstractInterface abstractInterface)
                {
                    var set = new HashSet<AbstractCallable>();
                    foreach (var inherit in manager.GetInheritIterator(abstractInterface))
                        foreach (var callable in inherit.functions)
                            if (set.Add(callable))
                            {
                                infos.Add(new CompletionInfo(callable.name.ToString(), CompletionItemKind.Method, callable.CodeInfo(manager, context.space)));
                                set.AddRange(callable.overrides);
                            }
                }
                else if (declaration is AbstractClass abstractClass)
                {
                    foreach (var inherit in manager.GetInheritIterator(abstractClass))
                    {
                        foreach (var member in inherit.variables)
                            if (context.IsVisiable(manager, member.declaration))
                                infos.Add(new CompletionInfo(member.name.ToString(), CompletionItemKind.Field, member.CodeInfo(manager, context.space)));
                        var set = new HashSet<AbstractCallable>();
                        foreach (var member in inherit.functions)
                            if (set.Add(member) && context.IsVisiable(manager, member.declaration))
                            {
                                infos.Add(new CompletionInfo(member.name.ToString(), CompletionItemKind.Method, member.CodeInfo(manager, context.space)));
                                set.AddRange(member.overrides);
                            }
                    }
                }
            }
        }
        public static void CollectClassFunction(Manager manager, Type type, Context context, List<CompletionInfo> infos)
        {
            if (type.dimension > 0) type = manager.kernelManager.ARRAY;
            else if (type.code == TypeCode.Delegate) type = manager.kernelManager.DELEGATE;
            else if (type.code == TypeCode.Task) type = manager.kernelManager.TASK;
            if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstractClass abstractClass)
            {
                foreach (var inherit in manager.GetInheritIterator(abstractClass))
                {
                    var set = new HashSet<AbstractCallable>();
                    foreach (var member in inherit.functions)
                        if (set.Add(member) && context.IsVisiable(manager, member.declaration))
                        {
                            infos.Add(new CompletionInfo(member.name.ToString(), CompletionItemKind.Method, member.CodeInfo(manager, context.space)));
                            set.AddRange(member.overrides);
                        }
                }
            }
        }
        private static void AddDeclaration(Manager manager, List<CompletionInfo> infos, Declaration declaration, CompletionItemKind kind)
        {
            if (manager.TryGetDeclaration(declaration, out var abstractDeclaration))
                if (abstractDeclaration is AbstractVariable variable && variable.isReadonly)
                    infos.Add(new CompletionInfo(abstractDeclaration.name.ToString(), CompletionItemKind.Value, abstractDeclaration.CodeInfo(manager)));
                else
                    infos.Add(new CompletionInfo(abstractDeclaration.name.ToString(), kind, abstractDeclaration.CodeInfo(manager)));
        }
        private static bool ContainAll(this CompletionFilter filter, CompletionFilter other)
        {
            return (filter & other) == other;
        }
        private static bool IsOperator(string value)
        {
            return value switch
            {
                "&" or "|" or "^" or "<" or "<<" or ">" or ">>" or "+" or "++" or "-" or "--" or "*" or "/" or "%" or "!" or "~" or "!=" or "==" or ">=" or "<=" => true,
                _ => false,
            };
        }
        public static void CollectSpaceDeclarations(Manager manager, List<CompletionInfo> infos, AbstractSpace space, Context context, CompletionFilter filter)
        {
            foreach (var item in space.declarations)
                if (!IsOperator(item.Key))
                    foreach (var declaration in item.Value)
                        if (context.IsVisiable(manager, declaration))
                            switch (declaration.category)
                            {
                                case DeclarationCategory.Invalid: break;
                                case DeclarationCategory.Variable:
                                    if (filter.ContainAll(CompletionFilter.All)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Variable);
                                    break;
                                case DeclarationCategory.Function:
                                    if (filter.ContainAll(CompletionFilter.All)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Function);
                                    break;
                                case DeclarationCategory.Enum:
                                    if (filter.ContainAll(CompletionFilter.Define)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Enum);
                                    break;
                                case DeclarationCategory.EnumElement:
                                    if (filter.ContainAll(CompletionFilter.All)) AddDeclaration(manager, infos, declaration, CompletionItemKind.EnumMember);
                                    break;
                                case DeclarationCategory.Struct:
                                    if (filter.ContainAll(CompletionFilter.Define)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Struct);
                                    break;
                                case DeclarationCategory.StructVariable:
                                    if (filter.ContainAll(CompletionFilter.All)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Field);
                                    break;
                                case DeclarationCategory.StructFunction:
                                    if (filter.ContainAll(CompletionFilter.All)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Method);
                                    break;
                                case DeclarationCategory.Class:
                                    if (filter.ContainAll(CompletionFilter.Class)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Class);
                                    break;
                                case DeclarationCategory.Constructor:
                                    if (filter.ContainAll(CompletionFilter.All)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Constructor);
                                    break;
                                case DeclarationCategory.ClassVariable:
                                    if (filter.ContainAll(CompletionFilter.All)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Field);
                                    break;
                                case DeclarationCategory.ClassFunction:
                                    if (filter.ContainAll(CompletionFilter.All)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Method);
                                    break;
                                case DeclarationCategory.Interface:
                                    if (filter.ContainAll(CompletionFilter.Interface)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Interface);
                                    break;
                                case DeclarationCategory.InterfaceFunction:
                                    if (filter.ContainAll(CompletionFilter.All)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Method);
                                    break;
                                case DeclarationCategory.Delegate:
                                    if (filter.ContainAll(CompletionFilter.Define)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Event);
                                    break;
                                case DeclarationCategory.Task:
                                    if (filter.ContainAll(CompletionFilter.Define)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Event);
                                    break;
                                case DeclarationCategory.Native:
                                    if (filter.ContainAll(CompletionFilter.All)) AddDeclaration(manager, infos, declaration, CompletionItemKind.Function);
                                    break;
                            }
        }
        public static void CollectDeclarations(Manager manager, List<CompletionInfo> infos, Context context, CompletionFilter filter)
        {
            foreach (var rely in context.relies)
                CollectSpaceDeclarations(manager, infos, rely, context, filter);
            for (var index = context.space; index != null; index = index.parent)
                CollectSpaceDeclarations(manager, infos, index, context, filter);
            if (filter.ContainAll(CompletionFilter.Define)) CollectBaseType(infos);
            if (context.declaration != null && filter == CompletionFilter.All)
                CollectMember(manager, context.declaration.declaration.DefineType, context, infos);
        }
        public static void Completion(Manager manager, Context context, List<TextRange> ranges, TextPosition position, List<CompletionInfo> infos, CompletionFilter filter)
        {
            if (ranges[0].Contain(position))
            {
                CollectSpaces(manager, infos, context.space, context.relies);
                CollectDeclarations(manager, infos, context, filter);
            }
            else if (context.TryFindSpace(manager, ranges[0], out var space, null))
                for (var i = 1; i < ranges.Count; i++)
                    if ((ranges[i - 1].end & ranges[i].end).Contain(position))
                    {
                        CollectChildrenSpaces(infos, space);
                        CollectSpaceDeclarations(manager, infos, space, context, filter);
                    }
                    else if (!space.children.TryGetValue(ranges[i].ToString(), out space)) return;
        }
        private static bool IsAccessKeyword(string value)
        {
            if (value == KeyWords.PUBLIC) return true;
            if (value == KeyWords.INTERNAL) return true;
            if (value == KeyWords.SPACE) return true;
            if (value == KeyWords.PROTECTED) return true;
            if (value == KeyWords.PRIVATE) return true;
            return false;
        }
        public static void Completion(Manager manager, Context context, TextRange range, TextPosition position, List<CompletionInfo> infos, bool accessKeyword = false, bool defineKeyword = false, bool namespaceKeyword = false)
        {
            if (accessKeyword || namespaceKeyword)
            {
                if (Lexical.TryAnalysis(range, 0, out var lexical, null))
                {
                    if (lexical.anchor.Contain(position))
                    {
                        if (namespaceKeyword)
                        {
                            infos.Add(new CompletionInfo(KeyWords.IMPORT, CompletionItemKind.Keyword, "关键字"));
                            infos.Add(new CompletionInfo(KeyWords.NAMESPACE, CompletionItemKind.Keyword, "关键字"));
                        }
                        CollectAccessKeyword(infos);
                        CollectDefineKeyword(infos);
                        CollectSpaces(manager, infos, context.space, context.relies);
                        CollectDeclarations(manager, infos, context, CompletionFilter.Define);
                        return;
                    }
                    else if (IsAccessKeyword(lexical.anchor.ToString())) range = lexical.anchor.end & range.end;
                }
                else return;
            }
            if (defineKeyword)
            {
                if (Lexical.TryAnalysis(range, 0, out var lexical, null))
                {
                    if (lexical.anchor.Contain(position))
                    {
                        CollectDefineKeyword(infos);
                        CollectSpaces(manager, infos, context.space, context.relies);
                        CollectDeclarations(manager, infos, context, CompletionFilter.Define);
                        return;
                    }
                }
                else return;
            }
            while (Lexical.TryExtractName(range, 0, out var names, null))
            {
                if ((names[0].start & names[^1].end).Contain(position))
                {
                    Completion(manager, context, names, position, infos, CompletionFilter.Define);
                    return;
                }
                if (Lexical.TryAnalysis(range, names[^1].end, out var lexical, null) && (lexical.type == LexicalType.Comma || lexical.type == LexicalType.Semicolon))
                    range = lexical.anchor.end & range.end;
                else return;
            }
        }
        public static void Completion(this FileType fileType, Manager manager, TextPosition position, List<CompletionInfo> infos)
        {
            if (ManagerOperator.TryGetContext(manager, position, out var context))
            {
                var ranges = new List<TextRange>(fileType.name.qualify) { fileType.name.name };
                Completion(manager, context, ranges, position, infos, CompletionFilter.Define);
            }
        }

        public static void AddNamespace(this SemanticTokenCollector collector, QualifiedName name)
        {
            for (var i = 0; i < name.qualify.Count; i++)
            {
                if (i > 0) collector.Add(DetailTokenType.Operator, name.qualify[i - 1].end & name.qualify[i].start);
                collector.Add(DetailTokenType.Namespace, name.qualify[i]);
            }
            if (name.qualify.Count > 0) collector.Add(DetailTokenType.Operator, name.qualify[^1].end & name.name.start);
        }
        public static void AddType(this SemanticTokenCollector collector, TextRange range, Manager manager, Type type)
        {
            var kernel = manager.kernelManager;
            type = new Type(type, 0);
            if (type == kernel.BOOL || type == kernel.BYTE || type == kernel.CHAR || type == kernel.INT || type == kernel.REAL || type == kernel.REAL2 || type == kernel.REAL3 || type == kernel.REAL4 ||
                type == kernel.ENUM || type == kernel.TYPE || type == kernel.STRING || type == kernel.ENTITY || type == kernel.HANDLE || type == kernel.DELEGATE || type == kernel.TASK || type == kernel.ARRAY)
                collector.Add(DetailTokenType.KeywordType, range);
            else
                switch (type.code)
                {
                    case TypeCode.Invalid: goto default;
                    case TypeCode.Struct:
                        collector.Add(DetailTokenType.TypeStruct, range);
                        break;
                    case TypeCode.Enum:
                        collector.Add(DetailTokenType.TypeEnum, range);
                        break;
                    case TypeCode.Handle:
                        collector.Add(DetailTokenType.TypeHandle, range);
                        break;
                    case TypeCode.Interface:
                        collector.Add(DetailTokenType.TypeInterface, range);
                        break;
                    case TypeCode.Delegate:
                        collector.Add(DetailTokenType.TypeDelegate, range);
                        break;
                    case TypeCode.Task:
                        collector.Add(DetailTokenType.TypeTask, range);
                        break;
                    default:
                        collector.Add(DetailTokenType.Label, range);
                        break;
                }
        }
        public static void AddType(this SemanticTokenCollector collector, FileType file, Manager manager, Type type)
        {
            collector.AddNamespace(file.name);
            collector.AddType(file.name.name, manager, type);
            if (file.name.name.end < file.range.end)
                collector.Add(DetailTokenType.Operator, file.name.name.end & file.range.end);
        }
        public static void Add(this SemanticTokenCollector collector, DetailTokenType type, TextRange range)
        {
            if (range.Count == 0) return;
            switch (type)
            {
                case DetailTokenType.GlobalVariable:
                    collector.AddRange(SemanticTokenType.Variable, SemanticTokenModifier.Static, range);
                    break;
                case DetailTokenType.GlobalFunction:
                    collector.AddRange(SemanticTokenType.Function, SemanticTokenModifier.Static, range);
                    break;
                case DetailTokenType.NativeFunction:
                    collector.AddRange(SemanticTokenType.Function, SemanticTokenModifier.Static, range);
                    break;

                case DetailTokenType.TypeEnum:
                    collector.AddRange(SemanticTokenType.Enum, SemanticTokenModifier.Definition, range);
                    break;
                case DetailTokenType.TypeStruct:
                    collector.AddRange(SemanticTokenType.Struct, SemanticTokenModifier.Definition, range);
                    break;
                case DetailTokenType.TypeInterface:
                    collector.AddRange(SemanticTokenType.Interface, SemanticTokenModifier.Definition, range);
                    break;
                case DetailTokenType.TypeHandle:
                    collector.AddRange(SemanticTokenType.Class, SemanticTokenModifier.Definition, range);
                    break;
                case DetailTokenType.TypeDelegate:
                    collector.AddRange(SemanticTokenType.Event, SemanticTokenModifier.Definition, range);
                    break;
                case DetailTokenType.TypeTask:
                    collector.AddRange(SemanticTokenType.Event, SemanticTokenModifier.Definition, range);
                    break;

                case DetailTokenType.MemberElement:
                    collector.AddRange(SemanticTokenType.EnumMember, SemanticTokenModifier.Readonly, range);
                    break;
                case DetailTokenType.MemberField:
                    collector.AddRange(SemanticTokenType.Variable, SemanticTokenModifier.Documentation, range);
                    break;
                case DetailTokenType.MemberFunction:
                    collector.AddRange(SemanticTokenType.Function, SemanticTokenModifier.Documentation, range);
                    break;
                case DetailTokenType.MemberConstructor:
                    collector.AddRange(SemanticTokenType.Class, SemanticTokenModifier.Documentation, range);
                    break;

                case DetailTokenType.Constant:
                    collector.AddRange(SemanticTokenType.Macro, SemanticTokenModifier.Readonly, range);
                    break;
                case DetailTokenType.Parameter:
                    collector.AddRange(SemanticTokenType.Parameter, SemanticTokenModifier.Modification, range);
                    break;
                case DetailTokenType.Local:
                    collector.AddRange(SemanticTokenType.Variable, SemanticTokenModifier.Modification, range);
                    break;
                case DetailTokenType.DeprecatedLocal:
                    collector.AddRange(SemanticTokenType.Variable, SemanticTokenModifier.Deprecated, range);
                    break;

                case DetailTokenType.KeywordCtrl:
                    collector.AddRange(SemanticTokenType.Keyword, SemanticTokenModifier.Async, range);
                    break;
                case DetailTokenType.KeywordType:
                    collector.AddRange(SemanticTokenType.Type, SemanticTokenModifier.DefaultLibrary, range);
                    break;
                case DetailTokenType.KeywordVariable:
                    collector.AddRange(SemanticTokenType.Keyword, SemanticTokenModifier.DefaultLibrary, range);
                    break;
                case DetailTokenType.KeywordConst:
                    //collector.AddRange(SemanticTokenType.Regexp, SemanticTokenModifier.Documentation, range);
                    break;

                case DetailTokenType.Numeric:
                    collector.AddRange(SemanticTokenType.Number, SemanticTokenModifier.Readonly, range);
                    break;
                case DetailTokenType.String:
                    //collector.AddRange(SemanticTokenType.String, SemanticTokenModifier.Readonly, range);
                    break;

                case DetailTokenType.Namespace:
                    collector.AddRange(SemanticTokenType.Namespace, SemanticTokenModifier.Documentation, range);
                    break;
                case DetailTokenType.Label:
                    collector.AddRange(SemanticTokenType.Label, SemanticTokenModifier.Documentation, range);
                    break;

                case DetailTokenType.Operator:
                    collector.AddRange(SemanticTokenType.Operator, SemanticTokenModifier.Documentation, range);
                    break;
            }
        }
    }
}