using LanguageServer;
using LanguageServer.Parameters;
using LanguageServer.Parameters.TextDocument;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RainLanguageServer.RainLanguage
{
    internal readonly struct CodeLenInfo(TextRange range, string title)
    {
        public readonly TextRange range = range;
        public readonly string title = title;
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
                    if (FileSpaceOperator(space, textPosition, null, fileDeclaratioin =>
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

        private static void CollectSemanticToken(Manager manager, SemanticTokenCollector collector, FileSpace space)
        {
            foreach (var child in space.children)
                CollectSemanticToken(manager, collector, child);
            if (space.name != null)
                collector.Add(DetailTokenType.Namespace, space.name.Value);
            foreach (var import in space.imports)
                for (var i = 0; i < import.names.Count; i++)
                {
                    if (i > 0) collector.Add(DetailTokenType.Operator, import.names[i - 1].end & import.names[i].start);
                    collector.Add(DetailTokenType.Namespace, import.names[i]);
                }
            foreach (var declaration in space.variables) declaration.abstractDeclaration?.CollectSemanticToken(manager, collector);
            foreach (var declaration in space.functions) declaration.abstractDeclaration?.CollectSemanticToken(manager, collector);
            foreach (var declaration in space.enums) declaration.abstractDeclaration?.CollectSemanticToken(manager, collector);
            foreach (var declaration in space.structs) declaration.abstractDeclaration?.CollectSemanticToken(manager, collector);
            foreach (var declaration in space.interfaces) declaration.abstractDeclaration?.CollectSemanticToken(manager, collector);
            foreach (var declaration in space.classes) declaration.abstractDeclaration?.CollectSemanticToken(manager, collector);
            foreach (var declaration in space.delegates) declaration.abstractDeclaration?.CollectSemanticToken(manager, collector);
            foreach (var declaration in space.tasks) declaration.abstractDeclaration?.CollectSemanticToken(manager, collector);
            foreach (var declaration in space.natives) declaration.abstractDeclaration?.CollectSemanticToken(manager, collector);
        }
        public static SemanticTokenCollector CollectSemanticToken(Manager manager, DocumentUri uri)
        {
            var collector = new SemanticTokenCollector();
            lock (manager)
                if (manager.allFileSpaces.TryGetValue(new UnifiedPath(uri), out var space))
                    CollectSemanticToken(manager, collector, space);
            return collector;
        }

        private static void CollectCodeLens(Manager manager, FileSpace space, List<CodeLenInfo> infos)
        {
            foreach (var child in space.children)
                CollectCodeLens(manager, child, infos);
            foreach (var file in space.variables)
                if (file.abstractDeclaration is AbstractVariable abstractVariable)
                {
                    infos.Add(new CodeLenInfo(abstractVariable.name, $"读取：{abstractVariable.references.Count}"));
                    infos.Add(new CodeLenInfo(abstractVariable.name, $"写入：{abstractVariable.write.Count}"));
                }
            foreach (var file in space.functions)
                if (file.abstractDeclaration is AbstractFunction abstractFunction)
                    infos.Add(new CodeLenInfo(abstractFunction.name, $"引用：{abstractFunction.references.Count}"));
            foreach (var file in space.enums)
                if (file.abstractDeclaration is AbstractEnum abstractEnum)
                    infos.Add(new CodeLenInfo(abstractEnum.name, $"引用：{abstractEnum.references.Count}"));
            foreach (var file in space.structs)
                if (file.abstractDeclaration is AbstractStruct abstractStruct)
                {
                    infos.Add(new CodeLenInfo(abstractStruct.name, $"引用：{abstractStruct.references.Count}"));
                    foreach (var member in abstractStruct.variables)
                    {
                        infos.Add(new CodeLenInfo(member.name, $"读取：{member.references.Count}"));
                        infos.Add(new CodeLenInfo(member.name, $"写入：{member.write.Count}"));
                    }
                    foreach (var member in abstractStruct.functions)
                        infos.Add(new CodeLenInfo(member.name, $"引用：{member.references.Count}"));
                }
            foreach (var file in space.interfaces)
                if (file.abstractDeclaration is AbstractInterface abstractInterface)
                {
                    infos.Add(new CodeLenInfo(abstractInterface.name, $"引用：{abstractInterface.references.Count}"));
                    infos.Add(new CodeLenInfo(abstractInterface.name, $"实现：{abstractInterface.implements.Count}"));
                    foreach (var member in abstractInterface.functions)
                    {
                        infos.Add(new CodeLenInfo(member.name, $"引用：{member.references.Count}"));
                        infos.Add(new CodeLenInfo(member.name, $"实现：{member.implements.Count}"));
                    }
                }
            foreach (var file in space.classes)
                if (file.abstractDeclaration is AbstractClass abstractClass)
                {
                    infos.Add(new CodeLenInfo(abstractClass.name, $"引用：{abstractClass.references.Count}"));
                    infos.Add(new CodeLenInfo(abstractClass.name, $"子类：{abstractClass.implements.Count}"));
                    foreach (var member in abstractClass.variables)
                    {
                        infos.Add(new CodeLenInfo(member.name, $"读取：{member.references.Count}"));
                        infos.Add(new CodeLenInfo(member.name, $"写入：{member.write.Count}"));
                    }
                    foreach (var member in abstractClass.functions)
                    {
                        infos.Add(new CodeLenInfo(member.name, $"引用：{member.references.Count}"));
                        infos.Add(new CodeLenInfo(member.name, $"实现：{member.implements.Count}"));
                        infos.Add(new CodeLenInfo(member.name, $"覆盖：{member.overrides.Count}"));
                    }
                    foreach (var member in abstractClass.constructors)
                        infos.Add(new CodeLenInfo(member.name, $"引用：{member.references.Count}"));
                }
            foreach (var file in space.delegates)
                if (file.abstractDeclaration is AbstractDelegate abstractDelegate)
                    infos.Add(new CodeLenInfo(abstractDelegate.name, $"引用：{abstractDelegate.references.Count}"));
            foreach (var file in space.tasks)
                if (file.abstractDeclaration is AbstractTask abstractTask)
                    infos.Add(new CodeLenInfo(abstractTask.name, $"引用：{abstractTask.references.Count}"));
            foreach (var file in space.natives)
                if (file.abstractDeclaration is AbstractNative abstractNative)
                    infos.Add(new CodeLenInfo(abstractNative.name, $"引用：{abstractNative.references.Count}"));
        }
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

        public static AbstractSpace? GetSpace(Manager manager, TextPosition position)
        {
            if (manager.allFileSpaces.TryGetValue(position.document.path, out var result))
                return GetSpace(result, position).space;
            return null;
        }
        private static FileSpace GetSpace(FileSpace space, TextPosition position)
        {
            foreach (var child in space.children)
                if (child.range.Contain(position))
                    return GetSpace(child, position);
            return space;
        }
        public static bool TryGetContext(Manager manager, TextPosition position, out Context context)
        {
            if (manager.allFileSpaces.TryGetValue(position.document.path, out var result))
            {
                var space = GetSpace(result, position);
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
