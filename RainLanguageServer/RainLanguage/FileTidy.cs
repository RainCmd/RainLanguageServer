namespace RainLanguageServer.RainLanguage
{
    internal static class FileTidy
    {
        private static void AddRely(FileSpace space, ImportSpaceInfo import, AbstractSpace? rely)
        {
            import.space = rely;
            for (var i = 1; i < import.names.Count; i++)
                if (rely!.children.TryGetValue(import.names[i].ToString(), out rely)) rely.references.Add(import.names[i]);
                else
                {
                    space.collector.Add(import.names[i], ErrorLevel.Error, "导入的命名空间未找到");
                    return;
                }
            if (!space.relies.Add(rely!)) space.collector.Add(import.range, ErrorLevel.Info, "重复导入的命名空间");
        }
        private static void InitRelies(Manager manager, AbstractLibrary library, FileSpace space)
        {
            foreach (var import in space.imports)
            {
                var spaceName = import.names[0].ToString();
                for (var index = space.space; index != null; index = index.parent)
                {
                    if (index.children.TryGetValue(spaceName, out var rely))
                    {
                        AddRely(space, import, rely);
                        goto label_next;
                    }
                }
                if (import.names[0] == library.name) space.collector.Add(import.range, ErrorLevel.Error, "不能导入自己");
                else if (manager.TryLoadLibrary(import.names[0].ToString(), out var rely)) AddRely(space, import, rely);
                else space.collector.Add(import.names[0], ErrorLevel.Error, "导入的命名空间未找到");
                label_next:;
            }
        }
        private static void AddDeclaration(FileDeclaration file, bool allowKeyword, Declaration declaration, FileSpace space)
        {
            if (!Utility.IsValidName(file.name, allowKeyword, false, space.collector))
                space.collector.Add(file.name, ErrorLevel.Error, "无效的名称标识符");
            space.space.declarations.Add(file.name.ToString(), declaration);
        }
        public static void Tidy(Manager manager, AbstractLibrary library, FileSpace space)
        {
            InitRelies(manager, library, space);
            foreach (var child in space.children)
            {
                child.relies.AddRange(space.relies);
                Tidy(manager, library, child);
            }
            var allowKeyword = library.library == Manager.LIBRARY_KERNEL;
            foreach (var file in space.enums)
            {
                var declaration = new Declaration(library.library, file.visibility, DeclarationCategory.Enum, library.enums.Count);
                var abstractEnum = new AbstractEnum(file, space.space, file.name, declaration);
                AddDeclaration(file, allowKeyword, declaration, space);
                library.enums.Add(abstractEnum);
            }
            foreach (var file in space.structs)
            {
                var declaration = new Declaration(library.library, file.visibility, DeclarationCategory.Struct, library.structs.Count);
                var abstractStruct = new AbstractStruct(file, space.space, file.name, declaration);
                AddDeclaration(file, allowKeyword, declaration, space);
                library.structs.Add(abstractStruct);
            }
            foreach (var file in space.interfaces)
            {
                var declaration = new Declaration(library.library, file.visibility, DeclarationCategory.Interface, library.interfaces.Count);
                var abstractInterface = new AbstractInterface(file, space.space, file.name, declaration);
                AddDeclaration(file, allowKeyword, declaration, space);
                library.interfaces.Add(abstractInterface);
            }
            foreach (var file in space.classes)
            {
                var declaration = new Declaration(library.library, file.visibility, DeclarationCategory.Class, library.classes.Count);
                var abstractClass = new AbstractClass(file, space.space, file.name, declaration);
                AddDeclaration(file, allowKeyword, declaration, space);
                library.classes.Add(abstractClass);
            }
            foreach (var file in space.delegates)
            {
                var declaration = new Declaration(library.library, file.visibility, DeclarationCategory.Delegate, library.delegates.Count);
                var abstractDelegate = new AbstractDelegate(file, space.space, file.name, declaration, [], Tuple.Empty);
                AddDeclaration(file, allowKeyword, declaration, space);
                library.delegates.Add(abstractDelegate);
            }
            foreach (var file in space.tasks)
            {
                var declaration = new Declaration(library.library, file.visibility, DeclarationCategory.Task, library.tasks.Count);
                var abstractTask = new AbstractTask(file, space.space, file.name, declaration, Tuple.Empty);
                AddDeclaration(file, allowKeyword, declaration, space);
                library.tasks.Add(abstractTask);
            }
        }
    }
}
