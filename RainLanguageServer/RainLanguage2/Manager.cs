using LanguageServer;
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage2
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
            public readonly Type HANDLE = GetType(kernel.structs, "handle");
            public readonly Type INTERFACE = GetType(kernel.structs, "interface");
            public readonly Type DELEGATE = GetType(kernel.structs, "Delegate");
            public readonly Type TASK = GetType(kernel.structs, "Task");
            public readonly Type ARRAY = GetType(kernel.structs, "array");
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
        public readonly Dictionary<string, AbstractLibrary> relies = [];
        private readonly Dictionary<int, AbstractLibrary> librarys = [];
        private readonly HashSet<string> imports = [];
        private readonly Dictionary<string, TextDocument[]> importDocuments = [];
        private readonly Func<string, TextDocument[]> relyLoader;
        private readonly Func<IEnumerable<TextDocument>> documentLoader;
        private readonly Func<IEnumerable<TextDocument>> opendDocumentLoader;
        public Manager(string name, string kernelPath, string[]? imports, Func<string, TextDocument[]> relyLoader, Func<IEnumerable<TextDocument>> documentLoader, Func<IEnumerable<TextDocument>> opendDocumentLoader)
        {
            library = new AbstractLibrary(LIBRARY_SELF, name);
            librarys.Add(LIBRARY_SELF, library);
            if (imports != null) this.imports.AddRange(imports);
            this.relyLoader = relyLoader;
            kernelPath = new UnifiedPath(kernelPath);
            using var reader = File.OpenText(kernelPath);
            kernel = new AbstractLibrary(LIBRARY_KERNEL, KERNEL);
            kernelDocuments = [new TextDocument(ToRainScheme(KERNEL), reader.ReadToEnd())];
            Reparse();
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
                files.Add(FileParse.ParseSpace(library, document));
            foreach (var file in files)
                FileTidy.Tidy(this, library, file);
            foreach (var file in files)
                FileLink.Link(this, library, file);
            foreach (var file in files)
                file.collector.Clear();
        }
        public AbstractLibrary GetLibrary(int library) => librarys[library];
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
        public void Reparse()
        {
            library.Clear();
            fileSpaces.Clear();
            relies.Clear();
            librarys.Clear();
            kernel.Clear();
            ParseLibrary(kernel, kernelDocuments);
            if (documentLoader != null)
            {
                foreach (var document in documentLoader())
                {
                    var reader = new LineReader(document);
                    var file = new FileSpace(null, null, library, document, []);
                    FileParse.ParseSpace(file, reader, -1);
                    fileSpaces.Add(document.path, file);
                }
                foreach (var item in fileSpaces)
                    FileTidy.Tidy(this, library, item.Value);
                foreach (var item in fileSpaces)
                    FileLink.Link(this, library, item.Value);
                //todo 重命名检查
                //todo 继承检查
                //todo 常量和枚举的表达式解析
                if (opendDocumentLoader != null)
                    foreach (var document in opendDocumentLoader())
                    {
                        //todo 其他表达式的解析
                    }
            }
        }
        public static string ToRainScheme(string library, string? path = null)
        {
            if (!string.IsNullOrEmpty(path)) return $"{SCHEME}:{library}.rain";
            else return $"{SCHEME}:{library}/{path}.rain";
        }
    }
}
