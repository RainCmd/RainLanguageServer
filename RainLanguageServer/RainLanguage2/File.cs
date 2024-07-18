namespace RainLanguageServer.RainLanguage2
{
    internal class FileType(TextRange range, QualifiedName name, int dimension) : IRainObject
    {
        public readonly TextRange range = range;
        public readonly QualifiedName name = name;
        public readonly int dimension = dimension;
        public AbstractDeclaration? target;

        public void SetDeclaration(AbstractDeclaration declaration)
        {
            target = declaration;
            declaration.references.Add(name.name);
            var space = declaration.space;
            for (int i = 0; i < name.qualify.Count; i++)
            {
                if (i > 0) space = space!.parent;
                space!.references.Add(name.qualify[^(i + 1)]);
            }
        }
        public void Dispose(Manager manager)
        {
            if (target == null) return;
            target.references.Remove(name.name);
            var space = target.space;
            for (int i = 0; i < name.qualify.Count; i++)
            {
                if (i > 0) space = space!.parent;
                space!.references.Remove(name.qualify[^(i + 1)]);
            }
        }

        public void Mark(Manager manager)
        {

        }
    }
    internal class FileParameter(FileType type, TextRange? name)
    {
        public readonly TextRange range = name == null ? type.range : type.range & name.Value;
        public readonly FileType type = type;
        public readonly TextRange? name = name;
    }
    internal class FileDeclaration(FileSpace space, Visibility visibility, TextRange name) : IRainObject
    {
        public List<TextLine> annotation = [];

        public TextRange range;
        public readonly FileSpace space = space;
        public readonly Visibility visibility = visibility;
        public readonly TextRange name = name;
        public readonly List<TextRange> attributes = [];

        public AbstractDeclaration? abstractDeclaration;

        public virtual void Mark(Manager manager) 
        {
            abstractDeclaration?.Mark(manager);
        }

        public virtual void Dispose(Manager manager)
        {
            abstractDeclaration?.Mark(manager);
        }
    }
    internal class FileVariable(FileSpace space, Visibility visibility, TextRange name, bool isReadonly, FileType type, TextRange? expression = null)
        : FileDeclaration(space, visibility, name)
    {
        public readonly bool isReadonly = isReadonly;
        public readonly FileType type = type;
        public readonly TextRange? expression = expression;
        public override void Dispose(Manager manager)
        {
            type.Dispose(manager);
            base.Dispose(manager);
        }
    }
    internal class FileFunction(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns)
        : FileDeclaration(space, visibility, name)
    {
        public readonly List<FileParameter> parameters = parameters;
        public readonly List<FileType> returns = returns;
        public readonly List<TextLine> body = [];
        public override void Dispose(Manager manager)
        {
            foreach(var parameter in parameters) parameter.type.Dispose(manager);
            foreach (var type in returns) type.Dispose(manager);
            base.Dispose(manager);
        }
    }
    internal class FileEnum(FileSpace space, Visibility visibility, TextRange name)
        : FileDeclaration(space, visibility, name)
    {
        internal class Element(FileSpace space, TextRange name, TextRange? expression)
            : FileDeclaration(space, Visibility.Public, name)
        {
            public readonly TextRange? expression = expression;
        }
        public readonly List<Element> elements = [];
        public override void Dispose(Manager manager)
        {
            foreach(var element in elements) element.Dispose(manager);
            base.Dispose(manager);
        }
        public override void Mark(Manager manager)
        {
            foreach (var element in elements) element.Mark(manager);
            base.Mark(manager);
        }
    }
    internal class FileStruct(FileSpace space, Visibility visibility, TextRange name)
        : FileDeclaration(space, visibility, name)
    {
        internal class Variable(FileSpace space, TextRange name, FileType type)
            : FileDeclaration(space, Visibility.Public, name)
        {
            public readonly FileType type = type;
            public override void Dispose(Manager manager)
            {
                type.Dispose(manager);
                base.Dispose(manager);
            }
        }
        internal class Function(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns) :
            FileDeclaration(space, visibility, name)
        {
            public readonly List<FileParameter> parameters = parameters;
            public readonly List<FileType> returns = returns;
            public readonly List<TextLine> body = [];
            public override void Dispose(Manager manager)
            {
                foreach(var parameter in parameters) parameter.type.Dispose(manager);
                foreach(var type in returns) type.Dispose(manager);
                base.Dispose(manager);
            }
        }
        public readonly List<Variable> variables = [];
        public readonly List<Function> functions = [];
        public override void Mark(Manager manager)
        {
            foreach(var variable in variables) variable.Mark(manager);
            foreach(var function in functions) function.Mark(manager);
            base.Mark(manager);
        }
        public override void Dispose(Manager manager)
        {
            foreach (var variable in variables) variable.Dispose(manager);
            foreach (var function in functions) function.Dispose(manager);
            base.Dispose(manager);
        }
    }
    internal class FileInterface(FileSpace space, Visibility visibility, TextRange name)
        : FileDeclaration(space, visibility, name)
    {
        internal class Function(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns)
            : FileDeclaration(space, visibility, name)
        {
            public readonly List<FileParameter> parameters = parameters;
            public readonly List<FileType> returns = returns;
            public override void Dispose(Manager manager)
            {
                foreach(var parameter in parameters) parameter.type.Dispose(manager);
                foreach(var type in returns) type.Dispose(manager);
                base.Dispose(manager);
            }
        }
        public readonly List<FileType> inherits = [];
        public readonly List<Function> functions = [];
        public override void Dispose(Manager manager)
        {
            foreach(var type in inherits) type.Dispose(manager);
            foreach(var function in functions) function.Dispose(manager);
            base.Dispose(manager);
        }
        public override void Mark(Manager manager)
        {
            foreach(var function in functions) function.Mark(manager);
            base.Mark(manager);
        }
    }
    internal class FileClass(FileSpace space, Visibility visibility, TextRange name)
        : FileDeclaration(space, visibility, name)
    {
        internal class Variable(FileSpace space, Visibility visibility, TextRange name, FileType type, TextRange? expression)
            : FileDeclaration(space, visibility, name)
        {
            public readonly FileType type = type;
            public readonly TextRange? expression = expression;
            public override void Dispose(Manager manager)
            {
                type.Dispose(manager);
                base.Dispose(manager);
            }
        }
        internal class Constructor(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns, TextRange expression)
            : FileDeclaration(space, visibility, name)
        {
            public readonly List<FileParameter> parameters = parameters;
            public readonly List<FileType> returns = returns;
            public readonly TextRange expression = expression;
            public readonly List<TextLine> body = [];
            public override void Dispose(Manager manager)
            {
                foreach(var parameter in parameters) parameter.type.Dispose(manager);
                foreach(var type in returns) type.Dispose(manager);
                base.Dispose(manager);
            }
        }
        internal class Function(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns)
            : FileDeclaration(space, visibility, name)
        {
            public readonly List<FileParameter> parameters = parameters;
            public readonly List<FileType> returns = returns;
            public readonly List<TextLine> body = [];
            public override void Dispose(Manager manager)
            {
                foreach (var parameter in parameters) parameter.type.Dispose(manager);
                foreach(var type in returns) type.Dispose(manager);
                base.Dispose(manager);
            }
        }
        internal class Descontructor(TextRange name)
        {
            public TextRange range;
            public readonly TextRange name = name;
            public readonly List<TextLine> body = [];
        }
        public readonly List<FileType> inherits = [];
        public readonly List<Variable> variables = [];
        public readonly List<Constructor> constructors = [];
        public readonly List<Function> functions = [];
        public Descontructor? descontructor;
        public override void Dispose(Manager manager)
        {
            foreach(var type in inherits) type.Dispose(manager);
            foreach (var variable in variables) variable.Dispose(manager);
            foreach (var constructor in constructors) constructor.Dispose(manager);
            foreach(var function in functions) function.Dispose(manager);
            base.Dispose(manager);
        }
        public override void Mark(Manager manager)
        {
            foreach(var variable in variables) variable.Mark(manager);
            foreach (var constructor in constructors) constructor.Mark(manager);
            foreach(var function in functions) function.Mark(manager);
            base.Mark(manager);
        }
    }
    internal class FileDelegate(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns)
        : FileDeclaration(space, visibility, name)
    {
        public readonly List<FileParameter> parameters = parameters;
        public readonly List<FileType> returns = returns;
        public override void Dispose(Manager manager)
        {
            foreach(var parameter in parameters) parameter.type.Dispose(manager);
            foreach(var type in returns) type.Dispose(manager);
            base.Dispose(manager);
        }
    }
    internal class FileTask(FileSpace space, Visibility visibility, TextRange name, List<FileType> returns)
        : FileDeclaration(space, visibility, name)
    {
        public readonly List<FileType> returns = returns;
        public override void Dispose(Manager manager)
        {
            foreach (var type in returns) type.Dispose(manager);
            base.Dispose(manager);
        }
    }
    internal class FileNative(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns)
        : FileDeclaration(space, visibility, name)
    {
        public readonly List<FileParameter> parameters = parameters;
        public readonly List<FileType> returns = returns;
        public override void Dispose(Manager manager)
        {
            foreach (var parameter in parameters) parameter.type.Dispose(manager);
            foreach (var type in returns) type.Dispose(manager);
            base.Dispose(manager);
        }
    }
    internal class ImportSpaceInfo(List<TextRange> names) : System.IDisposable
    {
        public readonly TextRange range = names[0] & names[^1];
        public readonly List<TextRange> names = names;
        public AbstractSpace? space;//names[0] 对应的space
        public AbstractSpace? GetSpace(int index)
        {
            var space = this.space;
            if (index >= names.Count || space == null) return null;
            for (int i = 1; i <= index; i++)
                if (!space.children.TryGetValue(names[i].ToString(), out space))
                    return null;
            return space;
        }
        public void Dispose()
        {
            if (space == null) return;
            var index = space;
            index.references.Remove(names[0]);
            for (int i = 1; i < names.Count; i++)
                if (index.children.TryGetValue(names[i].ToString(), out index)) index.references.Remove(names[i]);
                else return;
        }
    }
    internal class FileSpace : IRainObject
    {
        public bool dirty = false;
        public TextRange range;
        public readonly TextRange? name;
        public readonly FileSpace? parent;
        public readonly AbstractSpace space;
        public readonly TextDocument document;
        public readonly MessageCollector collector;

        public readonly List<ImportSpaceInfo> imports = [];
        public readonly List<FileSpace> children = [];

        public readonly List<FileVariable> variables = [];
        public readonly List<FileFunction> functions = [];
        public readonly List<FileEnum> enums = [];
        public readonly List<FileStruct> structs = [];
        public readonly List<FileInterface> interfaces = [];
        public readonly List<FileClass> classes = [];
        public readonly List<FileDelegate> delegates = [];
        public readonly List<FileTask> tasks = [];
        public readonly List<FileNative> natives = [];

        public readonly HashSet<AbstractSpace> relies = [];

        public FileSpace(TextRange? name, FileSpace? parent, AbstractSpace space, TextDocument document, MessageCollector collector)
        {
            this.name = name;
            this.parent = parent;
            this.space = space;
            this.document = document;
            this.collector = collector;
            parent?.children.Add(this);
#if DEBUG
            space.AddDeclaractionFile(this);
#else
            space.AddDeclaractionFile();
#endif
        }

        public IEnumerator<FileDeclaration> GetEnumerator()
        {
            foreach (var file in variables) yield return file;
            foreach (var file in functions) yield return file;
            foreach (var file in structs) yield return file;
            foreach (var file in interfaces) yield return file;
            foreach (var file in classes) yield return file;
            foreach (var file in delegates) yield return file;
            foreach (var file in tasks) yield return file;
            foreach (var file in natives) yield return file;
        }

        public void Mark(Manager manager)
        {
            foreach (var child in children) child.Mark(manager);

            foreach (var file in this) file.Mark(manager);

            dirty = true;
        }
        public void Dispose(Manager manager)
        {
            foreach (var child in children) child.Dispose(manager);

            foreach (var import in imports) import.Dispose();

            foreach (var file in this) file.Dispose(manager);

#if DEBUG
            space.RemoveDeclaractionFile(manager, this);
#else
            space.RemoveDeclaractionFile(manager);
#endif
        }
    }
}
