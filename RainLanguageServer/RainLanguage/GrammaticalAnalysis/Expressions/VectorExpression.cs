using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class VectorMemberExpression : Expression
    {
        public readonly Expression target;
        public readonly TextRange symbol;
        public readonly TextRange member;
        public override bool Valid => true;
        public VectorMemberExpression(TextRange range, Type type, Expression target, TextRange symbol, TextRange member) : base(range, type)
        {
            this.target = target;
            this.symbol = symbol;
            this.member = member;
            attribute = ExpressionAttribute.Value | (target.attribute & ExpressionAttribute.Assignable);
        }
        public override void Read(ExpressionParameter parameter) => target.Read(parameter);
        public override void Write(ExpressionParameter parameter) => target.Write(parameter);
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (target.range.Contain(position)) return target.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (target.range.Contain(position)) return target.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            target.Operator(action);
            action(this);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (target.range.Contain(position)) return target.OnHover(manager, position, out info);
            if (member.Contain(position))
            {
                info = new HoverInfo(member, tuple[0].CodeInfo(manager, ManagerOperator.GetSpace(manager, position)), true);
                return true;
            }
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (target.range.Contain(position)) return target.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (target.range.Contain(position)) return target.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (target.range.Contain(position)) return target.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            target.CollectSemanticToken(manager, collector);
            collector.Add(DetailTokenType.Operator, symbol);
            collector.Add(DetailTokenType.MemberField, member);
        }
    }
    internal class VectorConstructorExpression : Expression
    {
        public readonly TypeExpression type;
        public readonly BracketExpression parameters;
        public override bool Valid => true;
        public VectorConstructorExpression(TextRange range, TypeExpression type, BracketExpression parameters) : base(range, type.type)
        {
            this.type = type;
            this.parameters = parameters;
            attribute = ExpressionAttribute.Value;
        }
        public override void Read(ExpressionParameter parameter)
        {
            type.Read(parameter);
            parameters.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (type.range.Contain(position)) return type.Operator(position, action);
            if (parameters.range.Contain(position)) return parameters.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (type.range.Contain(position)) return type.BreadthFirstOperator(position, action);
            if (parameters.range.Contain(position)) return parameters.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            type.Operator(action);
            parameters.Operator(action);
            action(this);
        }

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (type.range.Contain(position)) return type.OnHover(manager, position, out info);
            if (parameters.range.Contain(position)) return parameters.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (type.range.Contain(position)) return type.OnHighlight(manager, position, infos);
            if (parameters.range.Contain(position)) return parameters.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (type.range.Contain(position)) return type.TryGetDefinition(manager, position, out definition);
            if (parameters.range.Contain(position)) return parameters.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (type.range.Contain(position)) return type.FindReferences(manager, position, references);
            if (parameters.range.Contain(position)) return parameters.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            type.CollectSemanticToken(manager, collector);
            parameters.CollectSemanticToken(manager, collector);
        }

        protected override bool InternalTrySignatureHelp(Manager manager, TextPosition position, [MaybeNullWhen(false)] out List<SignatureInfo> infos, out int functionIndex, out int parameterIndex)
        {
            if (parameters.range.Contain(position))
            {
                if (parameters.TrySignatureHelp(manager, position, out infos, out functionIndex, out parameterIndex)) return true;
                if (manager.TryGetDeclaration(type.type, out var declaration) && declaration is AbstractStruct abstractStruct)
                {
                    infos = InfoUtility.GetStructConstructorSignatureInfos(manager, abstractStruct, ManagerOperator.GetSpace(manager, position));
                    if (parameters.tuple.Count > 0 && infos.Count > 1)
                    {
                        functionIndex = 1;
                        parameterIndex = parameters.GetTupleIndex(position);
                    }
                    else
                    {
                        functionIndex = 0;
                        parameterIndex = 0;
                    }
                    return true;
                }
            }
            infos = default;
            functionIndex = 0;
            parameterIndex = 0;
            return false;
        }
    }
}
