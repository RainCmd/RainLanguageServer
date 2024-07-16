using LanguageServer;
using System.Diagnostics.CodeAnalysis;
#if DEBUG
using IndexPool = System.Collections.Generic.HashSet<int>;
#else
using IndexPool = System.Collections.Generic.Stack<int>;
#endif

namespace RainLanguageServer.RainLanguage2
{
    internal interface IDisposable
    {
        void Mark(Manager manager);
        void Dispose(Manager manager);
    }
    internal class Manager
    {
        internal class FileManager(Manager manager)
        {
            private readonly Manager manager = manager;
            private readonly Dictionary<string, TextDocument> removed = [];
            private void Remove(string path, bool mark)
            {
                if (manager.fileSpaces.Remove(path, out var space))
                {
                    if (mark) space.Mark(manager);
                    space.Dispose(manager);
                }
                removed.Remove(path);
            }
            public void OnChanged(TextDocument document)
            {
                Remove(document.path, true);
                removed.Add(document.path, document);
            }
            public void OnRemove(string path)
            {
                Remove(path, true);
            }
            public void Parse()
            {
                var dirtys = new List<string>();
                foreach (var file in manager.fileSpaces)
                    if (file.Value.dirty)
                        dirtys.Add(file.Key);
                foreach (var dirty in dirtys)
                    Remove(dirty, false);

                foreach (var item in removed)
                {
                    var reader = new LineReader(item.Value);
                    var space = new FileSpace(null, null, manager.library, item.Value, []);
                    FileParse.ParseSpace(space, reader, -1);
                    manager.fileSpaces.Add(item.Key, space);
                }
                foreach (var item in removed)
                    FileTidy.Tidy(manager, manager.library, manager.fileSpaces[item.Key]);
                foreach (var item in removed)
                    FileLink.Link(manager, manager.library, manager.fileSpaces[item.Key]);

                removed.Clear();
            }
        }
        internal class IndexManager
        {
            private readonly IndexPool variables = [];
            private readonly IndexPool functions = [];
            private readonly IndexPool enums = [];
            private readonly IndexPool structs = [];
            private readonly IndexPool classes = [];
            private readonly IndexPool interfaces = [];
            private readonly IndexPool delegates = [];
            private readonly IndexPool tasks = [];
            private readonly IndexPool natives = [];
            private IndexPool GetPool(DeclarationCategory category)
            {
                switch (category)
                {
                    case DeclarationCategory.Invalid:
                        break;
                    case DeclarationCategory.Variable: return variables;
                    case DeclarationCategory.Function: return functions;
                    case DeclarationCategory.Enum: return enums;
                    case DeclarationCategory.EnumElement:
                        break;
                    case DeclarationCategory.Struct: return structs;
                    case DeclarationCategory.StructVariable:
                    case DeclarationCategory.StructFunction:
                        break;
                    case DeclarationCategory.Class: return classes;
                    case DeclarationCategory.Constructor:
                    case DeclarationCategory.ClassVariable:
                    case DeclarationCategory.ClassFunction:
                        break;
                    case DeclarationCategory.Interface: return interfaces;
                    case DeclarationCategory.InterfaceFunction:
                        break;
                    case DeclarationCategory.Delegate: return delegates;
                    case DeclarationCategory.Task: return tasks;
                    case DeclarationCategory.Native: return natives;
                }
                throw new InvalidOperationException("无效的声明类型");
            }
            public int GetIndex(AbstractLibrary library, DeclarationCategory category)
            {
                if (library.library == LIBRARY_SELF)
                {
                    var pool = GetPool(category);
#if DEBUG
                    if (pool.Count > 0)
                    {
                        var result = pool.First();
                        pool.Remove(result);
                        return result;
                    }
#else
                    if (pool.Count > 0) return pool.Pop();
#endif
                }
                switch (category)
                {
                    case DeclarationCategory.Invalid:
                        break;
                    case DeclarationCategory.Variable:
                        return library.variables.Count;
                    case DeclarationCategory.Function:
                        return library.functions.Count;
                    case DeclarationCategory.Enum:
                        return library.enums.Count;
                    case DeclarationCategory.EnumElement:
                        break;
                    case DeclarationCategory.Struct:
                        return library.structs.Count;
                    case DeclarationCategory.StructVariable:
                        break;
                    case DeclarationCategory.StructFunction:
                        break;
                    case DeclarationCategory.Class:
                        return library.classes.Count;
                    case DeclarationCategory.Constructor:
                        break;
                    case DeclarationCategory.ClassVariable:
                        break;
                    case DeclarationCategory.ClassFunction:
                        break;
                    case DeclarationCategory.Interface:
                        return library.interfaces.Count;
                    case DeclarationCategory.InterfaceFunction:
                        break;
                    case DeclarationCategory.Delegate:
                        return library.delegates.Count;
                    case DeclarationCategory.Task:
                        return library.tasks.Count;
                    case DeclarationCategory.Native:
                        return library.natives.Count;
                    default:
                        break;
                }
                throw new InvalidOperationException("无效的类型");
            }
            public void Recycle(AbstractLibrary library, DeclarationCategory category, int index)
            {
                if (library.library == LIBRARY_SELF)
                {
                    var pool = GetPool(category);
#if DEBUG
                    if (pool.Contains(index)) throw new InvalidOperationException("重复回收索引");
                    pool.Add(index);
#else
                    pool.Push(index);
#endif
                }
                else throw new InvalidOperationException("被引用的库不应该会出现回收索引");
            }
        }
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
        public readonly FileManager fileManager;
        public readonly IndexManager indexManager = new();
        public readonly KernelManager kernelManager;
        public readonly AbstractLibrary library;
        public readonly AbstractLibrary kernel;
        public readonly Dictionary<string, FileSpace> fileSpaces = [];
        public readonly Dictionary<string, AbstractLibrary> relies = [];
        private readonly Dictionary<int, AbstractLibrary> librarys = [];
        private readonly HashSet<string> imports = [];
        private readonly Func<string, TextDocument[]> relyLoader;
        public Manager(string name, string kernelPath, string[]? imports, Func<string, TextDocument[]> relyLoader)
        {
            fileManager = new FileManager(this);
            library = new AbstractLibrary(LIBRARY_SELF, name);
            librarys.Add(LIBRARY_SELF, library);
            if (imports != null) this.imports.AddRange(imports);
            this.relyLoader = relyLoader;
            kernelPath = new UnifiedPath(kernelPath);
            using var reader = File.OpenText(kernelPath);
            kernel = LoadLibrary(KERNEL, [new TextDocument(ToRainScheme(KERNEL), reader.ReadToEnd())], out var files);
            foreach (var file in files)
                FileLink.Link(this, kernel, file);
            kernelManager = new KernelManager(kernel);
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
                library = LoadLibrary(name, relyLoader(name), out var files);
                relies[name] = library;
                foreach (var file in files)
                    FileLink.Link(this, kernel, file);
                return true;
            }
            library = null;
            return false;
        }
        private AbstractLibrary LoadLibrary(string name, TextDocument[] documents, out List<FileSpace> files)
        {
            files = [];
            var result = new AbstractLibrary(name == KERNEL ? LIBRARY_KERNEL : relies.Count, name);
            librarys.Add(result.library, result);
            foreach (var document in documents)
                files.Add(FileParse.ParseSpace(result, document));
            foreach (var file in files)
                FileTidy.Tidy(this, result, file);
            return result;
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
            foreach (var declaration in declarations)
                if (TryGetDeclaration(declaration, out var result))
                    results.Add(result);
            return results;
        }
        public static string ToRainScheme(string library, string? path = null)
        {
            if (!string.IsNullOrEmpty(path)) return $"{SCHEME}:{library}.rain";
            else return $"{SCHEME}:{library}/{path}.rain";
        }
    }
}
