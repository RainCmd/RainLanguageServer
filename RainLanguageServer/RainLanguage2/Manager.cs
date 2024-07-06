using LanguageServer;
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage2
{
    internal interface IDisposable
    {
        void Dispose(Manager manager);
    }
    internal class Manager
    {
        internal class FileManager(Manager manager)
        {
            private readonly Manager manager = manager;
            private readonly HashSet<TextDocument> dirtys = [];
            public void OnChanged(TextDocument document)
            {
                dirtys.Add(document);
                OnRemove(document.path);
            }
            public void OnRemove(string path)
            {
                if (manager.fileSpaces.Remove(path, out var space))
                    space.Dispose(manager);
            }
            public void Parse()
            {
                foreach (var document in dirtys)
                {
                    var reader = new LineReader(document);
                    var space = new FileSpace(null, null, manager.library, document, []);
                    FileParse.ParseSpace(space, reader, -1);
                    manager.fileSpaces.Add(document.path, space);
                }
            }
        }
        public const string SCHEME = "rain-language";
        public const string KERNEL = "kernel";
        public const int LIBRARY_SELF = -1;
        public const int LIBRARY_KERNEL = -2;
        public readonly AbstractLibrary library;
        public readonly AbstractLibrary kernel;
        public readonly Dictionary<string, FileSpace> fileSpaces = [];
        public readonly Dictionary<string, AbstractLibrary> relies = [];
        private readonly Dictionary<int, AbstractLibrary> librarys = [];
        private readonly HashSet<string> imports = [];
        private readonly Func<string, TextDocument[]> relyLoader;
        public Manager(string name, string kernelPath, string[]? imports, Func<string, TextDocument[]> relyLoader)
        {
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
