using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class VectorMemberExpression : Expression
    {
        public readonly Expression target;
        public readonly TextRange symbol;
        public readonly TextRange member;
        public override bool Valid => true;
        public VectorMemberExpression(TextRange range, Type type, LocalContextSnapshoot snapshoot, Expression target, TextRange symbol, TextRange member) : base(range, type, snapshoot)
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

        protected override bool InternalOnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (member.Contain(position))
            {
                info = new HoverInfo(member, tuple[0].CodeInfo(manager, ManagerOperator.GetSpace(manager, position)), true);
                return true;
            }
            info = default;
            return false;
        }

        protected override void InternalCollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            collector.Add(DetailTokenType.Operator, symbol);
            collector.Add(DetailTokenType.MemberField, member);
        }
    }
    internal class VectorConstructorExpression : Expression
    {
        public readonly TypeExpression type;
        public readonly BracketExpression parameters;
        public override bool Valid => true;
        public VectorConstructorExpression(TextRange range, TypeExpression type, LocalContextSnapshoot snapshoot, BracketExpression parameters) : base(range, type.type, snapshoot)
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
