﻿using LanguageServer.Parameters.TextDocument;
using RainLanguageServer.RainLanguage.GrammaticalAnalysis;
using System.Diagnostics.CodeAnalysis;
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
        public virtual bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }
        public abstract void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges);
        public virtual void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos) { }
        public virtual void CollectInlayHint(Manager manager, List<InlayHintInfo> infos) { }
        public virtual void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos) { }
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
                sb.Append(type.Info(manager, space));
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
            collector.Add(isReadonly ? DetailTokenType.Constant : DetailTokenType.GlobalVariable, name);
            expression?.CollectSemanticToken(manager, collector);
        }
        public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            if (expression != null) return expression.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            return base.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
        }
        public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (name.Contain(position)) InfoUtility.Rename(this, ranges);
            else if (fileVariable.type.Rename(manager, position, type, ranges)) return;
            else if (expression != null && expression.range.Contain(position)) expression.Rename(manager, position, ranges);
        }
        public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos)
        {
            if (fileVariable.type.range.Contain(position)) fileVariable.type.Completion(manager, position, infos, fileVariable.defaultVisibility);
            else if (expression != null && expression.range.Contain(position)) expression.Completion(manager, position, infos);
        }
        public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos) => expression?.CollectInlayHint(manager, infos);
        public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
        {
            if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, isReadonly ? NamingRule.AllCaps : NamingRule.PascalCase, out var info, out var newName))
            {
                InfoUtility.AddEdits(info, references, name.ToString(), newName);
                InfoUtility.AddEdits(info, write, name.ToString(), newName);
                infos.Add(info);
            }
            InfoUtility.CheckDefaultAccess(this, range, infos);
            if (expression != null && expression.range.Overlap(range)) expression.CollectCodeAction(manager, range, infos);
        }
    }
    internal abstract class AbstractCallable : AbstractDeclaration
    {
        internal readonly struct Parameter(Type type, TextRange name)
        {
            public readonly Type type = type;
            public readonly TextRange name = name;
        }
        public readonly List<Parameter> parameters;
        public readonly Tuple signature;
        public readonly Tuple returns;
        private bool IsSelf => declaration.library == Manager.LIBRARY_SELF;
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
                    else if (IsSelf && parameter.name.Contain(position))
                    {
                        var sb = new StringBuilder();
                        sb.Append("(参数)");
                        sb.Append(signature[i].Info(manager, space));
                        sb.Append(' ');
                        sb.Append(parameter.name.ToString());
                        info = new HoverInfo(parameter.range, sb.ToString().MakedownCode(), true);
                        return true;
                    }
                }
            if (IsSelf && block != null)
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
                    else if (IsSelf && parameter.name.Contain(position))
                    {
                        if (block != null) block.parameters[i].OnHighlight(infos);
                        else infos.Add(new HighlightInfo(parameter.name, DocumentHighlightKind.Text));
                        return true;
                    }
                }
            if (IsSelf && block != null)
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
                    else if (IsSelf && parameter.name.Contain(position))
                    {
                        definition = parameter.name;
                        return true;
                    }
                }
            if (IsSelf && block != null)
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
                    else if (parameter.name.Contain(position))
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
                if (IsSelf && name.Count > 0)
                {
                    if (block != null && block.parameters[i].read.Count == 0)
                        collector.Add(DetailTokenType.DeprecatedLocal, name);
                    else
                        collector.Add(DetailTokenType.Parameter, name);
                }
            }
            if (IsSelf && block != null)
                foreach (var statement in block.statements)
                    statement.CollectSemanticToken(manager, collector);
        }
        protected void Rename(Manager manager, TextPosition position, List<FileType> returns, List<FileParameter> parameters, LogicBlock? block, HashSet<TextRange> ranges)
        {
            for (var i = 0; i < returns.Count; i++)
                if (returns[i].Rename(manager, position, this.returns[i], ranges))
                    return;
            for (var i = 0; i < parameters.Count; i++)
                if (parameters[i].range.Contain(position))
                {
                    var parameter = parameters[i];
                    if (parameter.type.Rename(manager, position, signature[i], ranges)) return;
                    else if (IsSelf && parameter.name.Contain(position))
                    {
                        block?.parameters[i].Rename(ranges);
                        return;
                    }
                }
            if (IsSelf && block != null)
                foreach (var statement in block.statements)
                    if (statement.range.Contain(position))
                    {
                        statement.Rename(manager, position, ranges);
                        return;
                    }
        }
        protected static void Completion(Manager manager, TextPosition position, bool defaultVisibility, List<FileType> returns, List<FileParameter> parameters, LogicBlock? block, List<CompletionInfo> infos)
        {
            var first = true;
            foreach (var type in returns)
            {
                if (type.range.Contain(position))
                {
                    type.Completion(manager, position, infos, first && defaultVisibility);
                    return;
                }
                first = false;
            }
            foreach (var parameter in parameters)
                if (parameter.range.Contain(position))
                {
                    if (parameter.type.range.Contain(position)) parameter.type.Completion(manager, position, infos);
                    return;
                }
            if (block != null)
                foreach (var statement in block.statements)
                    if (statement.range.Contain(position))
                    {
                        statement.Completion(manager, position, infos);
                        return;
                    }
        }
        protected static void CollectLogicBlockCodeAction(Manager manager, TextRange range, LogicBlock block, List<CodeActionInfo> infos)
        {
            foreach (var parameter in block.parameters)
                if (parameter.range.Overlap(range) && InfoUtility.CheckNamingRule(parameter.range, NamingRule.CamelCase, out var info, out var newName))
                {
                    InfoUtility.AddEdits(info, parameter.read, parameter.name, newName);
                    InfoUtility.AddEdits(info, parameter.write, parameter.name, newName);
                    infos.Add(info);
                }
            foreach (var statement in block.statements)
                if (statement.range.Overlap(range))
                    statement.CollectCodeAction(manager, range, infos);
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
        public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            foreach (var statement in logicBlock.statements)
                if (statement.range.Contain(position))
                    return statement.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            return base.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
        }
        public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (name.Contain(position)) InfoUtility.Rename(this, ranges);
            else Rename(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, ranges);
        }
        public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos) => Completion(manager, position, fileFunction.defaultVisibility, fileFunction.returns, fileFunction.parameters, logicBlock, infos);
        public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos)
        {
            foreach (var statement in logicBlock.statements)
                statement.CollectInlayHint(manager, infos);
        }
        public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
        {
            if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
            {
                InfoUtility.AddEdits(info, references, name.ToString(), newName);
                infos.Add(info);
            }
            InfoUtility.CheckDefaultAccess(this, range, infos);
            CollectLogicBlockCodeAction(manager, range, logicBlock, infos);
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
                    info = new HoverInfo(name, this.CodeInfo(manager, space), true);
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
                collector.Add(DetailTokenType.MemberElement, name);
                expression?.CollectSemanticToken(manager, collector);
            }
            public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
            {
                if (expression != null && expression.range.Contain(position)) return expression.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
                return base.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            }
            public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
            {
                if (name.Contain(position)) InfoUtility.Rename(this, ranges);
                else if (expression != null && expression.range.Contain(position)) expression.Rename(manager, position, ranges);
            }
            public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos) => expression?.Completion(manager, position, infos);
            public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos) => expression?.CollectInlayHint(manager, infos);
            public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
            {
                if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
                {
                    InfoUtility.AddEdits(info, references, name.ToString(), newName);
                    infos.Add(info);
                }
                if (expression != null && expression.range.Overlap(range)) expression.CollectCodeAction(manager, range, infos);
            }
        }
        public readonly FileEnum fileEnum = file;
        public readonly List<Element> elements = [];
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (name.Contain(position))
            {
                info = new HoverInfo(name, this.CodeInfo(manager, space), true);
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
            collector.Add(DetailTokenType.TypeEnum, name);
            foreach (var element in elements)
                element.CollectSemanticToken(manager, collector);
        }
        public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            foreach (var element in elements)
                if (element.file.range.Contain(position))
                    return element.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            return base.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
        }
        public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (name.Contain(position)) InfoUtility.Rename(this, ranges);
            else foreach (var element in elements)
                    if (element.file.range.Contain(position))
                    {
                        element.Rename(manager, position, ranges);
                        return;
                    }
        }
        public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos)
        {
            foreach (var element in elements)
                if (element.file.range.Contain(position))
                {
                    element.Completion(manager, position, infos);
                    return;
                }
        }
        public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos)
        {
            foreach (var element in elements)
                element.CollectInlayHint(manager, infos);
        }
        public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
        {
            if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
            {
                InfoUtility.AddEdits(info, references, name.ToString(), newName);
                infos.Add(info);
            }
            InfoUtility.CheckDefaultAccess(this, range, infos);
            foreach (var element in elements)
                element.CollectCodeAction(manager, range, infos);
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
                    info = new HoverInfo(name, this.CodeInfo(manager, space), true);
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
            public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
            {
                collector.AddType(fileVariable.type, manager, type);
                collector.Add(DetailTokenType.MemberField, name);
            }
            public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
            {
                if (name.Contain(position)) InfoUtility.Rename(this, ranges);
            }
            public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos)
            {
                if (fileVariable.type.range.Contain(position)) fileVariable.type.Completion(manager, position, infos);
            }
            public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos) => infos.Add(new InlayHintInfo($"{KeyWords.PUBLIC} ", fileVariable.range.Trim.start));
            public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
            {
                if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.CamelCase, out var info, out var newName))
                {
                    InfoUtility.AddEdits(info, references, name.ToString(), newName);
                    infos.Add(info);
                }
            }
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
            public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
            {
                foreach (var statement in logicBlock.statements)
                    if (statement.range.Contain(position))
                        return statement.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
                return base.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            }
            public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
            {
                if (name.Contain(position)) InfoUtility.Rename(this, ranges);
                else Rename(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, ranges);
            }
            public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos) => Completion(manager, position, fileFunction.defaultVisibility, fileFunction.returns, fileFunction.parameters, logicBlock, infos);
            public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos)
            {
                foreach (var statement in logicBlock.statements)
                    statement.CollectInlayHint(manager, infos);
            }
            public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
            {
                if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
                {
                    InfoUtility.AddEdits(info, references, name.ToString(), newName);
                    infos.Add(info);
                }
                InfoUtility.CheckDefaultAccess(this, range, infos);
                CollectLogicBlockCodeAction(manager, range, logicBlock, infos);
            }
        }
        public readonly FileStruct fileStruct = file;
        public readonly List<Variable> variables = [];
        public readonly List<Function> functions = [];
        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (name.Contain(position))
            {
                info = new HoverInfo(name, this.CodeInfo(manager, space), true);
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
            collector.Add(DetailTokenType.TypeStruct, name);
            foreach (var variable in variables) variable.CollectSemanticToken(manager, collector);
            foreach (var function in functions) function.CollectSemanticToken(manager, collector);
        }
        public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            foreach (var function in functions)
                if (function.file.range.Contain(position))
                    return function.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            return base.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
        }
        public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (name.Contain(position)) InfoUtility.Rename(this, ranges);
            else
            {
                foreach (var member in variables)
                    if (member.file.range.Contain(position))
                    {
                        member.Rename(manager, position, ranges);
                        return;
                    }
                foreach (var member in functions)
                    if (member.file.range.Contain(position))
                    {
                        member.Rename(manager, position, ranges);
                        return;
                    }
            }
        }
        public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos)
        {
            foreach (var member in variables)
                if (member.file.range.Contain(position))
                {
                    member.Completion(manager, position, infos);
                    return;
                }
            foreach (var member in functions)
                if (member.file.range.Contain(position))
                {
                    member.Completion(manager, position, infos);
                    return;
                }
            var context = new Context(fileStruct.space.document, space, fileStruct.space.relies, this);
            InfoUtility.Completion(manager, context, position.Line, position, infos, true);
        }
        public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos)
        {
            foreach (var variable in variables)
                variable.CollectInlayHint(manager, infos);
            foreach (var function in functions)
                function.CollectInlayHint(manager, infos);
        }
        public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
        {
            if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
            {
                InfoUtility.AddEdits(info, references, name.ToString(), newName);
                infos.Add(info);
            }
            InfoUtility.CheckDefaultAccess(this, range, infos);
            foreach (var variable in variables)
                variable.CollectCodeAction(manager, range, infos);
            foreach (var function in functions)
                function.CollectCodeAction(manager, range, infos);
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
            public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
            {
                if (name.Contain(position)) InfoUtility.Rename(this, ranges);
                else Rename(manager, position, fileFunction.returns, fileFunction.parameters, null, ranges);
            }
            public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos) => Completion(manager, position, false, fileFunction.returns, fileFunction.parameters, null, infos);
            public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos) => infos.Add(new InlayHintInfo($"{KeyWords.PUBLIC} ", fileFunction.range.Trim.start));
            public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
            {
                if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
                {
                    InfoUtility.AddEdits(info, references, name.ToString(), newName);
                    var ranges = new List<TextRange>();
                    foreach (var item in implements)
                        ranges.AddRange(item.references);
                    foreach (var item in overrides)
                        ranges.AddRange(item.references);
                    InfoUtility.AddEdits(info, ranges, name.ToString(), newName);
                    infos.Add(info);
                }
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
                info = new HoverInfo(name, this.CodeInfo(manager, space), true);
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
            collector.Add(DetailTokenType.TypeInterface, name);
            for (var i = 0; i < fileInterface.inherits.Count; i++)
                collector.AddType(fileInterface.inherits[i], manager, inherits[i]);
            foreach (var function in functions)
                function.CollectSemanticToken(manager, collector);
        }
        public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (name.Contain(position)) InfoUtility.Rename(this, ranges);
            else
            {
                for (int i = 0; i < fileInterface.inherits.Count; i++)
                    if (fileInterface.inherits[i].Rename(manager, position, inherits[i], ranges)) return;
                foreach (var member in functions)
                    if (member.file.range.Contain(position))
                    {
                        member.Rename(manager, position, ranges);
                        return;
                    }
            }
        }
        public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos)
        {
            foreach (var type in fileInterface.inherits)
                if (type.range.Contain(position))
                {
                    type.Completion(manager, position, infos);
                    return;
                }
            foreach (var function in functions)
                if (function.file.range.Contain(position))
                {
                    function.Completion(manager, position, infos);
                    return;
                }
            var context = new Context(fileInterface.space.document, space, fileInterface.space.relies, this);
            InfoUtility.Completion(manager, context, position.Line, position, infos);
        }
        public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos)
        {
            if (fileInterface.inherits.Count > 0)
            {
                infos.Add(new InlayHintInfo(":", name.end));
                for (var i = 0; i < fileInterface.inherits.Count - 1; i++)
                    infos.Add(new InlayHintInfo(",", fileInterface.inherits[i].name.name.end));
            }
            foreach (var function in functions)
                function.CollectInlayHint(manager, infos);
        }
        public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
        {
            if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
            {
                InfoUtility.AddEdits(info, references, name.ToString(), newName);
                infos.Add(info);
            }
            InfoUtility.CheckDefaultAccess(this, range, infos);
            foreach (var function in functions)
                function.CollectCodeAction(manager, range, infos);
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
                    info = new HoverInfo(name, this.CodeInfo(manager, space), true);
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
            public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
            {
                if (expression != null && expression.range.Contain(position)) return expression.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
                return base.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            }
            public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
            {
                if (name.Contain(position)) InfoUtility.Rename(this, ranges);
                else if (fileVariable.type.Rename(manager, position, type, ranges)) return;
                else if (expression != null && expression.range.Contain(position)) expression.Rename(manager, position, ranges);
            }
            public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos)
            {
                if (fileVariable.type.range.Contain(position)) fileVariable.type.Completion(manager, position, infos, fileVariable.defaultVisibility);
                else if (expression != null && expression.range.Contain(position)) expression.Completion(manager, position, infos);
            }
            public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos) => expression?.CollectInlayHint(manager, infos);
            public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
            {
                if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.CamelCase, out var info, out var newName))
                {
                    InfoUtility.AddEdits(info, references, name.ToString(), newName);
                    infos.Add(info);
                }
                InfoUtility.CheckDefaultAccess(this, range, infos);
                if (expression != null && expression.range.Overlap(range)) expression.CollectCodeAction(manager, range, infos);
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
                collector.Add(DetailTokenType.MemberConstructor, name);
                expression?.CollectSemanticToken(manager, collector);
                CollectSemanticToken(manager, collector, fileConstructor.returns, fileConstructor.parameters, logicBlock);
            }
            public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
            {
                if (expression != null && expression.range.Contain(position)) return expression.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
                foreach (var statement in logicBlock.statements)
                    if (statement.range.Contain(position))
                        return statement.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
                return base.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            }
            public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
            {
                if (name.Contain(position))
                {
                    if (manager.TryGetDefineDeclaration(declaration, out var abstractDeclaration) && abstractDeclaration is AbstractClass abstractClass)
                    {
                        InfoUtility.Rename(abstractClass, ranges);
                        foreach (var member in abstractClass.constructors)
                            InfoUtility.Rename(member, ranges);
                    }
                }
                else if (expression != null && expression.range.Contain(position)) expression.Rename(manager, position, ranges);
                else Rename(manager, position, fileConstructor.returns, fileConstructor.parameters, logicBlock, ranges);
            }
            public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos)
            {
                if (fileConstructor.expression != null)
                {
                    if (Lexical.TryAnalysis(fileConstructor.expression, 0, out var lexical, null))
                    {
                        if (lexical.anchor.Contain(position))
                        {
                            infos.Add(new CompletionInfo(KeyWords.THIS, CompletionItemKind.Keyword, "关键字"));
                            infos.Add(new CompletionInfo(KeyWords.BASE, CompletionItemKind.Keyword, "关键字"));
                            return;
                        }
                        else if (expression != null && expression.range.Contain(position))
                        {
                            expression.Completion(manager, position, infos);
                            return;
                        }
                    }
                }
                Completion(manager, position, fileConstructor.defaultVisibility, fileConstructor.returns, fileConstructor.parameters, logicBlock, infos);
            }
            public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos)
            {
                expression?.CollectInlayHint(manager, infos);
                foreach (var statement in logicBlock.statements)
                    statement.CollectInlayHint(manager, infos);
            }
            public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
            {
                InfoUtility.CheckDefaultAccess(this, range, infos);
                if (expression != null && expression.range.Overlap(range)) expression.CollectCodeAction(manager, range, infos);
                CollectLogicBlockCodeAction(manager, range, logicBlock, infos);
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
            public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
            {
                foreach (var statement in logicBlock.statements)
                    if (statement.range.Contain(position))
                        return statement.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
                return base.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            }
            public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
            {
                if (name.Contain(position)) InfoUtility.Rename(this, ranges);
                else Rename(manager, position, fileFunction.returns, fileFunction.parameters, logicBlock, ranges);
            }
            public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos) => Completion(manager, position, fileFunction.defaultVisibility, fileFunction.returns, fileFunction.parameters, logicBlock, infos);
            public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos)
            {
                foreach (var statement in logicBlock.statements)
                    statement.CollectInlayHint(manager, infos);
            }
            public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
            {
                if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
                {
                    InfoUtility.AddEdits(info, references, name.ToString(), newName);
                    var ranges = new List<TextRange>();
                    foreach (var item in implements)
                        ranges.AddRange(item.references);
                    foreach (var item in overrides)
                        ranges.AddRange(item.references);
                    InfoUtility.AddEdits(info, ranges, name.ToString(), newName);
                    infos.Add(info);
                }
                InfoUtility.CheckDefaultAccess(this, range, infos);
                CollectLogicBlockCodeAction(manager, range, logicBlock, infos);
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
                info = new HoverInfo(name, this.CodeInfo(manager, space), true);
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
            collector.Add(DetailTokenType.TypeHandle, name);
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
        public override bool TrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            foreach (var vaiable in variables)
                if (vaiable.file.range.Contain(position))
                    return vaiable.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            foreach (var ctor in constructors)
                if (ctor.file.range.Contain(position))
                    return ctor.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            foreach (var function in functions)
                if (function.file.range.Contain(position))
                    return function.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            foreach (var statement in descontructorLogicBlock.statements)
                if (statement.range.Contain(position))
                    return statement.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
            return base.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex);
        }
        public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (name.Contain(position))
            {
                InfoUtility.Rename(this, ranges);
                foreach (var member in constructors)
                    InfoUtility.Rename(member, ranges);
            }
            else
            {
                for (var i = 0; i < fileClass.inherits.Count; i++)
                    if (fileClass.inherits[i].range.Contain(position))
                    {
                        if (fileClass.inherits.Count == inherits.Count) fileClass.inherits[i].Rename(manager, position, inherits[i], ranges);
                        else if (i > 0) fileClass.inherits[i].Rename(manager, position, inherits[i - 1], ranges);
                        else fileClass.inherits[i].Rename(manager, position, parent, ranges);
                        return;
                    }
                foreach (var member in variables)
                    if (member.file.range.Contain(position))
                    {
                        member.Rename(manager, position, ranges);
                        return;
                    }
                foreach (var member in constructors)
                    if (member.file.range.Contain(position))
                    {
                        member.Rename(manager, position, ranges);
                        return;
                    }
                foreach (var member in functions)
                    if (member.file.range.Contain(position))
                    {
                        member.Rename(manager, position, ranges);
                        return;
                    }
                foreach (var statement in descontructorLogicBlock.statements)
                    if (statement.range.Contain(position))
                    {
                        statement.Rename(manager, position, ranges);
                        return;
                    }
            }
        }
        public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos)
        {
            foreach (var type in fileClass.inherits)
                if (type.range.Contain(position))
                {
                    type.Completion(manager, position, infos);
                    return;
                }
            foreach (var variable in variables)
                if (variable.file.range.Contain(position))
                {
                    variable.Completion(manager, position, infos);
                    return;
                }
            foreach (var ctor in constructors)
                if (ctor.file.range.Contain(position))
                {
                    ctor.Completion(manager, position, infos);
                    return;
                }
            foreach (var function in functions)
                if (function.file.range.Contain(position))
                {
                    function.Completion(manager, position, infos);
                    return;
                }
            foreach (var statement in descontructorLogicBlock.statements)
                if (statement.range.Contain(position))
                {
                    statement.Completion(manager, position, infos);
                    return;
                }
            var context = new Context(fileClass.space.document, space, fileClass.space.relies, this);
            InfoUtility.Completion(manager, context, position.Line, position, infos, true);
            InfoUtility.CollectOverride(manager, this, infos);
        }
        public override void CollectInlayHint(Manager manager, List<InlayHintInfo> infos)
        {
            if (fileClass.inherits.Count > 0)
            {
                if (fileClass.inherits.Count > inherits.Count)
                {
                    infos.Add(new InlayHintInfo("<:", name.end));
                    if (fileClass.inherits.Count > 1)
                    {
                        infos.Add(new InlayHintInfo(":", fileClass.inherits[0].range.end));
                        for (var i = 1; i < fileClass.inherits.Count - 1; i++)
                            infos.Add(new InlayHintInfo(",", fileClass.inherits[i].name.name.end));
                    }
                }
                else
                {
                    infos.Add(new InlayHintInfo(":", name.end));
                    for (var i = 0; i < fileClass.inherits.Count - 1; i++)
                        infos.Add(new InlayHintInfo(",", fileClass.inherits[i].name.name.end));
                }
            }
            foreach (var member in variables)
                member.CollectInlayHint(manager, infos);
            foreach (var memeber in constructors)
                memeber.CollectInlayHint(manager, infos);
            foreach (var member in functions)
                member.CollectInlayHint(manager, infos);
            foreach (var statement in descontructorLogicBlock.statements)
                statement.CollectInlayHint(manager, infos);
        }
        public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
        {
            if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
            {
                InfoUtility.AddEdits(info, references, name.ToString(), newName);
                infos.Add(info);
            }
            InfoUtility.CheckDefaultAccess(this, range, infos);
            var offset = inherits.Count == fileClass.inherits.Count ? 0 : 1;
            var filterAll = new HashSet<AbstractCallable>();
            foreach (var member in functions)
            {
                filterAll.Add(member);
                filterAll.AddRange(member.overrides);
            }
            var filter = new HashSet<AbstractCallable>();
            for (int i = 0; i < inherits.Count; i++)
            {
                var fileType = fileClass.inherits[i + offset];
                if (fileType.range.Overlap(range))
                {
                    var type = inherits[i];
                    if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstractInterface abstractInterface)
                    {
                        var list = new List<AbstractCallable>();
                        foreach (var inherit in manager.GetInheritIterator(abstractInterface))
                            foreach (var function in inherit.functions)
                                if (filter.Add(function))
                                {
                                    filter.AddRange(function.overrides);
                                    if (!filterAll.Contains(function))
                                        list.Add(function);
                                }
                        var endLine = fileClass.range.end.Line;
                        var sb = new StringBuilder();
                        for (var j = 0; j < endLine.indent; j++)
                            sb.Append(' ');
                        if (endLine.line == fileClass.range.start.Line.line)
                            sb.Append("    ");
                        var indent = sb.ToString();
                        if (list.Count > 0)
                        {
                            info = new CodeActionInfo("实现接口", null, []);
                            sb.Clear();
                            foreach (var function in list)
                            {
                                sb.Append('\n');
                                sb.Append(indent);
                                sb.Append(((TextRange)function.name.start.Line).Trim);
                            }
                            info.changes?.Add(endLine.end & endLine.end, sb.ToString());
                            infos.Add(info);
                        }
                    }
                }
                filter.Clear();
            }
            foreach (var member in variables)
                member.CollectCodeAction(manager, range, infos);
            foreach (var member in constructors)
                member.CollectCodeAction(manager, range, infos);
            foreach (var member in functions)
                member.CollectCodeAction(manager, range, infos);
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
            collector.Add(DetailTokenType.TypeDelegate, name);
            CollectSemanticToken(manager, collector, fileDelegate.returns, fileDelegate.parameters, null);
        }
        public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (name.Contain(position)) InfoUtility.Rename(this, ranges);
            else Rename(manager, position, fileDelegate.returns, fileDelegate.parameters, null, ranges);
        }
        public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos) => Completion(manager, position, fileDelegate.defaultVisibility, fileDelegate.returns, fileDelegate.parameters, null, infos);
        public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
        {
            if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
            {
                InfoUtility.AddEdits(info, references, name.ToString(), newName);
                infos.Add(info);
            }
            InfoUtility.CheckDefaultAccess(this, range, infos);
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
                info = new HoverInfo(name, this.CodeInfo(manager, space), true);
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
            collector.Add(DetailTokenType.TypeTask, name);
            for (var i = 0; i < returns.Count; i++)
                collector.AddType(fileTask.returns[i], manager, returns[i]);
        }
        public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (name.Contain(position)) InfoUtility.Rename(this, ranges);
            else for (var i = 0; i < returns.Count; i++)
                    if (fileTask.returns[i].Rename(manager, position, returns[i], ranges)) return;
        }
        public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos)
        {
            foreach (var type in fileTask.returns)
                if (type.range.Contain(position))
                {
                    type.Completion(manager, position, infos);
                    return;
                }
        }
        public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
        {
            if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
            {
                InfoUtility.AddEdits(info, references, name.ToString(), newName);
                infos.Add(info);
            }
            InfoUtility.CheckDefaultAccess(this, range, infos);
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
        public override void Rename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (name.Contain(position)) InfoUtility.Rename(this, ranges);
            else Rename(manager, position, fileNative.returns, fileNative.parameters, null, ranges);
        }
        public override void Completion(Manager manager, TextPosition position, List<CompletionInfo> infos) => Completion(manager, position, fileNative.defaultVisibility, fileNative.returns, fileNative.parameters, null, infos);
        public override void CollectCodeAction(Manager manager, TextRange range, List<CodeActionInfo> infos)
        {
            if (name.Overlap(range) && InfoUtility.CheckNamingRule(name, NamingRule.PascalCase, out var info, out var newName))
            {
                InfoUtility.AddEdits(info, references, name.ToString(), newName);
                infos.Add(info);
            }
            InfoUtility.CheckDefaultAccess(this, range, infos);
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
        public AbstractLibrary Library
        {
            get
            {
                var index = this;
                while (index.parent != null) index = index.parent;
                return (AbstractLibrary)index;
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
