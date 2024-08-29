using LanguageServer;
using LanguageServer.Parameters;
using LanguageServer.Parameters.TextDocument;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RainLanguageServer.RainLanguage2
{
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
        public static bool OnHighlight(Manager manager, DocumentUri uri, Position position, List<HighlightInfo> infos)
        {
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
        public static bool FindReferences(Manager manager, DocumentUri uri, Position position, List<TextRange> references)
        {
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
                collector.AddRange(SemanticTokenType.Namespace, space.name.Value);
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
            if (manager.allFileSpaces.TryGetValue(new UnifiedPath(uri), out var space))
                CollectSemanticToken(manager, collector, space);
            return collector;
        }

        public static AbstractSpace? GetSpace(Manager manager, TextPosition position)
        {
            if (manager.allFileSpaces.TryGetValue(position.document.path, out var result))
                return GetSpace(result, position);
            return null;
        }
        private static AbstractSpace GetSpace(FileSpace space, TextPosition position)
        {
            foreach (var child in space.children)
                if (child.range.Contain(position))
                    return GetSpace(child, position);
            return space.space;
        }
    }
}
