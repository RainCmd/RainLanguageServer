﻿
namespace RainLanguageServer.RainLanguage2
{
    internal class FileType(TextRange range, QualifiedName name, int dimension)
    {
        public readonly TextRange range = range;
        public readonly QualifiedName name = name;
        public readonly int dimension = dimension;
    }
    internal class FileParameter(TextRange range, FileType type, TextRange? name)
    {
        public readonly TextRange range = range;
        public readonly FileType type = type;
        public readonly TextRange? name = name;
    }
    internal class FileDeclaration(FileSpace space, Visibility visibility, TextRange name)
    {
        public List<TextLine> annotation = [];

        public TextRange range;
        public readonly FileSpace space = space;
        public readonly Visibility visibility = visibility;
        public readonly TextRange name = name;
        public readonly List<TextRange> attributes = [];
    }
    internal class FileVariable(FileSpace space, Visibility visibility, TextRange name, bool isReadonly, FileType type, TextRange? expression = null)
        : FileDeclaration(space, visibility, name)
    {
        public readonly bool isReadonly = isReadonly;
        public readonly FileType type = type;
        public readonly TextRange? expression = expression;
    }
    internal class FileFunction(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns)
        : FileDeclaration(space, visibility, name)
    {
        public readonly List<FileParameter> parameters = parameters;
        public readonly List<FileType> returns = returns;
        public readonly List<TextLine> body = [];
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
    }
    internal class FileStruct(FileSpace space, Visibility visibility, TextRange name)
        : FileDeclaration(space, visibility, name)
    {
        internal class Variable(FileSpace space, TextRange name, FileType type)
            : FileDeclaration(space, Visibility.Public, name)
        {
            public readonly FileType type = type;
        }
        internal class Function(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns) :
            FileDeclaration(space, visibility, name)
        {
            public readonly List<FileParameter> parameters = parameters;
            public readonly List<FileType> returns = returns;
            public readonly List<TextLine> body = [];
        }
        public readonly List<Variable> variables = [];
        public readonly List<Function> functions = [];
    }
    internal class FileInterface(FileSpace space, Visibility visibility, TextRange name)
        : FileDeclaration(space, visibility, name)
    {
        internal class Function(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns)
            : FileDeclaration(space, visibility, name)
        {
            public readonly List<FileParameter> parameters = parameters;
            public readonly List<FileType> returns = returns;
        }
        public readonly List<FileType> inherits = [];
        public readonly List<Function> functions = [];
    }
    internal class FileClass(FileSpace space, Visibility visibility, TextRange name)
        : FileDeclaration(space, visibility, name)
    {
        internal class Variable(FileSpace space, Visibility visibility, TextRange name, FileType type, TextRange? expression)
            : FileDeclaration(space, visibility, name)
        {
            public readonly FileType type = type;
            public readonly TextRange? expression = expression;
        }
        internal class Constructor(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns, TextRange? expression)
            : FileDeclaration(space, visibility, name)
        {
            public readonly List<FileParameter> parameters = parameters;
            public readonly List<FileType> returns = returns;
            public readonly TextRange? expression = expression;
            public readonly List<TextLine> body = [];
        }
        internal class Function(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns)
            : FileDeclaration(space, visibility, name)
        {
            public readonly List<FileParameter> parameters = parameters;
            public readonly List<FileType> returns = returns;
            public readonly List<TextLine> body = [];
        }
        internal class Descontructor(TextRange range, List<TextLine> body)
        {
            public readonly TextRange range = range;
            public readonly List<TextLine> body = body;
        }
        public readonly List<FileType> inherits = [];
        public readonly List<Variable> variables = [];
        public readonly List<Constructor> constructors = [];
        public readonly List<Function> functions = [];
        public Descontructor? descontructor;
    }
    internal class FileDelegate(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns)
        : FileDeclaration(space, visibility, name)
    {
        public readonly List<FileParameter> parameters = parameters;
        public readonly List<FileType> returns = returns;
    }
    internal class FileTask(FileSpace space, Visibility visibility, TextRange name, List<FileType> returns)
        : FileDeclaration(space, visibility, name)
    {
        public readonly List<FileType> returns = returns;
    }
    internal class FileNative(FileSpace space, Visibility visibility, TextRange name, List<FileParameter> parameters, List<FileType> returns)
        : FileDeclaration(space, visibility, name)
    {
        public readonly List<FileParameter> parameters = parameters;
        public readonly List<FileType> returns = returns;
    }
    internal readonly struct ImportSpaceInfo(List<TextRange> names)
    {
        public readonly TextRange range = names[0] & names[^1];
        public readonly List<TextRange> names = names;
    }
    internal class FileSpace(QualifiedName? name, FileSpace? parent, TextDocument document)
    {
        public TextRange range;
        public QualifiedName? name = name;
        public readonly FileSpace? parent = parent;
        public readonly TextDocument document = document;
        public readonly MessageCollector collector = [];

        public readonly List<ImportSpaceInfo> imports = [];
        public readonly List<FileSpace> children = [];

        public readonly List<FileVariable> variables = [];
        public readonly List<FileFunction> functions = [];
        public readonly List<FileStruct> structs = [];
        public readonly List<FileInterface> interfaces = [];
        public readonly List<FileClass> classes = [];
        public readonly List<FileDelegate> delegates = [];
        public readonly List<FileTask> tasks = [];
        public readonly List<FileNative> natives = [];
    }
}