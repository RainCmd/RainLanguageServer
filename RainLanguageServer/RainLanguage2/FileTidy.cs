namespace RainLanguageServer.RainLanguage2
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
            if (Utility.IsValidName(file.name, allowKeyword, false))
                space.space.declarations.Add(file.name.ToString(), declaration.index, declaration);
            else space.collector.Add(file.name, ErrorLevel.Error, "无效的名称标识符");
        }
        public static void Tidy(Manager manager, AbstractLibrary library, FileSpace space)
        {
            InitRelies(manager, library, space);
            foreach (var child in space.children)
            {
                foreach (var rely in space.relies) child.relies.Add(rely);
                Tidy(manager, library, child);
            }
            var allowKeyword = library == manager.kernel;
            foreach (var file in space.enums)
            {
                var declaration = Utility.GetDeclaration(manager, library, file.visibility, DeclarationCategory.Enum);
                var abstractEnum = new AbstractEnum(file, space.space, file.name, declaration);
                AddDeclaration(file, allowKeyword, declaration, space);
                library.enums.Add(abstractEnum);
                file.abstractDeclaration = abstractEnum;
            }
            foreach (var file in space.structs)
            {
                var declaration = Utility.GetDeclaration(manager, library, file.visibility, DeclarationCategory.Struct);
                var abstractStruct = new AbstractStruct(file, space.space, file.name, declaration);
                AddDeclaration(file, allowKeyword, declaration, space);
                library.structs.Add(abstractStruct);
                file.abstractDeclaration = abstractStruct;
            }
            foreach (var file in space.interfaces)
            {
                var declaration = Utility.GetDeclaration(manager, library, file.visibility, DeclarationCategory.Interface);
                var abstractInterface = new AbstractInterface(file, space.space, file.name, declaration);
                AddDeclaration(file, allowKeyword, declaration, space);
                library.interfaces.Add(abstractInterface);
                file.abstractDeclaration = abstractInterface;
            }
            foreach (var file in space.classes)
            {
                var declaration = Utility.GetDeclaration(manager, library, file.visibility, DeclarationCategory.Class);
                var abstractClass = new AbstractClass(file, space.space, file.name, declaration);
                AddDeclaration(file, allowKeyword, declaration, space);
                library.classes.Add(abstractClass);
                file.abstractDeclaration = abstractClass;
            }
            foreach (var file in space.delegates)
            {
                var declaration = Utility.GetDeclaration(manager, library, file.visibility, DeclarationCategory.Delegate);
                var abstructDelegate = new AbstructDelegate(file, space.space, file.name, declaration, [], new Tuple());
                AddDeclaration(file, allowKeyword, declaration, space);
                library.delegates.Add(abstructDelegate);
                file.abstractDeclaration = abstructDelegate;
            }
            foreach (var file in space.tasks)
            {
                var declaration = Utility.GetDeclaration(manager, library, file.visibility, DeclarationCategory.Task);
                var abstructTask = new AbstructTask(file, space.space, file.name, declaration, new Tuple());
                AddDeclaration(file, allowKeyword, declaration, space);
                library.tasks.Add(abstructTask);
                file.abstractDeclaration = abstructTask;
            }
        }
    }
}
