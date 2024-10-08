﻿namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal abstract class DelegateCreateExpression : Expression
    {
        public readonly AbstractCallable callable;
        public override bool Valid => true;
        public DelegateCreateExpression(TextRange range, Type type, LocalContextSnapshoot snapshoot, AbstractCallable callable, Manager.KernelManager manager) : base(range, type, snapshoot)
        {
            this.callable = callable;
            attribute = ExpressionAttribute.Value | type.GetAttribute(manager);
        }
    }
    internal class FunctionDelegateCreateExpression(TextRange range, TextRange? qualifier, QualifiedName name, Type type, LocalContextSnapshoot snapshoot, AbstractCallable callable, Manager.KernelManager manager) : DelegateCreateExpression(range, type, snapshoot, callable, manager)
    {
        public readonly TextRange? qualifier = qualifier;
        public readonly QualifiedName name = name;
        public override void Read(ExpressionParameter parameter) => callable.references.Add(name.name);
        public override bool Operator(TextPosition position, ExpressionOperator action) => action(this);
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action) => action(this);
        public override void Operator(Action<Expression> action) => action(this);

        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (InfoUtility.OnHover(name.qualify, position, out info)) return true;
            if (name.name.Contain(position))
            {
                info = new HoverInfo(name.name, callable.Info(manager, null, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            info = default;
            return false;
        }

        protected override bool InternalOnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (InfoUtility.OnHighlight(name.qualify, position, callable.space, infos)) return true;
            if (name.name.Contain(position))
            {
                InfoUtility.Highlight(callable, infos);
                return true;
            }
            return false;
        }

        protected override bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (name.name.Contain(position))
            {
                definition = callable.name;
                return true;
            }
            definition = default;
            return false;
        }

        protected override bool InternalFindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (InfoUtility.FindReferences(name.qualify, position, callable.space, references)) return true;
            if (name.name.Contain(position))
            {
                references.AddRange(callable.references);
                return true;
            }
            return false;
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            if (qualifier != null) collector.Add(DetailTokenType.KeywordCtrl, qualifier.Value);
            collector.AddNamespace(name);
            collector.Add(DetailTokenType.GlobalFunction, name.name);
        }

        protected override void InternalRename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (name.name.Contain(position)) InfoUtility.Rename(callable, ranges);
            else InfoUtility.Rename(name.qualify, position, ManagerOperator.GetSpace(manager, position), ranges);
        }
    }
    internal class MemberFunctionDelegateCreateExpression(TextRange range, Type type, LocalContextSnapshoot snapshoot, AbstractCallable callable, Manager.KernelManager manager, Expression? target, TextRange? symbol, TextRange member) : DelegateCreateExpression(range, type, snapshoot, callable, manager)
    {
        public readonly Expression? target = target;
        public readonly TextRange? symbol = symbol;
        public readonly TextRange member = member;

        public override void Read(ExpressionParameter parameter)
        {
            target?.Read(parameter);
            callable.references.Add(member);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (target != null && target.range.Contain(position)) return target.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (target != null && target.range.Contain(position)) return target.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            target?.Operator(action);
            action(this);
        }

        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (member.Contain(position))
            {
                manager.TryGetDefineDeclaration(callable.declaration, out var abstractDeclaration);
                info = new HoverInfo(member, callable.Info(manager, abstractDeclaration, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            info = default;
            return false;
        }

        protected override bool InternalOnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (member.Contain(position))
            {
                InfoUtility.Highlight(callable, infos);
                return true;
            }
            return false;
        }

        protected override bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (member.Contain(position))
            {
                definition = callable.name;
                return true;
            }
            definition = default;
            return false;
        }

        protected override bool InternalFindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (member.Contain(position))
            {
                references.AddRange(callable.references);
                return true;
            }
            return false;
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            if (symbol != null) collector.Add(DetailTokenType.Operator, symbol.Value);
            collector.Add(DetailTokenType.MemberFunction, member);
        }

        protected override void InternalRename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (member.Contain(position)) InfoUtility.Rename(callable, ranges);
        }
    }
    internal class VirtualFunctionDelegateCreateExpression(TextRange range, Type type, LocalContextSnapshoot snapshoot, AbstractCallable callable, Manager.KernelManager manager, Expression? target, TextRange? symbol, TextRange member) : DelegateCreateExpression(range, type, snapshoot, callable, manager)
    {
        public readonly Expression? target = target;
        public readonly TextRange? symbol = symbol;
        public readonly TextRange member = member;

        public override void Read(ExpressionParameter parameter)
        {
            target?.Read(parameter);
            if (callable is AbstractClass.Function function)
            {
                function.references.Add(member);
                foreach (var item in function.implements)
                    item.references.Add(member);
            }
            else callable.references.Add(member);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (target != null && target.range.Contain(position)) return target.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (target != null && target.range.Contain(position)) return target.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            target?.Operator(action);
            action(this);
        }

        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (member.Contain(position))
            {
                manager.TryGetDefineDeclaration(callable.declaration, out var abstractDeclaration);
                info = new HoverInfo(member, callable.Info(manager, abstractDeclaration, ManagerOperator.GetSpace(manager, position)).MakedownCode(), true);
                return true;
            }
            info = default;
            return false;
        }

        protected override bool InternalOnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (member.Contain(position))
            {
                InfoUtility.Highlight(callable, infos);
                if (callable is AbstractClass.Function function)
                    foreach (var item in function.overrides)
                        InfoUtility.Highlight(item, infos);
                return true;
            }
            return false;
        }

        protected override bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (member.Contain(position))
            {
                definition = callable.name;
                return true;
            }
            definition = default;
            return false;
        }

        protected override bool InternalFindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (member.Contain(position))
            {
                references.AddRange(callable.references);
                if (callable is AbstractClass.Function function)
                    foreach (var item in function.overrides)
                        references.AddRange(item.references);
                return true;
            }
            return false;
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            if (symbol != null) collector.Add(DetailTokenType.Operator, symbol.Value);
            collector.Add(DetailTokenType.MemberFunction, member);
        }

        protected override void InternalRename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            if (member.Contain(position)) InfoUtility.Rename(callable, ranges);
        }
    }
    internal class LambdaDelegateCreateExpression(TextRange range, Type type, LocalContextSnapshoot snapshoot, AbstractCallable callable, Manager.KernelManager manager, List<Local> parmeters, TextRange symbol, Expression body) : DelegateCreateExpression(range, type, snapshoot, callable, manager)
    {
        public readonly List<Local> parmeters = parmeters;
        public readonly TextRange symbol = symbol;
        public readonly Expression body = body;

        public override void Read(ExpressionParameter parameter) => body.Read(parameter);
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (body.range.Contain(position)) return body.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (body.range.Contain(position)) return body.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            body.Operator(action);
            action(this);
        }

        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            foreach (var local in parmeters)
                if (local.range.Contain(position))
                {
                    info = local.Hover(manager, position);
                    return true;
                }
            if (symbol.Contain(position))
            {
                info = new HoverInfo(symbol, tuple[0].CodeInfo(manager, ManagerOperator.GetSpace(manager, position)), true);
                return true;
            }
            info = default;
            return false;
        }

        protected override bool InternalOnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            foreach (var local in parmeters)
                if (local.range.Contain(position))
                {
                    local.OnHighlight(infos);
                    return true;
                }
            return false;
        }

        protected override bool InternalTryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            foreach (var local in parmeters)
                if (local.range.Contain(position))
                {
                    definition = local.range;
                    return true;
                }
            definition = default;
            return false;
        }

        protected override bool InternalFindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            foreach (var local in parmeters)
                if (local.range.Contain(position))
                {
                    local.FindReferences(references);
                    return true;
                }
            return false;
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            foreach (var local in parmeters)
                collector.Add(DetailTokenType.Local, local.range);
            collector.Add(DetailTokenType.Operator, symbol);
        }

        protected override void InternalRename(Manager manager, TextPosition position, HashSet<TextRange> ranges)
        {
            foreach (var local in parmeters)
                if (local.range.Contain(position))
                {
                    local.Rename(ranges);
                    return;
                }
        }

        protected override void InternalCollectInlayHint(Manager manager, List<InlayHintInfo> infos)
        {
            foreach (var local in parmeters)
                infos.Add(new InlayHintInfo($"{local.type.Info(manager, ManagerOperator.GetSpace(manager, local.range.start))} ", local.range.start, InlayHintInfo.Kind.Type));
        }
    }
}
