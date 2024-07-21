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
                                                msg.AddRelated(item.name, "参数类型列表相同的函数");
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
                                    msg.AddRelated(item.name, "名称重复的定义");
                            declaration.file.space.collector.Add(msg);
                        }
                    }
                    abstractDeclarations.Clear();
                }
        }
        private static bool FindStruct(Manager manager, HashSet<Type> set, Type define, Type index)
        {
            if (index.dimension > 0 || index.code != TypeCode.Struct) return false;
            else if (index.library != Manager.LIBRARY_SELF) return false;
            else if (define == index) return true;
            if (manager.TryGetDeclaration(index, out var declaration) && declaration is AbstractStruct abstractStruct && set.Add(abstractStruct.declaration.DefineType))
                foreach (var varibale in abstractStruct.variables)
                    if (FindStruct(manager, set, define, varibale.type))
                        return true;
            return false;
        }
        private static bool FindInterface(Manager manager, HashSet<Type> set, Type define, Type index)
        {
            if (index.dimension > 0 || index.code != TypeCode.Interface) return false;
            else if (index.library != Manager.LIBRARY_SELF) return false;
            else if (define == index) return true;
            if (manager.TryGetDeclaration(index, out var declaration) && declaration is AbstractInterface abstractInterface && set.Add(abstractInterface.declaration.DefineType))
                foreach (var inhert in abstractInterface.inherits)
                    if (FindInterface(manager, set, define, inhert))
                        return true;
            return false;
        }
        private static bool FindClass(Manager manager, HashSet<Type> set, Type define, Type index)
        {
            if (index.dimension > 0 || index.code != TypeCode.Handle) return false;
            else if (index.library != Manager.LIBRARY_SELF) return false;
            else if (define == index) return true;
            if (manager.TryGetDeclaration(index, out var declaration) && declaration is AbstractClass abstractClass && set.Add(abstractClass.declaration.DefineType))
                if (FindClass(manager, set, define, abstractClass.parent))
                    return true;
            return false;
        }
        public static void CheckValidity(Manager manager, AbstractLibrary library)
        {
            CheckDuplicationName(manager, library);
            var filter = new HashSet<Declaration>();
            var duplications = new List<AbstractDeclaration>();
            foreach (var enumeration in library.enums)
            {
                for (var x = 0; x < enumeration.elements.Count; x++)
                {
                    var elementX = enumeration.elements[x];
                    if (filter.Add(elementX.declaration))
                    {
                        var name = elementX.name.ToString();
                        for (var y = x + 1; y < enumeration.elements.Count; y++)
                        {
                            var elementY = enumeration.elements[y];
                            if (name == elementY.name)
                            {
                                duplications.Add(elementY);
                                filter.Add(elementY.declaration);
                            }
                        }
                        if (duplications.Count > 0)
                        {
                            duplications.Add(elementX);
                            foreach (var element in duplications)
                            {
                                var msg = new Message(element.name, ErrorLevel.Error, "枚举名称重复");
                                foreach (var item in duplications)
                                    if (element != item)
                                        msg.AddRelated(item.name, "重复的枚举名");
                                enumeration.file.space.collector.Add(msg);
                            }
                            duplications.Clear();
                        }
                    }
                }
                filter.Clear();
            }
            var typeSet = new HashSet<Type>();
            foreach (var abstractStruct in library.structs)
            {
                var type = abstractStruct.declaration.DefineType;
                for (var x = 0; x < abstractStruct.variables.Count; x++)
                {
                    var variableX = abstractStruct.variables[x];
                    if (FindStruct(manager, typeSet, type, variableX.type))
                        abstractStruct.file.space.collector.Add(new Message(variableX.name, ErrorLevel.Error, "结构体字段类型循环包含"));
                    typeSet.Clear();
                    if (filter.Add(variableX.declaration))
                    {
                        var name = variableX.name.ToString();
                        for (var y = x + 1; y < abstractStruct.variables.Count; y++)
                        {
                            var variableY = abstractStruct.variables[y];
                            if (name == variableY.name)
                            {
                                duplications.Add(variableY);
                                filter.Add(variableY.declaration);
                            }
                        }
                        foreach (var function in abstractStruct.functions)
                            if (name == function.name)
                                duplications.Add(function);
                        if (duplications.Count > 0)
                        {
                            duplications.Add(variableX);
                            foreach (var index in duplications)
                                if (index is AbstractStruct.Variable)
                                {
                                    var msg = new Message(index.name, ErrorLevel.Error, "成员名称重复");
                                    foreach (var item in duplications)
                                        if (item != index)
                                            msg.AddRelated(item.name, "成员名称重复");
                                    abstractStruct.file.space.collector.Add(msg);
                                }
                            duplications.Clear();
                        }
                    }
                }
                filter.Clear();
                for (var x = 0; x < abstractStruct.functions.Count; x++)
                {
                    var functionX = abstractStruct.functions[x];
                    var name = functionX.name.ToString();
                    if (name == abstractStruct.name)
                        abstractStruct.file.space.collector.Add(functionX.name, ErrorLevel.Error, "结构体不允许有构造函数");
                    else if (filter.Add(functionX.declaration))
                    {
                        for (var y = x + 1; y < abstractStruct.functions.Count; y++)
                        {
                            var functionY = abstractStruct.functions[y];
                            if (name == functionY.name && functionX.signature == functionY.signature)
                            {
                                duplications.Add(functionY);
                                filter.Add(functionY.declaration);
                            }
                        }
                        if (duplications.Count > 0)
                        {
                            duplications.Add(functionX);
                            foreach (var index in duplications)
                            {
                                var msg = new Message(index.name, ErrorLevel.Error, "无效的函数重载");
                                foreach (var item in duplications)
                                    if (item != index)
                                        msg.AddRelated(item.name, "函数名和参数列表相同的函数");
                                abstractStruct.file.space.collector.Add(msg);
                            }
                            duplications.Clear();
                        }
                    }
                }
                filter.Clear();
            }
            foreach (var abstractInterface in library.interfaces)
            {
                for (var i = 0; i < abstractInterface.inherits.Count; i++)
                    if (FindInterface(manager, typeSet, abstractInterface.declaration.DefineType, abstractInterface.inherits[i]))
                    {
                        var file = (FileInterface)abstractInterface.file;
                        file.space.collector.Add(file.inherits[i].range, ErrorLevel.Error, "存在循环继承");
                    }
                typeSet.Clear();
                for (var x = 0; x < abstractInterface.functions.Count; x++)
                {
                    var functionX = abstractInterface.functions[x];
                    if (filter.Add(functionX.declaration))
                    {
                        var name = functionX.name.ToString();
                        for (var y = x + 1; y < abstractInterface.functions.Count; y++)
                        {
                            var functionY = abstractInterface.functions[y];
                            if (name == functionY.name && functionY.signature == functionX.signature)
                            {
                                duplications.Add(functionY);
                                filter.Add(functionY.declaration);
                            }
                        }
                        if (duplications.Count > 0)
                        {
                            duplications.Add(functionX);
                            foreach (var index in duplications)
                            {
                                var msg = new Message(index.name, ErrorLevel.Error, "无效的函数重载");
                                foreach (var item in duplications)
                                    if (item != index)
                                        msg.AddRelated(item.name, "函数名和参数列表相同的函数");
                                abstractInterface.file.space.collector.Add(msg);
                            }
                            duplications.Clear();
                        }
                    }
                }
                filter.Clear();
            }
            foreach (var abstractClass in library.classes)
            {
                var file = (FileClass)abstractClass.file;
                if (FindClass(manager, typeSet, abstractClass.declaration.DefineType, abstractClass.parent))
                    file.space.collector.Add(file.inherits[0].range, ErrorLevel.Error, "存在循环继承");
                typeSet.Clear();
                for (var x = 0; x < abstractClass.variables.Count; x++)
                {
                    var variableX = abstractClass.variables[x];
                    if (filter.Add(variableX.declaration))
                    {
                        var name = variableX.name.ToString();
                        for (var y = x + 1; y < abstractClass.variables.Count; y++)
                        {
                            var variableY = abstractClass.variables[y];
                            if (name == variableY.name)
                            {
                                duplications.Add(variableY);
                                filter.Add(variableY.declaration);
                            }
                        }
                        foreach (var function in abstractClass.functions)
                            if (function.name == name)
                                duplications.Add(function);
                        if (duplications.Count > 0)
                        {
                            duplications.Add(variableX);
                            foreach (var index in duplications)
                                if (index is AbstractClass.Variable)
                                {
                                    var msg = new Message(index.name, ErrorLevel.Error, "成员名称重复");
                                    foreach (var item in duplications)
                                        if (item != index)
                                            msg.AddRelated(item.name, "名称重复的成员");
                                    file.space.collector.Add(msg);
                                }
                            duplications.Clear();
                        }
                    }
                }
                filter.Clear();
                for (var x = 0; x < abstractClass.constructors.Count; x++)
                {
                    var constructorX = abstractClass.constructors[x];
                    if (filter.Add(constructorX.declaration))
                    {
                        for (var y = x + 1; y < abstractClass.constructors.Count; y++)
                        {
                            var constructorY = abstractClass.constructors[y];
                            if (constructorX.signature == constructorY.signature)
                            {
                                duplications.Add(constructorY);
                                filter.Add(constructorY.declaration);
                            }
                        }
                        if (duplications.Count > 0)
                        {
                            duplications.Add(constructorX);
                            foreach (var index in duplications)
                            {
                                var msg = new Message(index.name, ErrorLevel.Error, "无效的重载");
                                foreach (var item in duplications)
                                    if (item != index)
                                        msg.AddRelated(item.name, "参数列表相同的构造函数");
                                file.space.collector.Add(msg);
                            }
                            duplications.Clear();
                        }
                    }
                }
                filter.Clear();
                for (var x = 0; x < abstractClass.functions.Count; x++)
                {
                    var functionX = abstractClass.functions[x];
                    var name = functionX.name.ToString();
                    if (name == abstractClass.name) file.space.collector.Add(functionX.name, ErrorLevel.Error, "成员函数名不能与类型名相同");
                    else if (filter.Add(functionX.declaration))
                    {
                        for (var y = x + 1; y < abstractClass.functions.Count; y++)
                        {
                            var functionY = abstractClass.functions[y];
                            if (name == functionY.name && functionX.signature == functionY.signature)
                            {
                                duplications.Add(functionY);
                                filter.Add(functionY.declaration);
                            }
                        }
                        if (duplications.Count > 0)
                        {
                            duplications.Add(functionX);
                            foreach (var index in duplications)
                            {
                                var msg = new Message(index.name, ErrorLevel.Error, "无效的函数重载");
                                foreach (var item in duplications)
                                    if (index != item)
                                        msg.AddRelated(item.name, "函数名和参数列表都相同的函数");
                                file.space.collector.Add(msg);
                            }
                            duplications.Clear();
                        }
                    }
                }
                filter.Clear();
            }
        }
    }
}
