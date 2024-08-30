using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RainLanguageServer.RainLanguage
{
    internal readonly struct Context(TextDocument document, AbstractSpace space, HashSet<AbstractSpace> relies, AbstractDeclaration? declaration)
    {
        public readonly TextDocument document = document;
        public readonly AbstractSpace space = space;
        public readonly HashSet<AbstractSpace> relies = relies;
        public readonly AbstractDeclaration? declaration = declaration;
        public Context(Context context, AbstractDeclaration? declaration) : this(context.document, context.space, context.relies, declaration) { }
        private bool IsVisiable(Manager manager, Declaration declaration, bool isMember)
        {
            if (isMember)
            {
                if (!manager.TryGetDefineDeclaration(declaration, out var abstractDeclaration)) return false;
                if (abstractDeclaration == this.declaration) return true;
                if (IsVisiable(manager, abstractDeclaration.declaration, false))
                    if (declaration.visibility.ContainAny(Visibility.Public | Visibility.Internal)) return true;
                    else if (declaration.visibility.ContainAny(Visibility.Space)) return abstractDeclaration.space.Contain(space);
                if (declaration.category == DeclarationCategory.ClassVariable || declaration.category == DeclarationCategory.Constructor || declaration.category == DeclarationCategory.ClassFunction)
                {
                    foreach (var index in manager.GetInheritIterator(this.declaration as AbstractClass))
                        if (index == abstractDeclaration) return true;
                    return false;
                }
            }
            else
            {
                if (declaration.visibility.ContainAny(Visibility.Public | Visibility.Internal)) return true;
                if (!manager.TryGetDeclaration(declaration, out var abstractDeclaration)) return false;
                if (abstractDeclaration.space.Contain(space))
                {
                    if (declaration.visibility.ContainAny(Visibility.Space)) return true;
                    else return abstractDeclaration.file.space.document == document;
                }
            }
            return false;
        }
        public bool IsVisiable(Manager manager, Declaration declaration)
        {
            if (declaration.library == Manager.LIBRARY_KERNEL) return true;
            else if (declaration.library == Manager.LIBRARY_SELF)
            {
                switch (declaration.category)
                {
                    case DeclarationCategory.Invalid: break;
                    case DeclarationCategory.Variable:
                    case DeclarationCategory.Function:
                    case DeclarationCategory.Enum:
                        return IsVisiable(manager, declaration, false);
                    case DeclarationCategory.EnumElement:
                        return IsVisiable(manager, declaration, true);
                    case DeclarationCategory.Struct:
                        return IsVisiable(manager, declaration, false);
                    case DeclarationCategory.StructVariable:
                        return IsVisiable(manager, declaration, true);
                    case DeclarationCategory.StructFunction:
                        return IsVisiable(manager, declaration, true);
                    case DeclarationCategory.Class:
                        return IsVisiable(manager, declaration, false);
                    case DeclarationCategory.Constructor:
                    case DeclarationCategory.ClassVariable:
                    case DeclarationCategory.ClassFunction:
                        return IsVisiable(manager, declaration, true);
                    case DeclarationCategory.Interface:
                        return IsVisiable(manager, declaration, false);
                    case DeclarationCategory.InterfaceFunction:
                        return IsVisiable(manager, declaration, true);
                    case DeclarationCategory.Delegate:
                    case DeclarationCategory.Task:
                    case DeclarationCategory.Native:
                        return IsVisiable(manager, declaration, false);
                }
            }
            else
            {
                switch (declaration.category)
                {
                    case DeclarationCategory.Invalid: break;
                    case DeclarationCategory.Variable:
                    case DeclarationCategory.Function:
                    case DeclarationCategory.Enum:
                        return declaration.visibility.ContainAny(Visibility.Public);
                    case DeclarationCategory.EnumElement:
                        return manager.TryGetDefineDeclaration(declaration, out var abstractDeclaration) && abstractDeclaration.declaration.visibility.ContainAny(Visibility.Public);
                    case DeclarationCategory.Struct:
                        return declaration.visibility.ContainAny(Visibility.Public);
                    case DeclarationCategory.StructVariable:
                        return manager.TryGetDefineDeclaration(declaration, out abstractDeclaration) && abstractDeclaration.declaration.visibility.ContainAny(Visibility.Public);
                    case DeclarationCategory.StructFunction:
                        return manager.TryGetDefineDeclaration(declaration, out abstractDeclaration) && abstractDeclaration.declaration.visibility.ContainAny(Visibility.Public) && declaration.visibility.ContainAny(Visibility.Public);
                    case DeclarationCategory.Class:
                        return declaration.visibility.ContainAny(Visibility.Public);
                    case DeclarationCategory.Constructor:
                    case DeclarationCategory.ClassVariable:
                    case DeclarationCategory.ClassFunction:
                        if (manager.TryGetDefineDeclaration(declaration, out abstractDeclaration) && abstractDeclaration.declaration.visibility.ContainAny(Visibility.Public))
                            if (declaration.visibility.ContainAny(Visibility.Public)) return true;
                            else if (this.declaration != null && this.declaration.declaration.category == DeclarationCategory.Class && declaration.visibility.ContainAny(Visibility.Protected))
                            {
                                foreach (var index in manager.GetInheritIterator(this.declaration as AbstractClass))
                                    if (index == abstractDeclaration) return true;
                                return false;
                            }
                        break;
                    case DeclarationCategory.Interface:
                        return declaration.visibility.ContainAny(Visibility.Public);
                    case DeclarationCategory.InterfaceFunction:
                        return manager.TryGetDefineDeclaration(declaration, out abstractDeclaration) && abstractDeclaration.declaration.visibility.ContainAny(Visibility.Public);
                    case DeclarationCategory.Delegate:
                    case DeclarationCategory.Task:
                    case DeclarationCategory.Native:
                        return declaration.visibility.ContainAny(Visibility.Public);
                }
            }
            return false;
        }
        public bool TryFindSpace(Manager manager, TextRange name, [MaybeNullWhen(false)] out AbstractSpace result, MessageCollector collector)
        {
            var targetName = name.ToString();
            for (var index = space; index != null; index = index.parent)
                if (index.children.TryGetValue(targetName, out result))
                    return true;
            var results = new HashSet<AbstractSpace>();
            foreach (var rely in relies)
                if (rely.children.TryGetValue(targetName, out result))
                    results.Add(rely);
            if (manager.relies.TryGetValue(targetName, out var library))
                results.Add(library);
            if (results.Count > 0)
            {
                if (results.Count > 1)
                {
                    var message = new StringBuilder().AppendLine("依赖的命名空间不明确：");
                    foreach (var target in results)
                        message.AppendLine(target.FullName);
                    collector.Add(name, ErrorLevel.Error, message.ToString());
                }
                result = results.First();
                return true;
            }
            else if (targetName == Manager.KERNEL)
            {
                result = manager.kernel;
                return true;
            }
            result = default;
            return false;
        }
        public bool TryFindMember<T>(Manager manager, TextRange name, Type type, out List<T> members) where T : AbstractDeclaration
        {
            members = [];
            if (type.dimension > 0) type = manager.kernelManager.ARRAY;
            else if (type.code == TypeCode.Enum) type = manager.kernelManager.ENUM;
            else if (type.code == TypeCode.Task) type = manager.kernelManager.TASK;
            else if (type.code == TypeCode.Delegate) type = manager.kernelManager.DELEGATE;
            var memberName = name.ToString();
            if (manager.TryGetDeclaration(type, out var declaration))
            {
                if (declaration is AbstractStruct abstractStruct)
                {
                    foreach (var member in abstractStruct.variables)
                        if (member.name == memberName)
                        {
                            if (member is T value)
                                members.Add(value);
                            return true;
                        }
                    foreach (var member in abstractStruct.functions)
                        if (member.name == memberName && IsVisiable(manager, member.declaration) && member is T value)
                            members.Add(value);
                }
                else if (declaration is AbstractClass abstractClass)
                {
                    var filter = new HashSet<AbstractCallable>();
                    foreach (var index in manager.GetInheritIterator(abstractClass))
                    {
                        if (members.Count == 0)
                            foreach (var member in index.variables)
                                if (member.name == memberName && IsVisiable(manager, member.declaration))
                                {
                                    if (member is T value)
                                        members.Add(value);
                                    return true;
                                }
                        foreach (var member in index.functions)
                            if (member.name == memberName && IsVisiable(manager, member.declaration) && filter.Add(member) && member is T value)
                            {
                                members.Add(value);
                                filter.AddRange(member.overrides);
                            }
                    }
                }
                else if (declaration is AbstractInterface abstractInterface)
                {
                    var filter = new HashSet<AbstractCallable>();
                    foreach (var index in manager.GetInheritIterator(abstractInterface))
                        foreach (var member in index.functions)
                            if (filter.Add(member) && member is T value)
                            {
                                members.Add(value);
                                filter.AddRange(member.overrides);
                            }
                }
            }
            return members.Count > 0;
        }
        public bool TryFindDeclaration(Manager manager, string name, [MaybeNullWhen(false)] out List<AbstractDeclaration> results)
        {
            results = [];
            if (declaration != null)
            {
                if (declaration is AbstractStruct abstractStruct)
                {
                    foreach (var variable in abstractStruct.variables)
                        if (variable.name == name && IsVisiable(manager, variable.declaration))
                            results.Add(variable);
                    foreach (var function in abstractStruct.functions)
                        if (function.name == name && IsVisiable(manager, function.declaration))
                            results.Add(function);
                }
                else if (declaration is AbstractClass abstractClass)
                    foreach (var index in manager.GetInheritIterator(abstractClass))
                    {
                        foreach (var variable in index.variables)
                            if (variable.name == name && IsVisiable(manager, variable.declaration))
                                results.Add(variable);
                        foreach (var function in index.functions)
                            if (function.name == name && IsVisiable(manager, function.declaration))
                                results.Add(function);
                    }
                if (results.Count > 0) return true;
            }
            for (var index = space; index != null; index = index.parent)
                if (index.declarations.TryGetValue(name, out var declarations))
                {
                    foreach (var declaration in declarations)
                        if (IsVisiable(manager, declaration) && manager.TryGetDeclaration(declaration, out var result))
                            results.Add(result);
                    if (results.Count > 0) return true;
                }
            foreach (var rely in relies)
                if (rely.declarations.TryGetValue(name, out var declarations))
                {
                    foreach (var declaration in declarations)
                        if (IsVisiable(manager, declaration) && manager.TryGetDeclaration(declaration, out var result))
                            results.Add(result);
                }
            return results.Count > 0;
        }
        public List<AbstractDeclaration> FindDeclaration(Manager manager, TextRange name, MessageCollector collector)
        {
            if (TryFindDeclaration(manager, name.ToString(), out var results)) return results;
            else
            {
                collector.Add(name, ErrorLevel.Error, "声明未找到");
                return [];
            }
        }
        public List<AbstractDeclaration> FindDeclaration(Manager manager, List<TextRange> names, MessageCollector collector)
        {
            if (names.Count > 1)
            {
                if (TryFindSpace(manager, names[0], out var space, collector))
                {
                    for (var i = 1; i < names.Count - 1; i++)
                        if (!space.children.TryGetValue(names[i].ToString(), out space))
                        {
                            collector.Add(names[i], ErrorLevel.Error, "命名空间未找到");
                            return [];
                        }
                    if (space.declarations.TryGetValue(names[^1].ToString(), out var declarations)) return manager.ToDeclarations(declarations);
                    else collector.Add(names[^1], ErrorLevel.Error, $"没有找到名称为 {names[^1]} 的声明");
                }
                else collector.Add(names[0], ErrorLevel.Error, "命名空间未找到");
                return [];
            }
            else return FindDeclaration(manager, names[0], collector);
        }
        public List<AbstractDeclaration> FindDeclaration(Manager manager, QualifiedName name, MessageCollector collector)
        {
            if (name.qualify.Count > 0)
            {
                if (TryFindSpace(manager, name.qualify[0], out var space, collector))
                {
                    for (var i = 1; i < name.qualify.Count; i++)
                        if (!space.children.TryGetValue(name.qualify[i].ToString(), out space))
                        {
                            collector.Add(name.qualify[i], ErrorLevel.Error, "命名空间未找到");
                            return [];
                        }
                    if (space.declarations.TryGetValue(name.name.ToString(), out var declarations)) return manager.ToDeclarations(declarations);
                    else collector.Add(name.name, ErrorLevel.Error, $"没有找到名称为 {name.name} 的声明");
                }
                else collector.Add(name.qualify[0], ErrorLevel.Error, "命名空间未找到");
                return [];
            }
            else return FindDeclaration(manager, name.name, collector);
        }
        public List<AbstractDeclaration> FindOperation(Manager manager, string name)
        {
            var set = new HashSet<Declaration>();
            if (manager.kernel.declarations.TryGetValue(name, out var declarations))
                set.AddRange(declarations);
            for (var index = space; index != null; index = index.parent)
                if (index.declarations.TryGetValue(name, out declarations))
                    set.AddRange(declarations);
            foreach (var rely in relies)
                if (rely.declarations.TryGetValue(name, out declarations))
                    set.AddRange(declarations);
            var result = new List<AbstractDeclaration>();
            foreach (var declaration in set)
                if (manager.TryGetDeclaration(declaration, out var abstractDeclaration))
                    result.Add(abstractDeclaration);
            return result;
        }
    }
}
