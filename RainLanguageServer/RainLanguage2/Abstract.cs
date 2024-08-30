using LanguageServer.Parameters;
using LanguageServer.Parameters.TextDocument;
using RainLanguageServer.RainLanguage.GrammaticalAnalysis;
using System.Text;

namespace RainLanguageServer.RainLanguage
{
    internal abstract class AbstractDeclaration
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
        public string FullName
        {
            get
            {
                var sb = new StringBuilder(name.ToString());
                for (var index = space; index != null; index = index.parent)
                {
                    sb.Insert(0, '.');
                    sb.Insert(0, index.name);
                }
                return sb.ToString();
            }
        }
        public abstract bool OnHover(Manager manager, TextPosition position, out HoverInfo info);
        public virtual bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (name.Contain(position))
            {
                InfoUtility.Highlight(this, infos);
                return true;
            }
            return false;
        }
        public virtual bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (name.Contain(position))
            {
                definition = name;
                return true;
            }
            definition = default;
            return false;
        }
        public virtual bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (name.Contain(position))
            {
                references.AddRange(this.references);
                return true;
            }
            return false;
        }
        public abstract void CollectSemanticToken(Manager manager, SemanticTokenCollector collector);
    }
    internal class AbstractVariable(FileVariable file, AbstractSpace space, TextRange name, Declaration declaration, bool isReadonly, Type type)
        : AbstractDeclaration(file, space, name, declaration)
    {
        public readonly FileVariable fileVariable = file;
        public readonly bool isReadonly = isReadonly;
        public readonly Type type = type;
        public Expression? expression;
        public bool calculated = false;
        public readonly HashSet<TextRange> write = [];
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (name.Contain(position))
            {
                var sb = new StringBuilder();
                if (isReadonly) sb.Append("(常量)");
                else sb.Append("(全局变量)");
                sb.Append(type.Info(manager, false, space));
                sb.Append(' ');
                sb.Append(name.ToString());
                info = new HoverInfo(name, sb.ToString().MakedownCode(), true);
                return true;
            }
            else if (fileVariable.type.OnHover(manager, position, type, space, out info)) return true;
            else if (expression != null && expression.range.Contain(position)) return expression.OnHover(manager, position, out info);
            info = default;
            return false;
        }
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (base.OnHighlight(manager, position, infos))
            {
                foreach (var range in write)
                    infos.Add(new HighlightInfo(range, DocumentHighlightKind.Write));
                return true;
            }
            if (fileVariable.type.OnHighlight(manager, position, type, infos)) return true;
            if (expression != null && expression.range.Contain(position)) return expression.OnHighlight(manager, position, infos);
            return false;
        }
        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (base.TryGetDefinition(manager, position, out definition)) return true;
            if (fileVariable.type.TryGetDefinition(manager, position, type, out definition)) return true;
            if (expression != null && expression.TryGetDefinition(manager, position, out definition)) return true;
            return false;
        }
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (base.FindReferences(manager, position, references))
            {
                references.AddRange(write);
                return true;
            }
            if (fileVariable.type.FindReferences(manager, position, type, references)) return true;
            if (expression != null && expression.range.Contain(position)) return expression.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.AddType(fileVariable.type, manager, type);
            collector.Add(DetailTokenType.GlobalVariable, name);
            expression?.CollectSemanticToken(manager, collector);
        }
    }
    internal abstract class AbstractCallable : AbstractDeclaration
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
        protected bool OnHover(Manager manager, TextPosition position, List<FileType> returns, List<FileParameter> parameters, LogicBlock? block, AbstractDeclaration? declaration, out HoverInfo info)
        {
            if (name.Contain(position))
            {
                info = new HoverInfo(name, this.Info(manager, declaration, space).MakedownCode(), true);
                return true;
            }
            for (var i = 0; i < returns.Count; i++)
                if (returns[i].OnHover(manager, position, this.returns[i], space, out info))
                    return true;
            for (var i = 0; i < parameters.Count; i++)
                if (parameters[i].range.Contain(position))
                {
                    var parameter = parameters[i];
                    if (parameter.type.OnHover(manager, position, signature[i], space, out info)) return true;
                    else if (parameter.name != null && parameter.name.Value.Contain(position))
                    {
                        var sb = new StringBuilder();
                        sb.Append("(参数)");
                        sb.Append(signature[i].Info(manager, false, space));
                        sb.Append(' ');
                        sb.Append(parameter.name.ToString());
                        info = new HoverInfo(parameter.range, sb.ToString().MakedownCode(), true);
                        return true;
                    }
                }
            if (block != null)
                foreach (var statement in block.statements)
                    if (statement.range.Contain(position))
                        return statement.OnHover(manager, position, out info);
            info = default;
            return false;
        }
        protected bool OnHighlight(Manager manager, TextPosition position, List<FileType> returns, List<FileParameter> parameters, LogicBlock? block, List<HighlightInfo> infos)
        {
            if (base.OnHighlight(manager, position, infos)) return true;
            for (var i = 0; i < returns.Count; i++)
                if (returns[i].OnHighlight(manager, position, this.returns[i], infos))
                    return true;
            for (var i = 0; i < parameters.Count; i++)
                if (parameters[i].range.Contain(position))
                {
                    var parameter = parameters[i];
                    if (parameter.type.OnHighlight(manager, position, signature[i], infos)) return true;
                    else if (parameter.name != null && parameter.name.Value.Contain(position))
                    {
                        if (block != null) block.parameters[i].OnHighlight(infos);
                        else infos.Add(new HighlightInfo(parameter.name.Value, DocumentHighlightKind.Text));
                        return true;
                    }
                }
            if (block != null)
                foreach (var statement in block.statements)
                    if (statement.range.Contain(position))
                        return statement.OnHighlight(manager, position, infos);
            return false;
        }
        protected bool TryGetDefinition(Manager manager, TextPosition position, List<FileType> returns, List<FileParameter> parameters, LogicBlock? block, out TextRange definition)
        {
            if (base.TryGetDefinition(manager, position, out definition)) return true;
            for (var i = 0; i < returns.Count; i++)
                if (returns[i].TryGetDefinition(manager, position, this.returns[i], out definition))
                    return true;
            for (var i = 0; i < parameters.Count; i++)
                if (parameters[i].range.Contain(position))
                {
                    var parameter = parameters[i];
                    if (parameter.type.TryGetDefinition(manager, position, signature[i], out definition)) return true;
                    else if (parameter.name != null && parameter.name.Value.Contain(position))
                    {
                        definition = parameter.name.Value;
                        return true;
                    }
                }
            if (block != null)
                foreach (var statement in block.statements)
                    if (statement.range.Contain(position))
                        return statement.TryGetDefinition(manager, position, out definition);
            return false;
        }
        protected bool FindReferences(Manager manager, TextPosition position, List<FileType> returns, List<FileParameter> parameters, LogicBlock? block, List<TextRange> references)
        {
            if (base.FindReferences(manager, position, references)) return true;
            for (var i = 0; i < returns.Count; i++)
                if (returns[i].FindReferences(manager, position, this.returns[i], references))
                    return true;
            for (var i = 0; i < parameters.Count; i++)
                if (parameters[i].range.Contain(position))
                {
                    var parameter = parameters[i];
                    if (parameter.type.FindReferences(manager, position, signature[i], references)) return true;
                    else if (parameter.name != null && parameter.name.Value.Contain(position))
                    {
                        block?.parameters[i].FindReferences(references);
                        return true;
                    }
                }
            if (block != null)
                foreach (var statement in block.statements)
                    if (statement.range.Contain(position))
                        return statement.FindReferences(manager, position, references);
            return false;
        }
        protected void CollectSemanticToken(Manager manager, SemanticTokenCollector collector, List<FileType> returns, List<FileParameter> parameters, LogicBlock? block)
        {
            for (var i = 0; i < returns.Count; i++)
                collector.AddType(returns[i], manager, this.returns[i]);
            for (var i = 0; i < parameters.Count; i++)
            {
                collector.AddType(parameters[i].type, manager, signature[i]);
                var name = parameters[i].name;
                if (name != null)
                    collector.Add(DetailTokenType.Parameter, name.Value);
            }
            if (block != null)
                foreach (var statement in block.statements)
                    statement.CollectSemanticToken(manager, collector);
        }
    }
    internal class AbstractFunction(FileFunction file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns)
        : AbstractCallable(file, space, name, declaration, parameters, returns)
    {
        public readonly FileFunction fileFunction = file;
        public readonly LogicBlock logicBlock = new();
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info) => OnHover(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, null, out info);
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => OnHighlight(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, infos);
        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition) => TryGetDefinition(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, out definition);
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => FindReferences(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, references);
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.GlobalFunction, name);
            CollectSemanticToken(manager, collector, fileFunction.returns, fileFunction.parameters, logicBlock);
        }
    }
    internal class AbstractEnum(FileEnum file, AbstractSpace space, TextRange name, Declaration declaration)
        : AbstractDeclaration(file, space, name, declaration)
    {
        internal class Element(FileEnum.Element file, AbstractSpace space, TextRange name, Declaration declaration, bool valid)
            : AbstractDeclaration(file, space, name, declaration)
        {
            public readonly FileEnum.Element fileElement = file;
            public readonly bool valid = valid;
            public long value;
            public Expression? expression;
            public bool calculated = false;
            public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
            {
                if (name.Contain(position))
                {
                    info = new HoverInfo(name, this.Info(manager, space).MakedownCode(), true);
                    return true;
                }
                else if (expression != null && expression.range.Contain(position))
                    return expression.OnHover(manager, position, out info);
                info = default;
                return false;
            }
            public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
            {
                if (base.OnHighlight(manager, position, infos)) return true;
                if (expression != null && expression.range.Contain(position))
                    return expression.OnHighlight(manager, position, infos);
                return false;
            }
            public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
            {
                if (base.TryGetDefinition(manager, position, out definition)) return true;
                if (expression != null && expression.range.Contain(position))
                    return expression.TryGetDefinition(manager, position, out definition);
                return false;
            }
            public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
            {
                if (base.FindReferences(manager, position, references)) return true;
                if (expression != null && expression.range.Contain(position))
                    return expression.FindReferences(manager, position, references);
                return false;
            }
            public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
            {
                collector.Add(DetailTokenType.EnumElement, name);
                expression?.CollectSemanticToken(manager, collector);
            }
        }
        public readonly FileEnum fileEnum = file;
        public readonly List<Element> elements = [];
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (name.Contain(position))
            {
                info = new HoverInfo(name, this.Info(manager, space).MakedownCode(), true);
                return true;
            }
            foreach (var element in elements)
                if (element.fileElement.range.Contain(position))
                    return element.OnHover(manager, position, out info);
            info = default;
            return false;
        }
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (base.OnHighlight(manager, position, infos)) return true;
            foreach (var element in elements)
                if (element.fileElement.range.Contain(position))
                    return element.OnHighlight(manager, position, infos);
            return false;
        }
        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (base.TryGetDefinition(manager, position, out definition)) return true;
            foreach (var element in elements)
                if (element.fileElement.range.Contain(position))
                    return element.TryGetDefinition(manager, position, out definition);
            return false;
        }
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (base.FindReferences(manager, position, references)) return true;
            foreach (var element in elements)
                if (element.fileElement.range.Contain(position))
                    return element.FindReferences(manager, position, references);
            return false;
        }
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.EnumType, name);
            foreach (var element in elements)
                element.CollectSemanticToken(manager, collector);
        }
    }
    internal class AbstractStruct(FileStruct file, AbstractSpace space, TextRange name, Declaration declaration)
        : AbstractDeclaration(file, space, name, declaration)
    {
        internal class Variable(FileStruct.Variable file, AbstractSpace space, TextRange name, Declaration declaration, Type type, bool valid)
            : AbstractDeclaration(file, space, name, declaration)
        {
            public readonly FileStruct.Variable fileVariable = file;
            public readonly bool valid = valid;
            public readonly Type type = type;
            public readonly HashSet<TextRange> write = [];
            public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
            {
                if (name.Contain(position))
                {
                    info = new HoverInfo(name, this.Info(manager, space).MakedownCode(), true);
                    return true;
                }
                return fileVariable.type.OnHover(manager, position, type, space, out info);
            }
            public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
            {
                if (base.OnHighlight(manager, position, infos))
                {
                    foreach (var range in write)
                        infos.Add(new HighlightInfo(range, DocumentHighlightKind.Write));
                    return true;
                }
                return fileVariable.type.OnHighlight(manager, position, type, infos);
            }
            public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
            {
                if (base.TryGetDefinition(manager, position, out definition)) return true;
                return fileVariable.type.TryGetDefinition(manager, position, type, out definition);
            }
            public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
            {
                if (base.FindReferences(manager, position, references))
                {
                    references.AddRange(this.write);
                    return true;
                }
                return fileVariable.type.FindReferences(manager, position, type, references);
            }
            public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector) => collector.Add(DetailTokenType.MemberField, name);
        }
        internal class Function(FileStruct.Function file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns, bool valid)
            : AbstractCallable(file, space, name, declaration, parameters, returns)
        {
            public readonly FileStruct.Function fileFunction = file;
            public readonly bool valid = valid;
            public readonly LogicBlock logicBlock = new();
            public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
            {
                if (!manager.TryGetDefineDeclaration(declaration, out var abstractDeclaration)) throw new InvalidOperationException();
                return OnHover(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, abstractDeclaration, out info);
            }
            public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => OnHighlight(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, infos);
            public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition) => TryGetDefinition(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, out definition);
            public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => FindReferences(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, references);
            public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
            {
                collector.Add(DetailTokenType.MemberFunction, name);
                CollectSemanticToken(manager, collector, fileFunction.returns, fileFunction.parameters, logicBlock);
            }
        }
        public readonly FileStruct fileStruct = file;
        public readonly List<Variable> variables = [];
        public readonly List<Function> functions = [];
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (name.Contain(position))
            {
                info = new HoverInfo(name, this.Info(manager, space).MakedownCode(), true);
                return true;
            }
            foreach (var variable in variables)
                if (variable.fileVariable.range.Contain(position))
                    return variable.OnHover(manager, position, out info);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.OnHover(manager, position, out info);
            info = default;
            return false;
        }
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (base.OnHighlight(manager, position, infos)) return true;
            foreach (var variable in variables)
                if (variable.fileVariable.range.Contain(position))
                    return variable.OnHighlight(manager, position, infos);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.OnHighlight(manager, position, infos);
            return false;
        }
        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (base.TryGetDefinition(manager, position, out definition)) return true;
            foreach (var variable in variables)
                if (variable.fileVariable.range.Contain(position))
                    return variable.TryGetDefinition(manager, position, out definition);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.TryGetDefinition(manager, position, out definition);
            return false;
        }
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (base.FindReferences(manager, position, references)) return true;
            foreach (var variable in variables)
                if (variable.fileVariable.range.Contain(position))
                    return variable.FindReferences(manager, position, references);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.FindReferences(manager, position, references);
            return false;
        }
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.StructType, name);
            foreach (var variable in variables) variable.CollectSemanticToken(manager, collector);
            foreach (var function in functions) function.CollectSemanticToken(manager, collector);
        }
    }
    internal class AbstractInterface(FileInterface file, AbstractSpace space, TextRange name, Declaration declaration)
        : AbstractDeclaration(file, space, name, declaration)
    {
        internal class Function(FileInterface.Function file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns, bool valid)
            : AbstractCallable(file, space, name, declaration, parameters, returns)
        {
            public readonly FileInterface.Function fileFunction = file;
            public readonly bool valid = valid;
            public readonly List<AbstractClass.Function> implements = [];
            public readonly List<AbstractCallable> overrides = [];//父接口中同名同参的函数
            public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
            {
                if (!manager.TryGetDefineDeclaration(declaration, out var abstractDeclaration)) throw new InvalidOperationException();
                return OnHover(manager, position, fileFunction.returns, fileFunction.parameters, null, abstractDeclaration, out info);
            }
            public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => OnHighlight(manager, position, fileFunction.returns, fileFunction.parameters, null, infos);
            public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition) => TryGetDefinition(manager, position, fileFunction.returns, fileFunction.parameters, null, out definition);
            public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => FindReferences(manager, position, fileFunction.returns, fileFunction.parameters, null, references);
            public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
            {
                collector.Add(DetailTokenType.MemberFunction, name);
                CollectSemanticToken(manager, collector, fileFunction.returns, fileFunction.parameters, null);
            }
        }
        public readonly FileInterface fileInterface = file;
        public readonly List<Type> inherits = [];
        public readonly List<Function> functions = [];
        public readonly List<AbstractDeclaration> implements = [];
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (name.Contain(position))
            {
                info = new HoverInfo(name, this.Info(manager, space).MakedownCode(), true);
                return true;
            }
            for (var i = 0; i < fileInterface.inherits.Count; i++)
                if (fileInterface.inherits[i].range.Contain(position))
                    return fileInterface.inherits[i].OnHover(manager, position, inherits[i], space, out info);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.OnHover(manager, position, out info);
            info = default;
            return false;
        }
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (base.OnHighlight(manager, position, infos)) return true;
            for (var i = 0; i < fileInterface.inherits.Count; i++)
                if (fileInterface.inherits[i].range.Contain(position))
                    return fileInterface.inherits[i].OnHighlight(manager, position, inherits[i], infos);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.OnHighlight(manager, position, infos);
            return false;
        }
        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (base.TryGetDefinition(manager, position, out definition)) return true;
            for (var i = 0; i < fileInterface.inherits.Count; i++)
                if (fileInterface.inherits[i].range.Contain(position))
                    return fileInterface.inherits[i].TryGetDefinition(manager, position, inherits[i], out definition);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.TryGetDefinition(manager, position, out definition);
            return false;
        }
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (base.FindReferences(manager, position, references)) return true;
            for (var i = 0; i < fileInterface.inherits.Count; i++)
                if (fileInterface.inherits[i].range.Contain(position))
                    return fileInterface.inherits[i].FindReferences(manager, position, inherits[i], references);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.FindReferences(manager, position, references);
            return false;
        }
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.InterfaceType, name);
            for (var i = 0; i < fileInterface.inherits.Count; i++)
                collector.AddType(fileInterface.inherits[i], manager, inherits[i]);
            foreach (var function in functions)
                function.CollectSemanticToken(manager, collector);
        }
    }
    internal class AbstractClass(FileClass file, AbstractSpace space, TextRange name, Declaration declaration)
        : AbstractDeclaration(file, space, name, declaration)
    {
        internal class Variable(FileClass.Variable file, AbstractSpace space, TextRange name, Declaration declaration, Type type, bool valid)
            : AbstractDeclaration(file, space, name, declaration)
        {
            public readonly FileClass.Variable fileVariable = file;
            public readonly bool valid = valid;
            public readonly Type type = type;
            public Expression? expression;
            public readonly HashSet<TextRange> write = [];
            public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
            {
                if (name.Contain(position))
                {
                    info = new HoverInfo(name, this.Info(manager, space).MakedownCode(), true);
                    return true;
                }
                else if (fileVariable.type.OnHover(manager, position, type, space, out info)) return true;
                else if (expression != null && expression.range.Contain(position))
                    return expression.OnHover(manager, position, out info);
                info = default;
                return false;
            }
            public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
            {
                if (base.OnHighlight(manager, position, infos))
                {
                    foreach (var range in write)
                        infos.Add(new HighlightInfo(range, DocumentHighlightKind.Write));
                    return true;
                }
                if (fileVariable.type.OnHighlight(manager, position, type, infos)) return true;
                if (expression != null && expression.range.Contain(position))
                    return expression.OnHighlight(manager, position, infos);
                return fileVariable.range.Contain(position);
            }
            public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
            {
                if (base.TryGetDefinition(manager, position, out definition)) return true;
                if (fileVariable.type.TryGetDefinition(manager, position, type, out definition)) return true;
                if (expression != null && expression.range.Contain(position))
                    return expression.TryGetDefinition(manager, position, out definition);
                return false;
            }
            public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
            {
                if (base.FindReferences(manager, position, references))
                {
                    references.AddRange(write);
                    return true;
                }
                if (fileVariable.type.FindReferences(manager, position, type, references)) return true;
                if (expression != null && expression.range.Contain(position))
                    return expression.FindReferences(manager, position, references);
                return fileVariable.range.Contain(position);
            }
            public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
            {
                collector.Add(DetailTokenType.MemberField, name);
                collector.AddType(fileVariable.type, manager, type);
                expression?.CollectSemanticToken(manager, collector);
            }
        }
        internal class Constructor(FileClass.Constructor file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns)
            : AbstractCallable(file, space, name, declaration, parameters, returns)
        {
            public readonly FileClass.Constructor fileConstructor = file;
            public Expression? expression;
            public readonly LogicBlock logicBlock = new();
            public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
            {
                if (expression != null && expression.range.Contain(position)) return expression.OnHover(manager, position, out info);
                else if (!manager.TryGetDefineDeclaration(declaration, out var abstractDeclaration)) throw new InvalidOperationException();
                else return OnHover(manager, position, fileConstructor.returns, fileConstructor.parameters, logicBlock, abstractDeclaration, out info);
            }
            public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
            {
                if (expression != null && expression.range.Contain(position)) return expression.OnHighlight(manager, position, infos);
                return (OnHighlight(manager, position, fileConstructor.returns, fileConstructor.parameters, logicBlock, infos));
            }
            public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
            {
                if (expression != null && expression.range.Contain(position)) return expression.TryGetDefinition(manager, position, out definition);
                return TryGetDefinition(manager, position, fileConstructor.returns, fileConstructor.parameters, logicBlock, out definition);
            }
            public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
            {
                if (expression != null && expression.range.Contain(position)) return expression.FindReferences(manager, position, references);
                return (FindReferences(manager, position, fileConstructor.returns, fileConstructor.parameters, logicBlock, references));
            }
            public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
            {
                collector.Add(DetailTokenType.Constructor, name);
                expression?.CollectSemanticToken(manager, collector);
                CollectSemanticToken(manager, collector, fileConstructor.returns, fileConstructor.parameters, logicBlock);
            }
        }
        internal class Function(FileClass.Function file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns, bool valid)
            : AbstractCallable(file, space, name, declaration, parameters, returns)
        {
            public readonly FileClass.Function fileFunction = file;
            public readonly bool valid = valid;
            public readonly LogicBlock logicBlock = new();
            public readonly List<AbstractCallable> overrides = [];//所有被override的函数，包括接口的
            public readonly List<Function> implements = [];
            public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
            {
                if (!manager.TryGetDefineDeclaration(declaration, out var abstractDeclaration)) throw new InvalidOperationException();
                return OnHover(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, abstractDeclaration, out info);
            }
            public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
            {
                if (name.Contain(position))
                {
                    InfoUtility.Highlight(this, infos);
                    foreach (var function in overrides)
                        InfoUtility.Highlight(function, infos);
                    return true;
                }
                return OnHighlight(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, infos);
            }
            public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition) => TryGetDefinition(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, out definition);
            public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
            {
                if (name.Contain(position))
                {
                    references.AddRange(this.references);
                    foreach (var function in overrides)
                        references.AddRange(function.references);
                    return true;
                }
                return FindReferences(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, references);
            }
            public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
            {
                collector.Add(DetailTokenType.MemberFunction, name);
                CollectSemanticToken(manager, collector, fileFunction.returns, fileFunction.parameters, logicBlock);
            }
        }
        public readonly FileClass fileClass = file;
        public Type parent;
        public readonly List<Type> inherits = [];
        public readonly List<Variable> variables = [];
        public readonly List<Constructor> constructors = [];
        public readonly List<Function> functions = [];
        public readonly LogicBlock descontructorLogicBlock = new();
        public readonly List<AbstractClass> implements = [];
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (name.Contain(position))
            {
                info = new HoverInfo(name, this.Info(manager, space).MakedownCode(), true);
                return true;
            }
            for (var i = 0; i < fileClass.inherits.Count; i++)
                if (fileClass.inherits[i].range.Contain(position))
                {
                    if (fileClass.inherits.Count == inherits.Count) return fileClass.inherits[i].OnHover(manager, position, inherits[i], space, out info);
                    else if (i == 0) return fileClass.inherits[i].OnHover(manager, position, parent, space, out info);
                    else return fileClass.inherits[i].OnHover(manager, position, inherits[i - 1], space, out info);
                }
            foreach (var variable in variables)
                if (variable.fileVariable.range.Contain(position))
                    return variable.OnHover(manager, position, out info);
            foreach (var constructor in constructors)
                if (constructor.fileConstructor.range.Contain(position))
                    return constructor.OnHover(manager, position, out info);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.OnHover(manager, position, out info);
            foreach (var statement in descontructorLogicBlock.statements)
                if (statement.range.Contain(position))
                    return statement.OnHover(manager, position, out info);
            info = default;
            return false;
        }
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (base.OnHighlight(manager, position, infos)) return true;
            for (var i = 0; i < fileClass.inherits.Count; i++)
                if (fileClass.inherits[i].range.Contain(position))
                {
                    if (fileClass.inherits.Count == inherits.Count) return fileClass.inherits[i].OnHighlight(manager, position, inherits[i], infos);
                    else if (i == 0) return fileClass.inherits[i].OnHighlight(manager, position, parent, infos);
                    else return fileClass.inherits[i].OnHighlight(manager, position, inherits[i - 1], infos);
                }
            foreach (var variable in variables)
                if (variable.fileVariable.range.Contain(position))
                    return variable.OnHighlight(manager, position, infos);
            foreach (var constructor in constructors)
                if (constructor.fileConstructor.range.Contain(position))
                    return constructor.OnHighlight(manager, position, infos);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.OnHighlight(manager, position, infos);
            foreach (var statement in descontructorLogicBlock.statements)
                if (statement.range.Contain(position))
                    return statement.OnHighlight(manager, position, infos);
            return false;
        }
        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (base.TryGetDefinition(manager, position, out definition)) return true;
            for (var i = 0; i < fileClass.inherits.Count; i++)
                if (fileClass.inherits[i].range.Contain(position))
                {
                    if (fileClass.inherits.Count == inherits.Count) return fileClass.inherits[i].TryGetDefinition(manager, position, inherits[i], out definition);
                    else if (i == 0) return fileClass.inherits[i].TryGetDefinition(manager, position, parent, out definition);
                    else return fileClass.inherits[i].TryGetDefinition(manager, position, inherits[i - 1], out definition);
                }
            foreach (var variable in variables)
                if (variable.fileVariable.range.Contain(position))
                    return variable.TryGetDefinition(manager, position, out definition);
            foreach (var constructor in constructors)
                if (constructor.fileConstructor.range.Contain(position))
                    return constructor.TryGetDefinition(manager, position, out definition);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.TryGetDefinition(manager, position, out definition);
            foreach (var statement in descontructorLogicBlock.statements)
                if (statement.range.Contain(position))
                    return statement.TryGetDefinition(manager, position, out definition);
            return false;
        }
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (base.FindReferences(manager, position, references)) return true;
            for (var i = 0; i < fileClass.inherits.Count; i++)
                if (fileClass.inherits[i].range.Contain(position))
                {
                    if (fileClass.inherits.Count == inherits.Count) return fileClass.inherits[i].FindReferences(manager, position, inherits[i], references);
                    else if (i == 0) return fileClass.inherits[i].FindReferences(manager, position, parent, references);
                    else return fileClass.inherits[i].FindReferences(manager, position, inherits[i - 1], references);
                }
            foreach (var variable in variables)
                if (variable.fileVariable.range.Contain(position))
                    return variable.FindReferences(manager, position, references);
            foreach (var constructor in constructors)
                if (constructor.fileConstructor.range.Contain(position))
                    return constructor.FindReferences(manager, position, references);
            foreach (var function in functions)
                if (function.fileFunction.range.Contain(position))
                    return function.FindReferences(manager, position, references);
            foreach (var statement in descontructorLogicBlock.statements)
                if (statement.range.Contain(position))
                    return statement.FindReferences(manager, position, references);
            return false;
        }
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.HandleType, name);
            for (var i = 0; i < fileClass.inherits.Count; i++)
            {
                if (fileClass.inherits.Count == inherits.Count) collector.AddType(fileClass.inherits[i], manager, inherits[i]);
                else if (i == 0) collector.AddType(fileClass.inherits[i], manager, parent);
                else collector.AddType(fileClass.inherits[i], manager, inherits[i - 1]);
            }
            foreach (var member in variables) member.CollectSemanticToken(manager, collector);
            foreach (var member in constructors) member.CollectSemanticToken(manager, collector);
            foreach (var member in functions) member.CollectSemanticToken(manager, collector);
            foreach (var statement in descontructorLogicBlock.statements) statement.CollectSemanticToken(manager, collector);
        }
    }
    internal class AbstractDelegate(FileDelegate file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns)
        : AbstractCallable(file, space, name, declaration, parameters, returns)
    {
        public readonly FileDelegate fileDelegate = file;
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info) => OnHover(manager, position, fileDelegate.returns, fileDelegate.parameters, null, null, out info);
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => OnHighlight(manager, position, fileDelegate.returns, fileDelegate.parameters, null, infos);
        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition) => TryGetDefinition(manager, position, fileDelegate.returns, fileDelegate.parameters, null, out definition);
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => FindReferences(manager, position, fileDelegate.returns, fileDelegate.parameters, null, references);
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.DelegateType, name);
            CollectSemanticToken(manager, collector, fileDelegate.returns, fileDelegate.parameters, null);
        }
    }
    internal class AbstractTask(FileTask file, AbstractSpace space, TextRange name, Declaration declaration, Tuple returns)
        : AbstractDeclaration(file, space, name, declaration)
    {
        public readonly FileTask fileTask = file;
        public readonly Tuple returns = returns;
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (name.Contain(position))
            {
                info = new HoverInfo(name, this.Info(manager, space).MakedownCode(), true);
                return true;
            }
            for (var i = 0; i < fileTask.returns.Count; i++)
                if (fileTask.returns[i].OnHover(manager, position, returns[i], space, out info))
                    return true;
            info = default;
            return false;
        }
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (base.OnHighlight(manager, position, infos)) return true;
            for (var i = 0; i < returns.Count; i++)
                if (fileTask.returns[i].OnHighlight(manager, position, returns[i], infos))
                    return true;
            return false;
        }
        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (base.TryGetDefinition(manager, position, out definition)) return true;
            for (var i = 0; i < returns.Count; i++)
                if (fileTask.returns[i].TryGetDefinition(manager, position, returns[i], out definition))
                    return true;
            return false;
        }
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (base.FindReferences(manager, position, references)) return true;
            for (var i = 0; i < returns.Count; i++)
                if (fileTask.returns[i].FindReferences(manager, position, returns[i], references))
                    return true;
            return false;
        }
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.TaskType, name);
            for (var i = 0; i < returns.Count; i++)
                collector.AddType(fileTask.returns[i], manager, returns[i]);
        }
    }
    internal class AbstractNative(FileNative file, AbstractSpace space, TextRange name, Declaration declaration, List<AbstractCallable.Parameter> parameters, Tuple returns)
        : AbstractCallable(file, space, name, declaration, parameters, returns)
    {
        public readonly FileNative fileNative = file;
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info) => OnHover(manager, position, fileNative.returns, fileNative.parameters, null, null, out info);
        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos) => OnHighlight(manager, position, fileNative.returns, fileNative.parameters, null, infos);
        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition) => TryGetDefinition(manager, position, fileNative.returns, fileNative.parameters, null, out definition);
        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references) => FindReferences(manager, position, fileNative.returns, fileNative.parameters, null, references);
        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.NativeFunction, name);
            CollectSemanticToken(manager, collector, fileNative.returns, fileNative.parameters, null);
        }
    }
    internal class AbstractSpace(AbstractSpace? parent, string name)
    {
        public readonly AbstractSpace? parent = parent;
        public readonly string name = name;
        public readonly List<TextRange> attributes = [];
        public readonly Dictionary<string, AbstractSpace> children = [];
        public readonly Dictionary<string, List<Declaration>> declarations = [];
        public readonly HashSet<TextRange> references = [];
        public string FullName
        {
            get
            {
                var name = new StringBuilder(this.name);
                for (var index = parent; index != null; index = index.parent)
                {
                    name.Insert(0, '.');
                    name.Insert(0, index.name);
                }
                return name.ToString();
            }
        }
        public AbstractSpace GetChild(string name)
        {
            if (children.TryGetValue(name, out var child)) return child;
            else return children[name] = new AbstractSpace(this, name);
        }
        public bool Contain(AbstractSpace? space)
        {
            while (space != null)
            {
                if (space == this) return true;
                space = space.parent;
            }
            return false;
        }
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
        public readonly List<AbstractDelegate> delegates = [];
        public readonly List<AbstractTask> tasks = [];
        public readonly List<AbstractNative> natives = [];

        public void Clear()
        {
            attributes.Clear();
            children.Clear();
            declarations.Clear();
            references.Clear();

            variables.Clear();
            functions.Clear();
            enums.Clear();
            structs.Clear();
            interfaces.Clear();
            classes.Clear();
            delegates.Clear();
            tasks.Clear();
            natives.Clear();
        }
    }
}
