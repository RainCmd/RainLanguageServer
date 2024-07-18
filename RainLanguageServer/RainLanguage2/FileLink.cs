namespace RainLanguageServer.RainLanguage2
{
    internal static class FileLink
    {
        private static Type GetType(Context context, Manager manager, FileType type, MessageCollector collector)
        {
            Type resultType = default;
            var result = context.FindDeclaration(manager, type.name, collector);
            if (result.Count > 0)
            {
                if (result.Count == 1)
                {
                    resultType = result[0].declaration.DefineType;
                    if (resultType.code == TypeCode.Invalid) collector.Add(type.name.Range, ErrorLevel.Error, "无效的类型");
                    else resultType = resultType.ToDimension(type.dimension);
                    type.SetDeclaration(result[0]);
                }
                else collector.Add(type.name.name, ErrorLevel.Error, "类型不明确");
            }
            return resultType;
        }
        private static void AddDeclaration(FileDeclaration file, bool allowKeyword, bool operatorReloadable, Declaration declaration, FileSpace space)
        {
            if (Utility.IsValidName(file.name, allowKeyword, operatorReloadable, space.collector))
                space.space.declarations.Add(file.name.ToString(), declaration);
            else space.collector.Add(file.name, ErrorLevel.Error, "无效的名称标识符");
        }
        private static T Find<T>(List<T> list, FileSpace space, TextRange name) where T : AbstractDeclaration
        {
            var declarations = space.space.declarations[name.ToString()];
            foreach (var declaration in declarations)
                if (list[declaration.index].name == name)
                    return list[declaration.index];
            throw new Exception();
        }
        private static bool IsValidMemberName(TextRange name, MessageCollector collector)
        {
            if (KeyWords.IsKeyWorld(name.ToString()))
            {
                collector.Add(name, ErrorLevel.Error, "关键字不能作为成员名称使用");
                return false;
            }
            Lexical.TryAnalysis(name, 0, out var lexical, null);
            if (lexical.type != LexicalType.Word || name == KeyWords.DISCARD_VARIABLE)
            {
                collector.Add(name, ErrorLevel.Error, "成员名称不是合法的标识符");
                return false;
            }
            return true;
        }
        public static void Link(Manager manager, AbstractLibrary library, FileSpace space)
        {
            foreach (var child in space.children) Link(manager, library, child);
            var allowKeyword = library.library == Manager.LIBRARY_KERNEL;
            var context = new Context(space.document, space.space, space.relies, default);
            foreach (var file in space.variables)
            {
                var type = GetType(context, manager, file.type, space.collector);
                var declaration = Utility.GetDeclaration(manager, library, file.visibility, DeclarationCategory.Variable);
                var variable = new AbstractVariable(file, space.space, file.name, declaration, file.isReadonly, type);
                AddDeclaration(file, allowKeyword, false, declaration, space);
                library.variables.Add(declaration.index, variable);
            }
            foreach (var file in space.functions)
            {
                var parameters = new List<AbstractCallable.Parameter>();
                foreach (var parameter in file.parameters)
                    parameters.Add(new AbstractCallable.Parameter(GetType(context, manager, parameter.type, space.collector), parameter.name));
                var returns = new Type[file.returns.Count];
                for (var i = 0; i < returns.Length; i++)
                    returns[i] = GetType(context, manager, file.returns[i], space.collector);
                var declaration = Utility.GetDeclaration(manager, library, file.visibility, DeclarationCategory.Function);
                var function = new AbstractFunction(file, space.space, file.name, declaration, parameters, returns);
                AddDeclaration(file, allowKeyword, true, declaration, space);
                library.functions.Add(declaration.index, function);
            }
            foreach (var file in space.enums)
            {
                var abstractEnum = Find(library.enums, space, file.name);

                foreach (var element in file.elements)
                {
                    var declaration = new Declaration(library.library, Visibility.Public, DeclarationCategory.EnumElement, abstractEnum.elements.Count, abstractEnum.declaration.index);
                    var enumElement = new AbstractEnum.Element(element, space.space, element.name, declaration, IsValidMemberName(element.name, space.collector));
                    abstractEnum.elements.Add(declaration.index, enumElement);
                }
            }
            foreach (var file in space.structs)
            {
                var abstractStruct = Find(library.structs, space, file.name);

                foreach (var variable in file.variables)
                {
                    var type = GetType(context, manager, variable.type, space.collector);
                    var declaration = new Declaration(library.library, Visibility.Public, DeclarationCategory.StructVariable, abstractStruct.variables.Count, abstractStruct.declaration.index);
                    var structVariable = new AbstractStruct.Variable(variable, space.space, variable.name, declaration, type, IsValidMemberName(variable.name, space.collector));
                    abstractStruct.variables.Add(declaration.index, structVariable);
                }
                foreach (var function in file.functions)
                {
                    var parameters = new List<AbstractCallable.Parameter>();
                    foreach (var parameter in function.parameters)
                        parameters.Add(new AbstractCallable.Parameter(GetType(context, manager, parameter.type, space.collector), parameter.name));
                    var returns = new Type[function.returns.Count];
                    for (var i = 0; i < returns.Length; i++)
                        returns[i] = GetType(context, manager, function.returns[i], space.collector);
                    var declaration = new Declaration(library.library, function.visibility, DeclarationCategory.StructFunction, abstractStruct.functions.Count, abstractStruct.declaration.index);
                    var structFunction = new AbstractStruct.Function(function, space.space, function.name, declaration, parameters, returns, IsValidMemberName(function.name, space.collector));
                    abstractStruct.functions.Add(declaration.index, structFunction);
                }
            }
            foreach (var file in space.interfaces)
            {
                var abstractInterface = Find(library.interfaces, space, file.name);
                foreach (var inherit in file.inherits)
                {
                    var type = GetType(context, manager, inherit, space.collector);
                    if (type.dimension > 0) space.collector.Add(inherit.range, ErrorLevel.Error, "不能继承数组");
                    else if (type.code != TypeCode.Interface) space.collector.Add(inherit.range, ErrorLevel.Error, "只能继承接口");
                    else abstractInterface.inherits.Add(type);
                    if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstractInterface parent)
                        parent.implements.Add(abstractInterface);
                }
                foreach (var function in file.functions)
                {
                    var parameters = new List<AbstractCallable.Parameter>();
                    foreach (var parameter in function.parameters)
                        parameters.Add(new AbstractCallable.Parameter(GetType(context, manager, parameter.type, space.collector), parameter.name));
                    var returns = new Type[function.returns.Count];
                    for (var i = 0; i < returns.Length; i++)
                        returns[i] = GetType(context, manager, function.returns[i], space.collector);
                    var declaration = new Declaration(library.library, function.visibility, DeclarationCategory.InterfaceFunction, abstractInterface.functions.Count, abstractInterface.declaration.index);
                    var interfaceFunction = new AbstractInterface.Function(function, space.space, function.name, declaration, parameters, returns, IsValidMemberName(function.name, space.collector));
                    abstractInterface.functions.Add(declaration.index, interfaceFunction);
                }
            }
            foreach (var file in space.classes)
            {
                var abstractClass = Find(library.classes, space, file.name);

                for (var i = 0; i < file.inherits.Count; i++)
                {
                    var type = GetType(context, manager, file.inherits[i], space.collector);
                    if (type.dimension > 0) space.collector.Add(file.inherits[i].range, ErrorLevel.Error, "不能继承数组");
                    else if (i > 0 || type.code == TypeCode.Interface)
                    {
                        if (type.code == TypeCode.Interface) abstractClass.inherits.Add(type);
                        else space.collector.Add(file.inherits[i].range, ErrorLevel.Error, "必须是接口类型");
                    }
                    else if (type.code == TypeCode.Handle) abstractClass.parent = type;
                    else space.collector.Add(file.inherits[i].range, ErrorLevel.Error, "不能继承该类型");
                    if (manager.TryGetDeclaration(type, out var declaration))
                    {
                        if (declaration is AbstractClass parent) parent.implements.Add(abstractClass);
                        else if (declaration is AbstractInterface inherit) inherit.implements.Add(abstractClass);
                    }
                }

                foreach (var variable in file.variables)
                {
                    var type = GetType(context, manager, variable.type, space.collector);
                    var declaration = new Declaration(library.library, variable.visibility, DeclarationCategory.ClassVariable, abstractClass.variables.Count, abstractClass.declaration.index);
                    var classVariable = new AbstractClass.Variable(variable, space.space, variable.name, declaration, type, IsValidMemberName(variable.name, space.collector));
                    abstractClass.variables.Add(declaration.index, classVariable);
                }
                foreach (var constructor in file.constructors)
                {
                    var parameters = new List<AbstractCallable.Parameter>();
                    foreach (var parameter in constructor.parameters)
                        parameters.Add(new AbstractCallable.Parameter(GetType(context, manager, parameter.type, space.collector), parameter.name));
                    var declaration = new Declaration(library.library, constructor.visibility, DeclarationCategory.Constructor, abstractClass.constructors.Count, abstractClass.declaration.index);
                    var classConstructor = new AbstractClass.Constructor(constructor, space.space, constructor.name, declaration, parameters, new Tuple());
                    abstractClass.constructors.Add(declaration.index, classConstructor);
                }
                foreach (var function in file.functions)
                {
                    var parameters = new List<AbstractCallable.Parameter>();
                    foreach (var parameter in function.parameters)
                        parameters.Add(new AbstractCallable.Parameter(GetType(context, manager, parameter.type, space.collector), parameter.name));
                    var returns = new Type[function.returns.Count];
                    for (var i = 0; i < returns.Length; i++)
                        returns[i] = GetType(context, manager, function.returns[i], space.collector);
                    var valid = true;
                    if (function.name.ToString() == abstractClass.name)
                    {
                        valid = false;
                        space.collector.Add(function.name, ErrorLevel.Error, "成员函数名不能与类型名相同");
                    }
                    var declaration = new Declaration(library.library, function.visibility, DeclarationCategory.ClassFunction, abstractClass.functions.Count);
                    var classFunction = new AbstractClass.Function(function, space.space, function.name, declaration, parameters, returns, valid && IsValidMemberName(function.range, space.collector));
                    abstractClass.functions.Add(declaration.index, classFunction);
                }
            }
            foreach (var file in space.delegates)
            {
                var abstructDelegate = Find(library.delegates, space, file.name);
                var references = abstructDelegate.references;
                var parameters = new List<AbstractCallable.Parameter>();
                foreach (var parameter in file.parameters)
                    parameters.Add(new AbstractCallable.Parameter(GetType(context, manager, parameter.type, space.collector), parameter.name));
                var returns = new Type[file.returns.Count];
                for (var i = 0; i < returns.Length; i++)
                    returns[i] = GetType(context, manager, file.returns[i], space.collector);
                abstructDelegate = new AbstructDelegate(file, space.space, file.name, abstructDelegate.declaration, parameters, returns);
                abstructDelegate.references.AddRange(references);
                library.delegates[abstructDelegate.declaration.index] = abstructDelegate;
            }
            foreach (var file in space.tasks)
            {
                var abstructTask = Find(library.tasks, space, file.name);
                var references = abstructTask.references;
                var returns = new Type[file.returns.Count];
                for (var i = 0; i < returns.Length; i++)
                    returns[i] = GetType(context, manager, file.returns[i], space.collector);
                abstructTask = new AbstructTask(file, space.space, file.name, abstructTask.declaration, returns);
                abstructTask.references.AddRange(references);
                library.tasks[abstructTask.declaration.index] = abstructTask;
            }
            foreach (var file in space.natives)
            {
                var parameters = new List<AbstractCallable.Parameter>();
                foreach (var parameter in file.parameters)
                    parameters.Add(new AbstractCallable.Parameter(GetType(context, manager, parameter.type, space.collector), parameter.name));
                var returns = new Type[file.returns.Count];
                for (var i = 0; i < returns.Length; i++)
                    returns[i] = GetType(context, manager, file.returns[i], space.collector);
                var declaration = Utility.GetDeclaration(manager, library, file.visibility, DeclarationCategory.Native);
                var native = new AbstructNative(file, space.space, file.name, declaration, parameters, returns);
                AddDeclaration(file, allowKeyword, false, declaration, space);
                library.natives.Add(declaration.index, native);
            }
        }
    }
}
