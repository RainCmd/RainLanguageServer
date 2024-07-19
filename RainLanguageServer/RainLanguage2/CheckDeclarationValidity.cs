namespace RainLanguageServer.RainLanguage2
{
    internal static class CheckDeclarationValidity
    {
        private static Tuple GetSignature(AbstractDeclaration declaration)
        {
            if (declaration is AbstractCallable callable) return callable.signature;
            return default;
        }
        private static bool IsFunctions(List<Declaration> declarations)
        {
            foreach (var declaration in declarations)
                if (declaration.category != DeclarationCategory.Function || declaration.category != DeclarationCategory.Native)
                    return false;
            return true;
        }
        private static void CheckDuplicationName(Manager manager, AbstractSpace space)
        {
            foreach (var child in space.children)
            {
                CheckDuplicationName(manager, child.Value);
                if (space.declarations.TryGetValue(child.Key, out var declarations))
                    foreach (var declaration in declarations)
                        if (manager.TryGetDeclaration(declaration, out var abstractDeclaration))
                            abstractDeclaration.file.space.collector.Add(abstractDeclaration.name, ErrorLevel.Error, "当前命名空间中有同名的子命名空间");
            }
            var filter = new HashSet<Declaration>();
            var duplications = new List<AbstractDeclaration>();
            var abstractDeclarations = new List<AbstractDeclaration>();
            foreach (var declarations in space.declarations.Values)
                if (declarations.Count > 1)
                {
                    manager.ToDeclarations(declarations, abstractDeclarations);
                    if (IsFunctions(declarations))
                    {
                        for (var x = 0; x < abstractDeclarations.Count; x++)
                        {
                            var declarationX = abstractDeclarations[x];
                            if (filter.Add(declarationX.declaration))
                            {
                                var signature = GetSignature(declarationX);
                                for (var y = x + 1; y < abstractDeclarations.Count; y++)
                                {
                                    var declarationY = abstractDeclarations[y];
                                    if (signature == GetSignature(declarationY))
                                    {
                                        duplications.Add(declarationY);
                                        filter.Add(declarationY.declaration);
                                    }
                                }
                                if (declarations.Count > 0)
                                {
                                    duplications.Add(declarationX);
                                    foreach (var declaration in duplications)
                                    {
                                        var msg = new Message(declaration.name, ErrorLevel.Error, "无效的重载");
                                        foreach (var item in duplications)
                                            if (declaration != item)
                                                msg.related.Add(new RelatedInfo(item.name, "参数类型列表相同的函数"));
                                        declaration.file.space.collector.Add(msg);
                                    }
                                    duplications.Clear();
                                }
                            }
                        }
                        filter.Clear();
                    }
                    else
                    {
                        foreach (var declaration in abstractDeclarations)
                        {
                            var msg = new Message(declaration.name, ErrorLevel.Error, "名称重复");
                            foreach (var item in abstractDeclarations)
                                if (declaration != item)
                                    msg.related.Add(new RelatedInfo(item.name, "名称重复的定义"));
                            declaration.file.space.collector.Add(msg);
                        }
                    }
                    abstractDeclarations.Clear();
                }
        }
        public static void CheckValidity(Manager manager, AbstractLibrary library)
        {
            CheckDuplicationName(manager, library);

        }
    }
}
