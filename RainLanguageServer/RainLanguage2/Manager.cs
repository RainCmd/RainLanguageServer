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
                removed.Clear();
            }
        }
        internal class FreeDeclarationIndex
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
                throw new InvalidOperationException();
            }
            public int GetIndex(int library, DeclarationCategory category)
            {
                if (library == LIBRARY_SELF)
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
                return -1;
            }
            public void Recycle(int library, DeclarationCategory category, int index)
            {
                if (library == LIBRARY_SELF)
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
        public const string SCHEME = "rain-language";
        public const string KERNEL = "kernel";
        public const int LIBRARY_SELF = -1;
        public const int LIBRARY_KERNEL = -2;
        public readonly FileManager fileManager;
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
            if (imports != null)
                foreach (var import in imports)
                    this.imports.Add(import);
            this.relyLoader = relyLoader;
            kernelPath = new UnifiedPath(kernelPath);
            using var reader = File.OpenText(kernelPath);
            kernel = LoadLibrary(KERNEL, [new TextDocument(ToRainScheme(KERNEL), reader.ReadToEnd())], out var files);
            foreach (var file in files)
                FileLink.Link(this, kernel, file);
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
        public static string ToRainScheme(string library, string? path = null)
        {
            if (!string.IsNullOrEmpty(path)) return $"{SCHEME}:{library}.rain";
            else return $"{SCHEME}:{library}/{path}.rain";
        }
    }
}
