namespace RainLanguageServer.RainLanguage2
{
    internal static class FileLink
    {
        private static Type GetType(Context context, Manager manager, FileType type)
        {
            return default;
        }
        public static void Link(Manager manager, AbstractLibrary library, FileSpace space)
        {
            foreach (var child in space.children) Link(manager, library, child);
            var context = new Context(space.document, space.space, space.relies, default);
            foreach (var file in space.variables)
            {
                var type = GetType(context, manager, file.type);
                var declaration = Utility.GetDeclaration(manager, library, file.visibility, DeclarationCategory.Variable);
                var variable = new AbstractVariable(file, space.space, file.name, declaration, file.isReadonly, type);
                if (file.isReadonly)
                {
                }
            }
        }
    }
}
