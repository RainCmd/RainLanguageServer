using LanguageServer;
using RainLanguageServer.RainLanguage.GrammaticalAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage
{
    internal class Manager
    {
        internal class KernelManager(AbstractLibrary kernel)
        {
            public readonly Type BOOL = GetType(kernel.structs, "bool");
            public readonly Type BYTE = GetType(kernel.structs, "byte");
            public readonly Type CHAR = GetType(kernel.structs, "char");
            public readonly Type INT = GetType(kernel.structs, "integer");
            public readonly Type REAL = GetType(kernel.structs, "real");
            public readonly Type REAL2 = GetType(kernel.structs, "real2");
            public readonly Type REAL3 = GetType(kernel.structs, "real3");
            public readonly Type REAL4 = GetType(kernel.structs, "real4");
            public readonly Type ENUM = GetType(kernel.structs, "enum");
            public readonly Type TYPE = GetType(kernel.structs, "type");
            public readonly Type STRING = GetType(kernel.structs, "string");
            public readonly Type ENTITY = GetType(kernel.structs, "entity");
            public readonly Type HANDLE = GetType(kernel.classes, "handle");
            public readonly Type DELEGATE = GetType(kernel.classes, "Delegate");
            public readonly Type TASK = GetType(kernel.classes, "Task");
            public readonly Type ARRAY = GetType(kernel.classes, "array");

            private static Type GetType<T>(List<T> declarations, string name) where T : AbstractDeclaration
            {
                foreach (var declaration in declarations)
                    if (declaration.name == name)
                        return declaration.declaration.DefineType;
                throw new Exception($"kernel中类型 {name} 查找失败");
            }
        }
        public const string SCHEME = "rain-language";
        public const string KERNEL = "kernel";
        public const int LIBRARY_SELF = -1;
        public const int LIBRARY_KERNEL = -2;
        public readonly KernelManager kernelManager;
        public readonly AbstractLibrary library;
        public readonly AbstractLibrary kernel;
        private readonly TextDocument[] kernelDocuments;
        public readonly Dictionary<string, FileSpace> fileSpaces = [];
        public readonly Dictionary<string, FileSpace> allFileSpaces = [];
        public readonly Dictionary<string, AbstractLibrary> relies = [];
        private readonly Dictionary<int, AbstractLibrary> librarys = [];
        private readonly HashSet<string> imports = [];
        private readonly Dictionary<string, TextDocument[]> importDocuments = [];
        private readonly Func<string, TextDocument[]> relyLoader;
        private readonly Func<IEnumerable<TextDocument>> documentLoader;
        private readonly Func<IEnumerable<TextDocument>> opendDocumentLoader;
        private readonly HashSet<AbstractDeclaration> set = [];
        private readonly Queue<AbstractInterface> interfaceQueue = [];
        public Manager(string name, string kernelPath, string[]? imports, Func<string, TextDocument[]> relyLoader, Func<IEnumerable<TextDocument>> documentLoader, Func<IEnumerable<TextDocument>> opendDocumentLoader)
        {
            library = new AbstractLibrary(LIBRARY_SELF, name);
            if (imports != null) this.imports.AddRange(imports);
            this.relyLoader = relyLoader;
            kernelPath = new UnifiedPath(kernelPath);
            using var reader = File.OpenText(kernelPath);
            kernel = new AbstractLibrary(LIBRARY_KERNEL, KERNEL);
            kernelDocuments = [new TextDocument(ToRainScheme(KERNEL), reader.ReadToEnd())];
            Reparse(true);
            kernelManager = new KernelManager(kernel);
            this.documentLoader = documentLoader;
            this.opendDocumentLoader = opendDocumentLoader;
        }
        public bool TryLoadLibrary(string name, [MaybeNullWhen(false)] out AbstractLibrary library)
        {
            if (name == kernel.name)
            {
                library = kernel;
                return true;
            }
            else if (name == this.library.name)
            {
                library = this.library;
                return true;
            }
            else if (relies.TryGetValue(name, out library)) return true;
            else if (imports.Contains(name))
            {
                if (!importDocuments.TryGetValue(name, out var documents)) importDocuments.Add(name, documents = relyLoader(name));
                library = new AbstractLibrary(relies.Count, name);
                relies.Add(name, library);
                ParseLibrary(library, documents);
                return true;
            }
            library = null;
            return false;
        }
        private void ParseLibrary(AbstractLibrary library, TextDocument[] documents)
        {
            librarys.Add(library.library, library);
            var files = new List<FileSpace>(documents.Length);
            foreach (var document in documents)
            {
                var file = FileParse.ParseSpace(library, document);
                files.Add(file);
                allFileSpaces.Add(document.path, file);
            }
            foreach (var file in files)
                FileTidy.Tidy(this, library, file);
            foreach (var file in files)
                FileLink.Link(this, library, file);
            foreach (var file in files)
                file.collector.Clear();
        }
        public AbstractLibrary GetLibrary(int library) => librarys[library];
        public IEnumerable<AbstractClass> GetInheritIterator(AbstractClass? abstractClass)
        {
            set.Clear();
            while (abstractClass != null)
            {
                if (!set.Add(abstractClass)) break;
                yield return abstractClass;
                if (TryGetDeclaration(abstractClass.parent, out var declaration))
                    abstractClass = declaration as AbstractClass;
                else break;
            }
        }
        public IEnumerable<AbstractInterface> GetInheritIterator(AbstractInterface? abstractInterface)
        {
            if (abstractInterface != null)
            {
                set.Clear();
                interfaceQueue.Clear();
                interfaceQueue.Enqueue(abstractInterface);
                while (interfaceQueue.Count > 0)
                {
                    var index = interfaceQueue.Dequeue();
                    if (set.Add(index))
                    {
                        yield return index;
                        foreach (var inheritType in index.inherits)
                            if (TryGetDeclaration(inheritType, out var declaration) && declaration is AbstractInterface inherit)
                                interfaceQueue.Enqueue(inherit);
                    }
                }
            }
        }
        private int InternalGetInterfaceInheritDeep(Type baseType, Type subType)
        {
            if (baseType == subType) return 0;
            if (TryGetDeclaration(subType, out var declaration) && set.Add(declaration) && declaration is AbstractInterface abstractInterface)
            {
                var min = -1;
                foreach (var inherit in abstractInterface.inherits)
                {
                    var deep = GetInterfaceInheritDeep(inherit, subType);
                    if (deep >= 0 && (deep < min || min < 0)) min = deep;
                }
                if (min >= 0) min++;
                return min;
            }
            return -1;
        }
        public int GetInterfaceInheritDeep(Type baseType, Type subType)
        {
            set.Clear();
            return InternalGetInterfaceInheritDeep(baseType, subType);
        }
        public bool TryGetDeclaration(Type type, [MaybeNullWhen(false)] out AbstractDeclaration declaration)
        {
            if (type.code == TypeCode.Invalid)
            {
                declaration = default;
                return false;
            }
            if (librarys.TryGetValue(type.library, out var library))
                switch (type.code)
                {
                    case TypeCode.Invalid: break;
                    case TypeCode.Struct:
                        declaration = library.structs[type.index];
                        return true;
                    case TypeCode.Enum:
                        declaration = library.enums[type.index];
                        return true;
                    case TypeCode.Handle:
                        declaration = library.classes[type.index];
                        return true;
                    case TypeCode.Interface:
                        declaration = library.interfaces[type.index];
                        return true;
                    case TypeCode.Delegate:
                        declaration = library.delegates[type.index];
                        return true;
                    case TypeCode.Task:
                        declaration = library.tasks[type.index];
                        return true;
                }
            declaration = null;
            return false;
        }
        public bool TryGetDefineDeclaration(Declaration declaration, [MaybeNullWhen(false)] out AbstractDeclaration abstractDeclaration)
        {
            if (librarys.TryGetValue(declaration.library, out var library))
                switch (declaration.category)
                {
                    case DeclarationCategory.Invalid: break;
                    case DeclarationCategory.Variable:
                        abstractDeclaration = library.variables[declaration.index];
                        return true;
                    case DeclarationCategory.Function:
                        abstractDeclaration = library.functions[declaration.index];
                        return true;
                    case DeclarationCategory.Enum:
                        abstractDeclaration = library.enums[declaration.index];
                        return true;
                    case DeclarationCategory.EnumElement:
                        abstractDeclaration = library.enums[declaration.define];
                        return true;
                    case DeclarationCategory.Struct:
                        abstractDeclaration = library.structs[declaration.index];
                        return true;
                    case DeclarationCategory.StructVariable:
                    case DeclarationCategory.StructFunction:
                        abstractDeclaration = library.structs[declaration.define];
                        return true;
                    case DeclarationCategory.Class:
                        abstractDeclaration = library.classes[declaration.index];
                        return true;
                    case DeclarationCategory.Constructor:
                    case DeclarationCategory.ClassVariable:
                    case DeclarationCategory.ClassFunction:
                        abstractDeclaration = library.classes[declaration.define];
                        return true;
                    case DeclarationCategory.Interface:
                        abstractDeclaration = library.interfaces[declaration.index];
                        return true;
                    case DeclarationCategory.InterfaceFunction:
                        abstractDeclaration = library.interfaces[declaration.define];
                        return true;
                    case DeclarationCategory.Delegate:
                        abstractDeclaration = library.delegates[declaration.index];
                        return true;
                    case DeclarationCategory.Task:
                        abstractDeclaration = library.tasks[declaration.index];
                        return true;
                    case DeclarationCategory.Native:
                        abstractDeclaration = library.natives[declaration.index];
                        return true;
                }
            abstractDeclaration = null;
            return false;
        }
        public bool TryGetDeclaration(Declaration declaration, [MaybeNullWhen(false)] out AbstractDeclaration abstractDeclaration)
        {
            if (librarys.TryGetValue(declaration.library, out var library))
                switch (declaration.category)
                {
                    case DeclarationCategory.Invalid: break;
                    case DeclarationCategory.Variable:
                        abstractDeclaration = library.variables[declaration.index];
                        return true;
                    case DeclarationCategory.Function:
                        abstractDeclaration = library.functions[declaration.index];
                        return true;
                    case DeclarationCategory.Enum:
                        abstractDeclaration = library.enums[declaration.index];
                        return true;
                    case DeclarationCategory.EnumElement:
                        abstractDeclaration = library.enums[declaration.define].elements[declaration.index];
                        return true;
                    case DeclarationCategory.Struct:
                        abstractDeclaration = library.structs[declaration.index];
                        return true;
                    case DeclarationCategory.StructVariable:
                        abstractDeclaration = library.structs[declaration.define].variables[declaration.index];
                        return true;
                    case DeclarationCategory.StructFunction:
                        abstractDeclaration = library.structs[declaration.define].functions[declaration.index];
                        return true;
                    case DeclarationCategory.Class:
                        abstractDeclaration = library.classes[declaration.index];
                        return true;
                    case DeclarationCategory.Constructor:
                        abstractDeclaration = library.classes[declaration.define].constructors[declaration.index];
                        return true;
                    case DeclarationCategory.ClassVariable:
                        abstractDeclaration = library.classes[declaration.define].variables[declaration.index];
                        return true;
                    case DeclarationCategory.ClassFunction:
                        abstractDeclaration = library.classes[declaration.define].functions[declaration.index];
                        return true;
                    case DeclarationCategory.Interface:
                        abstractDeclaration = library.interfaces[declaration.index];
                        return true;
                    case DeclarationCategory.InterfaceFunction:
                        abstractDeclaration = library.interfaces[declaration.define].functions[declaration.index];
                        return true;
                    case DeclarationCategory.Delegate:
                        abstractDeclaration = library.delegates[declaration.index];
                        return true;
                    case DeclarationCategory.Task:
                        abstractDeclaration = library.tasks[declaration.index];
                        return true;
                    case DeclarationCategory.Native:
                        abstractDeclaration = library.natives[declaration.index];
                        return true;
                }
            abstractDeclaration = null;
            return false;
        }
        public List<AbstractDeclaration> ToDeclarations(IEnumerable<Declaration> declarations)
        {
            var results = new List<AbstractDeclaration>();
            ToDeclarations(declarations, results);
            return results;
        }
        public void ToDeclarations(IEnumerable<Declaration> declarations, List<AbstractDeclaration> results)
        {
            foreach (var declaration in declarations)
                if (TryGetDeclaration(declaration, out var result))
                    results.Add(result);
        }
        public void Reparse(bool onlyOpened)
        {
            library.Clear();
            fileSpaces.Clear();
            allFileSpaces.Clear();
            relies.Clear();
            librarys.Clear();
            kernel.Clear();
            ParseLibrary(kernel, kernelDocuments);
            librarys.Add(LIBRARY_SELF, library);
            if (documentLoader != null)
            {
                foreach (var document in documentLoader())
                {
                    var reader = new LineReader(document);
                    var file = new FileSpace(null, null, library, document, []);
                    FileParse.ParseSpace(file, reader, -1);
                    file.range = document[0].start & document[document.LineCount - 1].end;
                    fileSpaces.Add(document.path, file);
                    allFileSpaces.Add(document.path, file);
                }
                foreach (var item in fileSpaces)
                    FileTidy.Tidy(this, library, item.Value);
                foreach (var item in fileSpaces)
                    FileLink.Link(this, library, item.Value);
                CheckDeclarationValidity.CheckValidity(this, library);
                CheckImplements.Check(this);
                var constants = new List<AbstractVariable>();
                foreach (var item in library.variables)
                    if (item.isReadonly && item.type.code != TypeCode.Invalid && item.fileVariable.expression != null)
                    {
                        var context = new Context(item.file.space.document, item.space, item.file.space.relies, null);
                        var localContext = new LocalContext(item.file.space.collector, null);
                        var parser = new ExpressionParser(this, context, localContext, item.file.space.collector, false);
                        item.expression = parser.Parse(item.fileVariable.expression.Value);
                        var parameter = new ExpressionParameter(this, item.file.space.collector);
                        item.expression.Read(parameter);
                        constants.Add(item);
                        CheckType(item.expression, item.type, item.file.space.collector);
                    }
                while (constants.RemoveAll(value => value.calculated = value.expression!.Calculability()) > 0) ;
                foreach (var item in constants)
                    item.file.space.collector.Add(item.name, ErrorLevel.Error, "无法计算常量值");
                foreach (var item in library.enums)
                {
                    var context = new Context(item.file.space.document, item.space, item.file.space.relies, null);
                    var localContext = new LocalContext(item.file.space.collector);
                    var parser = new ExpressionParser(this, context, localContext, item.file.space.collector, false);
                    var parameter = new ExpressionParameter(this, item.file.space.collector);
                    var value = 0L;
                    foreach (var element in item.elements)
                        if (element.fileElement.expression == null)
                        {
                            element.value = value++;
                            element.calculated = true;
                        }
                        else
                        {
                            localContext.PushBlock();
                            element.expression = parser.Parse(element.fileElement.expression.Value);
                            element.expression.Read(parameter);
                            if (element.expression.Calculability()) element.calculated = true;
                            else item.file.space.collector.Add(item.name, ErrorLevel.Error, "无法计算常量值，可能存在循环定义");
                            localContext.PopBlock();
                        }
                }
                if (opendDocumentLoader == null || !onlyOpened)
                    foreach (var file in fileSpaces.Values)
                        Parse(file);
                else
                    foreach (var document in opendDocumentLoader())
                        if (fileSpaces.TryGetValue(document.path, out var file))
                            Parse(file);
            }
        }
        private void Parse(FileSpace space)
        {
            foreach (var child in space.children)
                Parse(child);
            var parameter = new ExpressionParameter(this, space.collector);
            var context = new Context(space.document, space.space, space.relies, null);
            foreach (var file in space.variables)
                if (!file.isReadonly && file.expression != null && file.abstractDeclaration is AbstractVariable variable && variable.type.code != TypeCode.Invalid)
                {
                    var localContext = new LocalContext(space.collector);
                    var parser = new ExpressionParser(this, context, localContext, space.collector, false);
                    variable.expression = parser.Parse(file.expression.Value);
                    variable.expression.Read(parameter);
                    CheckType(variable.expression, variable.type, space.collector);
                }
            foreach (var file in space.functions)
                if (file.abstractDeclaration is AbstractFunction function)
                    LogicBlockParser.Parse(this, function.logicBlock, null, function, function.fileFunction.body);
            foreach (var file in space.structs)
                if (file.abstractDeclaration is AbstractStruct abstractStruct)
                    foreach (var function in abstractStruct.functions)
                        LogicBlockParser.Parse(this, function.logicBlock, abstractStruct, function, function.fileFunction.body);
            foreach (var file in space.classes)
                if (file.abstractDeclaration is AbstractClass abstractClass)
                {
                    foreach (var member in abstractClass.variables)
                        if (member.fileVariable.expression != null)
                        {
                            var classContext = new Context(context, abstractClass);
                            var localContext = new LocalContext(space.collector, abstractClass);
                            var parser = new ExpressionParser(this, classContext, localContext, space.collector, false);
                            member.expression = parser.Parse(member.fileVariable.expression.Value);
                            member.expression.Read(parameter);
                            CheckType(member.expression, member.type, space.collector);
                        }
                    foreach (var member in abstractClass.constructors)
                    {
                        if (Lexical.TryAnalysis(member.fileConstructor.expression, 0, out var lexical, space.collector))
                        {
                            var parameterRange = lexical.anchor.end & member.fileConstructor.expression.end;
                            if (lexical.anchor == KeyWords.THIS)
                            {
                                var callables = new List<AbstractCallable>();
                                foreach (var callable in abstractClass.constructors) callables.Add(callable);
                                member.expression = LogicBlockParser.Parse(this, abstractClass, callables, lexical.anchor, parameterRange, space.collector);
                                member.expression.Read(parameter);
                            }
                            else if (lexical.anchor == KeyWords.BASE)
                            {
                                if (TryGetDeclaration(abstractClass.parent, out var declaration) && declaration is AbstractClass parent)
                                {
                                    var selfContext = new Context(context, abstractClass);
                                    var callables = new List<AbstractCallable>();
                                    foreach (var callable in parent.constructors)
                                        if (selfContext.IsVisiable(this, callable.declaration))
                                            callables.Add(callable);
                                    member.expression = LogicBlockParser.Parse(this, abstractClass, callables, lexical.anchor, parameterRange, space.collector);
                                    member.expression.Read(parameter);
                                }
                                else space.collector.Add(lexical.anchor, ErrorLevel.Error, "父类未找到");
                            }
                            else space.collector.Add(lexical.anchor, ErrorLevel.Error, "无效的表达式");
                        }
                        LogicBlockParser.Parse(this, member.logicBlock, abstractClass, member, member.fileConstructor.body);
                    }
                    foreach (var member in abstractClass.functions)
                        LogicBlockParser.Parse(this, member.logicBlock, abstractClass, member, member.fileFunction.body);
                    if (file.descontructor != null)
                        LogicBlockParser.Parse(this, file.descontructor.name, abstractClass.descontructorLogicBlock, abstractClass, space.relies, file.descontructor.body);
                }
        }
        private void CheckType(Expression expression, Type type, MessageCollector collector)
        {
            if (expression.Valid)
            {
                if (!expression.attribute.ContainAny(ExpressionAttribute.Value))
                    collector.Add(expression.range, ErrorLevel.Error, "不是一个值表达式");
                else if (ExpressionParser.Convert(this, expression.tuple[0], type) < 0)
                    collector.Add(expression.range, ErrorLevel.Error, "类型不匹配");
            }
        }
        public static string ToRainScheme(string library, string? path = null)
        {
            if (string.IsNullOrEmpty(path)) return $"{SCHEME}:{library}.rain";
            else return $"{SCHEME}:{library}/{path}.rain";
        }
    }
}
