namespace RainLanguageServer.RainLanguage2
{
    internal abstract class AbstractDeclaration : IDisposable
    {
        public readonly FileDeclaration file;
        public readonly AbstractSpace space;
        public readonly TextRange name;
        public readonly Declaration declaration;
        public readonly HashSet<TextRange> references = [];

        public AbstractDeclaration(FileDeclaration file, AbstractSpace space, TextRange name, Declaration declaration)
        {
            this.file = file;
            this.space = space;
            this.name = name;
            this.declaration = declaration;
            file.abstractDeclaration = this;
        }

        public virtual void Dispose(Manager manager) { }//todo dispose

        public void Mark(Manager manager) { }//todo mark
    }
    internal class AbstractVariable(FileVariable file, AbstractSpace space, TextRange name, Declaration declaration, bool isReadonly, Type type)
        : AbstractDeclaration(file, space, name, declaration)
    {
        public readonly bool isReadonly = isReadonly;
        public readonly Type type = type;
        //todo expression
        public readonly HashSet<TextRange> write = [];
    }
    internal class AbstractCallable : AbstractDeclaration
    {
        internal readonly struct Parameter(Type type, TextRange? name)
        {
            public readonly Type type = type;
            public readonly TextRange? name = name;
        }
        public readonly List<Parameter> parameters;
        public readonly Tuple signature;
        public readonly Tuple returns;

        public AbstractCallable(FileDeclaration file, AbstractSpace space, TextRange name, Declaration declaration, List<Parameter> parameters, Tuple returns) : base(file, space, name, declaration)
        {
            this.parameters = parameters;
            Type[] signature = new Type[parameters.Count];
            for (int i = 0; i < signature.Length; i++)
                signature[i] = parameters[i].type;
            this.signature = new Tuple(signature);
            this.returns = returns;
        }
    }
    internal class AbstractFunction(FileFunction file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns)
        : AbstractCallable(file, space, name, declaration, parameters, returns)
    {
        //todo logicBlock
    }
    internal class AbstractEnum(FileEnum file, AbstractSpace space, TextRange name, Declaration declaration)
        : AbstractDeclaration(file, space, name, declaration)
    {
        internal class Element(FileEnum.Element file, AbstractSpace space, TextRange name, Declaration declaration, bool valid)
            : AbstractDeclaration(file, space, name, declaration)
        {
            public readonly bool valid = valid;
            public long? value;
            //todo expression
        }
        public readonly List<Element> elements = [];
    }
    internal class AbstractStruct(FileStruct file, AbstractSpace space, TextRange name, Declaration declaration)
        : AbstractDeclaration(file, space, name, declaration)
    {
        internal class Variable(FileStruct.Variable file, AbstractSpace space, TextRange name, Declaration declaration, Type type, bool valid)
            : AbstractDeclaration(file, space, name, declaration)
        {
            public readonly bool valid = valid;
            public readonly Type type = type;
            public readonly HashSet<TextRange> write = [];
        }
        internal class Function(FileStruct.Function file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns, bool valid)
            : AbstractCallable(file, space, name, declaration, parameters, returns)
        {
            public readonly bool valid = valid;
            //todo logicBlock
        }
        public readonly List<Variable> variables = [];
        public readonly List<Function> functions = [];
    }
    internal class AbstractInterface(FileInterface file, AbstractSpace space, TextRange name, Declaration declaration)
        : AbstractDeclaration(file, space, name, declaration)
    {
        internal class Function(FileInterface.Function file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns, bool valid)
            : AbstractCallable(file, space, name, declaration, parameters, returns)
        {
            public readonly bool valid = valid;
            public readonly List<AbstractClass.Function> implements = [];
        }
        public readonly List<Type> inherits = [];
        public readonly List<Function> functions = [];
        public readonly List<AbstractDeclaration> implements = [];
    }
    internal class AbstractClass(FileClass file, AbstractSpace space, TextRange name, Declaration declaration)
        : AbstractDeclaration(file, space, name, declaration)
    {
        internal class Variable(FileClass.Variable file, AbstractSpace space, TextRange name, Declaration declaration, Type type, bool valid)
            : AbstractDeclaration(file, space, name, declaration)
        {
            public readonly bool valid = valid;
            public readonly Type type = type;
            //todo expression
            public readonly HashSet<TextRange> write = [];
        }
        internal class Constructor(FileClass.Constructor file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns)
            : AbstractCallable(file, space, name, declaration, parameters, returns)
        {
            //todo expression
            //todo logicBlock
        }
        internal class Function(FileClass.Function file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns, bool valid)
            : AbstractCallable(file, space, name, declaration, parameters, returns)
        {
            public readonly bool valid = valid;
            //todo logicBlock
            public readonly List<AbstractCallable> overrides = [];
            public readonly List<Function> implements = [];
        }
        public Type parent;
        public readonly List<Type> inherits = [];
        public readonly List<Variable> variables = [];
        public readonly List<Constructor> constructors = [];
        public readonly List<Function> functions = [];
        //todo descontructor
        public readonly List<AbstractClass> implements = [];
    }
    internal class AbstructDelegate(FileDelegate file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns)
        : AbstractCallable(file, space, name, declaration, parameters, returns)
    {
    }
    internal class AbstructTask(FileTask file, AbstractSpace space, TextRange name, Declaration declaration, Tuple returns)
        : AbstractDeclaration(file, space, name, declaration)
    {
        public readonly Tuple returns = returns;
    }
    internal class AbstructNative(FileNative file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns)
        : AbstractCallable(file, space, name, declaration, parameters, returns)
    {
    }
    internal class AbstractSpace(AbstractSpace? parent, string name)
    {
        public readonly AbstractSpace? parent = parent;
        public readonly string name = name;
        public readonly List<TextRange> attributes = [];
        public readonly Dictionary<string, AbstractSpace> children = [];
        public readonly Dictionary<string, List<Declaration>> declarations = [];
        public readonly HashSet<TextRange> references = [];
#if DEBUG
        private readonly HashSet<FileSpace> files = [];
        public void AddDeclaractionFile(FileSpace space)
        {
            if (!files.Add(space)) throw new InvalidOperationException();
        }

        public void RemoveDeclaractionFile(Manager manager, FileSpace space)
        {
            if (!files.Remove(space)) throw new InvalidOperationException();
            if (parent != null && files.Count == 0)
            {
                parent.children.Remove(name);
                Dispose(manager);
            }
        }
#else
        private int files = 0;
        public void AddDeclaractionFile() => files++;

        public void RemoveDeclaractionFile(Manager manager)
        {
            if (files == 0) throw new InvalidOperationException();
            else if (--files == 0 && parent != null)
            {
                parent.children.Remove(name);
                Dispose(manager);
            }
        }
#endif
        public AbstractSpace GetChild(string name)
        {
            if (children.TryGetValue(name, out var child)) return child;
            else return children[name] = new AbstractSpace(this, name);
        }
        public void Dispose(Manager manager) { }//todo dispose
    }
    internal class AbstractLibrary(int library, string name) : AbstractSpace(null, name)
    {
        public readonly int library = library;
        public readonly List<AbstractVariable> variables = [];
        public readonly List<AbstractFunction> functions = [];
        public readonly List<AbstractEnum> enums = [];
        public readonly List<AbstractStruct> structs = [];
        public readonly List<AbstractInterface> interfaces = [];
        public readonly List<AbstractClass> classes = [];
        public readonly List<AbstructDelegate> delegates = [];
        public readonly List<AbstructTask> tasks = [];
        public readonly List<AbstructNative> natives = [];
    }
}
