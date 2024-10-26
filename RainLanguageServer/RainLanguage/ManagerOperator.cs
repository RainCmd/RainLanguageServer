using LanguageServer;
using LanguageServer.Parameters;
using LanguageServer.Parameters.TextDocument;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RainLanguageServer.RainLanguage
{
    internal readonly struct CodeLenInfo(TextRange range, string title, string command = "", dynamic[]? arguments = null)
    {
        public readonly TextRange range = range;
        public readonly string title = title;
        public readonly string command = command;
        public readonly dynamic[]? arguments = arguments;
    }
    internal static class ManagerOperator
    {
        private static TextPosition ToTextPosition(this Position position, TextDocument document) => document[(int)position.line].start + (int)position.character;
        private static bool TryGetFileSpace(Manager manager, DocumentUri uri, Position position, [MaybeNullWhen(false)] out FileSpace space, out TextPosition textPosition)
        {
            if (manager.allFileSpaces.TryGetValue(new UnifiedPath(uri), out space))
            {
                textPosition = position.ToTextPosition(space.document);
                return space.range.Contain(textPosition);
            }
            textPosition = default;
            return false;
        }

        private delegate bool FileSpaceAction(FileSpace space);
        private delegate bool FileDeclarationAction(FileDeclaration declaration);
        private static bool FileDeclarationOperator<T>(List<T> list, TextPosition position, out bool result, FileDeclarationAction action) where T : FileDeclaration
        {
            result = false;
            foreach (var item in list)
                if (item.range.Contain(position))
                {
                    result = action(item);
                    return true;
                }
            return false;
        }
        private static bool FileSpaceOperator(FileSpace space, TextPosition position, FileSpaceAction? spaceAction, FileDeclarationAction declarationAction)
        {
            foreach (var child in space.children)
                if (child.range.Contain(position))
                    return FileSpaceOperator(child, position, spaceAction, declarationAction);
            if (FileDeclarationOperator(space.variables, position, out var result, declarationAction)) return result;
            if (FileDeclarationOperator(space.functions, position, out result, declarationAction)) return result;
            if (FileDeclarationOperator(space.enums, position, out result, declarationAction)) return result;
            if (FileDeclarationOperator(space.structs, position, out result, declarationAction)) return result;
            if (FileDeclarationOperator(space.interfaces, position, out result, declarationAction)) return result;
            if (FileDeclarationOperator(space.classes, position, out result, declarationAction)) return result;
            if (FileDeclarationOperator(space.delegates, position, out result, declarationAction)) return result;
            if (FileDeclarationOperator(space.tasks, position, out result, declarationAction)) return result;
            if (FileDeclarationOperator(space.natives, position, out result, declarationAction)) return result;
            if (spaceAction != null) return spaceAction(space);
            return false;
        }
        private static void FileSpaceOperator(FileSpace space, Action<FileSpace>? spaceAction, Action<FileDeclaration>? declarationAction)
        {
            foreach (var child in space.children)
                FileSpaceOperator(child, spaceAction, declarationAction);
            spaceAction?.Invoke(space);
            if (declarationAction != null)
            {
                foreach (var declaration in space.variables) declarationAction(declaration);
                foreach (var declaration in space.functions) declarationAction(declaration);
                foreach (var declaration in space.enums) declarationAction(declaration);
                foreach (var declaration in space.structs) declarationAction(declaration);
                foreach (var declaration in space.interfaces) declarationAction(declaration);
                foreach (var declaration in space.classes) declarationAction(declaration);
                foreach (var declaration in space.delegates) declarationAction(declaration);
                foreach (var declaration in space.tasks) declarationAction(declaration);
                foreach (var declaration in space.natives) declarationAction(declaration);
            }
        }
        private static void FileSpaceOperator(Manager manager, DocumentUri uri, Action<FileSpace>? spaceAction, Action<FileDeclaration>? declarationAction)
        {
            if (manager.allFileSpaces.TryGetValue(new UnifiedPath(uri), out var space))
                FileSpaceOperator(space, spaceAction, declarationAction);
        }

        public static bool OnHover(Manager manager, DocumentUri uri, Position position, out HoverInfo info)
        {
            lock (manager)
                if (TryGetFileSpace(manager, uri, position, out var space, out var textPosition))
                {
                    HoverInfo result = default;
                    if (FileSpaceOperator(space, textPosition, fileSpace =>
                        {
                            if (fileSpace.name != null && fileSpace.name.Value.Contain(textPosition))
                            {
                                var sb = new StringBuilder();
                                sb.Append(KeyWords.NAMESPACE);
                                sb.Append(' ');
                                sb.Append(fileSpace.name.Value);
                                result = new HoverInfo(fileSpace.name.Value, sb.ToString().MakedownCode(), true);
                                return true;
                            }
                            return false;
                        },
                        fileDeclaration =>
                        {
                            if (fileDeclaration.abstractDeclaration != null)
                                return fileDeclaration.abstractDeclaration.OnHover(manager, textPosition, out result);
                            return false;
                        }))
                    {
                        info = result;
                        return true;
                    }
                }
            info = default;
            return false;
        }
        public static bool OnHighlight(Manager manager, DocumentUri uri, Position position, out List<HighlightInfo> result)
        {
            var infos = result = [];
            lock (manager)
                if (TryGetFileSpace(manager, uri, position, out var space, out var textPosition))
                {
                    if (FileSpaceOperator(space, textPosition, fileSpace =>
                        {
                            if (fileSpace.name != null && fileSpace.name.Value.Contain(textPosition))
                            {
                                foreach (var reference in fileSpace.space.references)
                                    infos.Add(new HighlightInfo(reference, DocumentHighlightKind.Text));
                                return true;
                            }
                            return false;
                        },
                        fileDeclaration =>
                        {
                            if (fileDeclaration.abstractDeclaration != null)
                                return fileDeclaration.abstractDeclaration.OnHighlight(manager, textPosition, infos);
                            return false;
                        }))
                    {
                        infos.RemoveAll(value => value.range.start.document != space.document);
                        return true;
                    }
                }
            return false;
        }
        public static bool TryGetDefinition(Manager manager, DocumentUri uri, Position position, out TextRange definition)
        {
            lock (manager)
                if (TryGetFileSpace(manager, uri, position, out var space, out var textPosition))
                {
                    TextRange result = default;
                    if (FileSpaceOperator(space, textPosition,
                        fileSpace =>
                        {
                            if (fileSpace.name != null && fileSpace.name.Value.Contain(textPosition))
                            {
                                result = fileSpace.name.Value;
                                return true;
                            }
                            foreach (var import in fileSpace.imports)
                                if (import.range.Contain(textPosition))
                                {
                                    result = import.range;
                                    return true;
                                }
                            return false;
                        },
                        fileDeclaratioin =>
                        {
                            if (fileDeclaratioin.abstractDeclaration != null)
                                return fileDeclaratioin.abstractDeclaration.TryGetDefinition(manager, textPosition, out result);
                            return false;
                        }))
                    {
                        definition = result;
                        return true;
                    }
                }
            definition = default;
            return false;
        }
        public static bool FindReferences(Manager manager, DocumentUri uri, Position position, out List<TextRange> result)
        {
            var references = result = [];
            lock (manager)
                if (TryGetFileSpace(manager, uri, position, out var space, out var textPosition))
                {
                    return FileSpaceOperator(space, textPosition, fileSpace =>
                        {
                            if (fileSpace.name != null && fileSpace.name.Value.Contain(textPosition))
                            {
                                references.AddRange(fileSpace.space.references);
                                return true;
                            }
                            foreach (var import in fileSpace.imports)
                                if (import.range.Contain(textPosition))
                                {
                                    if (import.space != null)
                                        for (var i = 0; i < import.names.Count; i++)
                                            if (import.names[i].Contain(textPosition))
                                            {
                                                var importReferences = import.GetSpace(i)?.references;
                                                if (importReferences != null) references.AddRange(importReferences);
                                                return true;
                                            }
                                    return false;
                                }
                            return false;
                        },
                        fileDeclaration =>
                        {
                            if (fileDeclaration.abstractDeclaration != null)
                                return fileDeclaration.abstractDeclaration.FindReferences(manager, textPosition, references);
                            return false;
                        });
                }
            return false;
        }

        public static SemanticTokenCollector CollectSemanticToken(Manager manager, DocumentUri uri)
        {
            var collector = new SemanticTokenCollector();
            lock (manager)
                FileSpaceOperator(manager, uri,
                    space =>
                    {
                        if (space.name != null)
                            collector.Add(DetailTokenType.Namespace, space.name.Value);
                        foreach (var import in space.imports)
                            for (var i = 0; i < import.names.Count; i++)
                            {
                                if (i > 0) collector.Add(DetailTokenType.Operator, import.names[i - 1].end & import.names[i].start);
                                collector.Add(DetailTokenType.Namespace, import.names[i]);
                            }
                    },
                    declaration => declaration.abstractDeclaration?.CollectSemanticToken(manager, collector));
            return collector;
        }

        [RequiresDynamicCode("Calls RainLanguageServer.RainLanguage.ManagerOperator.GetReferenceParameter(FileDeclaration, HashSet<TextRange>)")]
        private static CodeLenInfo GetReferenceInfo(FileDeclaration file, string title, int count)
        {
            if (count > 0)
            {
                var line = file.name.start.Line;
                return new CodeLenInfo(file.name, $"{title}：{count}", "cmd.rain.peek-reference", [new Position(line.line, file.name.start - line.start)]);
            }
            return new CodeLenInfo(file.name, $"{title}:{count}");
        }
        [RequiresDynamicCode("Calls RainLanguageServer.RainLanguage.ManagerOperator.GetReferenceInfo(FileDeclaration, HashSet<TextRange>, String)")]
        private static CodeLenInfo GetCodeLenInfo<T>(FileDeclaration file, List<T> values, string title) where T : AbstractDeclaration
        {
            return GetReferenceInfo(file, title, values.Count);
        }
        [RequiresDynamicCode("Calls RainLanguageServer.RainLanguage.ManagerOperator.GetReferenceParameter(FileDeclaration, TextRange, HashSet<TextRange>)")]
        private static void CollectCodeLens(Manager manager, FileSpace space, List<CodeLenInfo> infos)
        {
            foreach (var child in space.children)
                CollectCodeLens(manager, child, infos);
            //foreach (var file in space.variables)
            //    if (file.abstractDeclaration is AbstractVariable abstractVariable)
            //    {
            //        infos.Add(GetReferenceInfo(file, "读取", abstractVariable.references.Count));
            //        infos.Add(GetReferenceInfo(file, "写入", abstractVariable.write.Count));
            //    }
            foreach (var file in space.functions)
                if (file.abstractDeclaration is AbstractFunction abstractFunction)
                {
                    infos.Add(GetReferenceInfo(file, "引用", abstractFunction.references.Count));
                    if (abstractFunction.parameters.Count == 0)
                        infos.Add(new CodeLenInfo(abstractFunction.name, $"执行", "cmd.rain.execute", [$"{abstractFunction.space.FullName}.{abstractFunction.name}"]));
                }
            foreach (var file in space.enums)
                if (file.abstractDeclaration is AbstractEnum abstractEnum)
                    infos.Add(GetReferenceInfo(file, "引用", abstractEnum.references.Count));
            foreach (var file in space.structs)
                if (file.abstractDeclaration is AbstractStruct abstractStruct)
                {
                    infos.Add(GetReferenceInfo(file, "引用", abstractStruct.references.Count));
                    //foreach (var member in abstractStruct.variables)
                    //{
                    //    infos.Add(GetReferenceInfo(member.file, "读取", member.references.Count));
                    //    infos.Add(GetReferenceInfo(member.file, "写入", member.write.Count));
                    //}
                    foreach (var member in abstractStruct.functions)
                        infos.Add(GetReferenceInfo(member.file, "读取", member.references.Count));
                }
            foreach (var file in space.interfaces)
                if (file.abstractDeclaration is AbstractInterface abstractInterface)
                {
                    infos.Add(GetReferenceInfo(file, "引用", abstractInterface.references.Count));
                    infos.Add(GetCodeLenInfo(file, abstractInterface.implements, "实现"));
                    foreach (var member in abstractInterface.functions)
                    {
                        infos.Add(GetReferenceInfo(member.file, "引用", member.references.Count));
                        infos.Add(GetCodeLenInfo(member.file, member.implements, "实现"));
                    }
                }
            foreach (var file in space.classes)
                if (file.abstractDeclaration is AbstractClass abstractClass)
                {
                    infos.Add(GetReferenceInfo(file, "引用", abstractClass.references.Count));
                    infos.Add(GetCodeLenInfo(file, abstractClass.implements, "子类"));
                    //foreach (var member in abstractClass.variables)
                    //{
                    //    infos.Add(GetReferenceInfo(member.file, "读取", member.references.Count));
                    //    infos.Add(GetReferenceInfo(member.file, "写入", member.write.Count));
                    //}
                    foreach (var member in abstractClass.functions)
                    {
                        infos.Add(GetReferenceInfo(member.file, "引用", member.references.Count));
                        infos.Add(GetCodeLenInfo(member.file, member.implements, "实现"));
                        infos.Add(GetCodeLenInfo(member.file, member.overrides, "覆盖"));
                    }
                    foreach (var member in abstractClass.constructors)
                        infos.Add(GetReferenceInfo(member.file, "引用", member.references.Count));
                }
            foreach (var file in space.delegates)
                if (file.abstractDeclaration is AbstractDelegate abstractDelegate)
                    infos.Add(GetReferenceInfo(file, "引用", abstractDelegate.references.Count));
            foreach (var file in space.tasks)
                if (file.abstractDeclaration is AbstractTask abstractTask)
                    infos.Add(GetReferenceInfo(file, "引用", abstractTask.references.Count));
            foreach (var file in space.natives)
                if (file.abstractDeclaration is AbstractNative abstractNative)
                    infos.Add(GetReferenceInfo(file, "引用", abstractNative.references.Count));
        }

        [RequiresDynamicCode("Calls RainLanguageServer.RainLanguage.ManagerOperator.CollectCodeLens(Manager, FileSpace, List<CodeLenInfo>)")]
        public static List<CodeLenInfo> CollectCodeLens(Manager manager, DocumentUri uri)
        {
            var results = new List<CodeLenInfo>();
            lock (manager)
                if (manager.allFileSpaces.TryGetValue(new UnifiedPath(uri), out var space))
                    CollectCodeLens(manager, space, results);
            return results;
        }

        public static bool TrySignatureHelp(Manager manager, DocumentUri uri, Position position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            lock (manager)
                if (TryGetFileSpace(manager, uri, position, out var space, out var textPosition))
                {
                    List<SignatureInfo>? results = null;
                    int function = 0; int parameter = 0;
                    if (FileSpaceOperator(space, textPosition, null,
                        fileDeclaration =>
                        {
                            if (fileDeclaration.abstractDeclaration != null)
                                return fileDeclaration.abstractDeclaration.TrySignatureHelp(manager, textPosition, out results, out function, out parameter);
                            return false;
                        }))
                    {
                        infos = results!;
                        functionIndex = function;
                        parameterIndex = parameter;
                        return true;
                    }
                }
            infos = default;
            functionIndex = -1;
            parameterIndex = -1;
            return false;
        }

        public static bool TryRename(Manager manager, DocumentUri uri, Position position, [MaybeNullWhen(false)] out HashSet<TextRange> edits)
        {
            lock (manager)
                if (TryGetFileSpace(manager, uri, position, out var space, out var textPosition) && space.space.Library.library == Manager.LIBRARY_SELF)
                {
                    var result = new HashSet<TextRange>();
                    if (FileSpaceOperator(space, textPosition, fileSpace =>
                    {
                        if (fileSpace.name != null && fileSpace.name.Value.Contain(textPosition))
                        {
                            result.AddRange(fileSpace.space.references);
                            return true;
                        }
                        return false;
                    },
                        fileDeclaration =>
                        {
                            fileDeclaration.abstractDeclaration?.Rename(manager, textPosition, result);
                            return true;
                        }))
                    {
                        edits = result;
                        return true;
                    }
                }
            edits = default;
            return false;
        }

        public static void Completion(Manager manager, DocumentUri uri, Position position, List<CompletionInfo> infos)
        {
            lock (manager)
                if (TryGetFileSpace(manager, uri, position, out var space, out var textPosition))
                {
                    FileSpaceOperator(space, textPosition,
                        fileSpace =>
                        {
                            if (fileSpace.name != null && fileSpace.name.Value.Contain(textPosition))
                            {
                                if (fileSpace.parent != null)
                                    InfoUtility.CollectChildrenSpaces(infos, fileSpace.parent.space);
                                return true;
                            }
                            var context = new Context(fileSpace.document, fileSpace.space, fileSpace.relies, null);
                            foreach (var info in fileSpace.imports)
                                if (info.range.Contain(textPosition))
                                {
                                    for (var i = 0; i < info.names.Count; i++)
                                        if (info.names[i].Contain(textPosition))
                                        {
                                            if (i == 0) InfoUtility.CollectSpaces(manager, infos, context.space, context.relies);
                                            else
                                            {
                                                var indexSpace = info.space;
                                                for (var index = 1; index < i && indexSpace != null; index++)
                                                    indexSpace.children.TryGetValue(info.names[index].ToString(), out indexSpace);
                                                if (indexSpace != null)
                                                    InfoUtility.CollectChildrenSpaces(infos, indexSpace);
                                            }
                                            return true;
                                        }
                                    return true;
                                }
                            InfoUtility.Completion(manager, context, textPosition.Line, textPosition, infos, true, true, true);
                            return default;
                        },
                        fileDeclaration =>
                        {
                            fileDeclaration.abstractDeclaration?.Completion(manager, textPosition, infos);
                            return default;
                        });
                }
        }

        public static void CollectInlayHint(Manager manager, DocumentUri uri, List<InlayHintInfo> infos)
        {
            lock (manager)
                FileSpaceOperator(manager, uri, null, declaration => declaration.abstractDeclaration?.CollectInlayHint(manager, infos));
        }

        public static AbstractSpace? GetSpace(Manager manager, TextPosition position) => GetFileSpace(manager, position)?.space;
        public static FileSpace? GetFileSpace(Manager manager, TextPosition position)
        {
            if (manager.allFileSpaces.TryGetValue(position.document.path, out var result))
                return GetFileSpace(result, position);
            return null;
        }
        private static FileSpace GetFileSpace(FileSpace space, TextPosition position)
        {
            foreach (var child in space.children)
                if (child.range.Contain(position))
                    return GetFileSpace(child, position);
            return space;
        }
        public static bool TryGetContext(Manager manager, TextPosition position, out Context context)
        {
            if (manager.allFileSpaces.TryGetValue(position.document.path, out var result))
            {
                var space = GetFileSpace(result, position);
                foreach (var declaration in space.structs)
                    if (declaration.range.Contain(position))
                    {
                        context = new Context(position.document, space.space, space.relies, declaration.abstractDeclaration);
                        return true;
                    }
                foreach (var declaration in space.classes)
                    if (declaration.range.Contain(position))
                    {
                        context = new Context(position.document, space.space, space.relies, declaration.abstractDeclaration);
                        return true;
                    }
                context = new Context(position.document, space.space, space.relies, null);
                return true;
            }
            context = default;
            return false;
        }
    }
}
