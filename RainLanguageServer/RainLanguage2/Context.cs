﻿using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RainLanguageServer.RainLanguage2
{
    internal readonly struct Context(TextDocument document, AbstractSpace space, HashSet<AbstractSpace> relies, AbstractDeclaration? declaration)
    {
        public readonly TextDocument document = document;
        public readonly AbstractSpace space = space;
        public readonly HashSet<AbstractSpace> relies = relies;
        public readonly AbstractDeclaration? declaration = declaration;
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
                    for (var index = this.declaration as AbstractClass; index != null;)
                    {
                        if (index == abstractDeclaration) return true;
                        if (!manager.TryGetDeclaration(index.parent, out var parentDeclaration)) return false;
                        index = parentDeclaration as AbstractClass;
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
                                for (var index = this.declaration as AbstractClass; index != null;)
                                {
                                    if (index == abstractDeclaration) return true;
                                    if (!manager.TryGetDeclaration(index.parent, out var parentDeclaration)) return false;
                                    index = parentDeclaration as AbstractClass;
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
        public List<AbstractDeclaration> FindDeclaration(Manager manager, TextRange name, MessageCollector collector)
        {
            var results = new List<AbstractDeclaration>();
            var targetName = name.ToString();
            if (declaration != null)
            {
                if (declaration is AbstractStruct abstractStruct)
                {
                    foreach (var variable in abstractStruct.variables)
                        if (variable.name == targetName)
                            results.Add(variable);
                    foreach (var function in abstractStruct.functions)
                        if (function.name == targetName)
                            results.Add(function);
                }
                else if (declaration is AbstractClass abstractClass)
                {
                    for (var index = abstractClass; index != null;)
                    {
                        foreach (var variable in index.variables)
                            if (variable.name == targetName)
                                results.Add(variable);
                        foreach (var function in index.functions)
                            if (function.name == targetName)
                                results.Add(function);
                        if (manager.TryGetDeclaration(index.parent, out var parent)) index = parent as AbstractClass;
                        else break;
                    }
                }
                if (results.Count > 0) return results;
            }
            for (var index = space; index != null; index = index.parent)
                if (index.declarations.TryGetValue(targetName, out var declarations))
                    return manager.ToDeclarations(declarations);
            foreach (var rely in relies)
                if (rely.declarations.TryGetValue(targetName, out var declarations))
                    return manager.ToDeclarations(declarations);
            collector.Add(name, ErrorLevel.Error, "声明未找到");
            return [];
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
