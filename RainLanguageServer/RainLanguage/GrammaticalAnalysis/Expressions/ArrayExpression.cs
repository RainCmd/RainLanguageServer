namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis.Expressions
{
    internal class ArrayCreateExpression : Expression
    {
        public readonly TypeExpression type;
        public readonly BracketExpression length;
        public override bool Valid => true;
        public ArrayCreateExpression(TextRange range, Type arrayType, LocalContextSnapshoot snapshoot, TypeExpression type, BracketExpression length) : base(range, arrayType, snapshoot)
        {
            this.type = type;
            this.length = length;
            attribute = ExpressionAttribute.Value | ExpressionAttribute.Array;
        }
        public override void Read(ExpressionParameter parameter)
        {
            type.Read(parameter);
            length.Read(parameter);
        }

        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (type.range.Contain(position)) return type.Operator(position, action);
            if (length.range.Contain(position)) return length.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (type.range.Contain(position)) return type.BreadthFirstOperator(position, action);
            if (length.range.Contain(position)) return length.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            type.Operator(action);
            length.Operator(action);
            action(this);
        }
    }
    internal class ArrayInitExpression : Expression
    {
        public readonly TypeExpression? type;
        public readonly BracketExpression elements;
        public override bool Valid => true;
        public ArrayInitExpression(TextRange range, Type arrayType, LocalContextSnapshoot snapshoot, TypeExpression? type, BracketExpression elements) : base(range, arrayType, snapshoot)
        {
            this.type = type;
            this.elements = elements;
            attribute = ExpressionAttribute.Value | ExpressionAttribute.Array;
        }
        public override void Read(ExpressionParameter parameter)
        {
            type?.Read(parameter);
            elements.Read(parameter);
        }

        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (type != null && type.range.Contain(position)) return type.Operator(position, action);
            if (elements.range.Contain(position)) return elements.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (type != null && type.range.Contain(position)) return type.BreadthFirstOperator(position, action);
            if (elements.range.Contain(position)) return elements.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            type?.Operator(action);
            elements.Operator(action);
            action(this);
        }
    }
    internal class ArrayEvaluationExpression : Expression
    {
        public readonly Expression array;
        public readonly BracketExpression index;
        public override bool Valid => true;
        public ArrayEvaluationExpression(TextRange range, LocalContextSnapshoot snapshoot, Expression array, BracketExpression index, ExpressionAttribute attribute, Manager.KernelManager manager) : base(range, new Type(array.tuple[0], array.tuple[0].dimension - 1), snapshoot)
        {
            this.array = array;
            this.index = index;
            this.attribute = attribute | tuple[0].GetAttribute(manager);
        }
        public override void Read(ExpressionParameter parameter)
        {
            array.Read(parameter);
            index.Read(parameter);
        }
        public override void Write(ExpressionParameter parameter)
        {
            array.Read(parameter);
            index.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (array.range.Contain(position)) return array.Operator(position, action);
            if (index.range.Contain(position)) return index.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (array.range.Contain(position)) return array.BreadthFirstOperator(position, action);
            if (index.range.Contain(position)) return index.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            array.Operator(action);
            index.Operator(action);
            action(this);
        }
    }
    internal class StringEvaluationExpression : Expression
    {
        public readonly Expression source;
        public readonly BracketExpression index;
        public override bool Valid => true;
        public StringEvaluationExpression(TextRange range, LocalContextSnapshoot snapshoot, Expression source, BracketExpression index, Manager.KernelManager manager) : base(range, manager.CHAR, snapshoot)
        {
            this.source = source;
            this.index = index;
            attribute = ExpressionAttribute.Value | tuple[0].GetAttribute(manager);
        }
        public override void Read(ExpressionParameter parameter)
        {
            source.Read(parameter);
            index.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (source.range.Contain(position)) return source.Operator(position, action);
            if (index.range.Contain(position)) return index.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (source.range.Contain(position)) return source.BreadthFirstOperator(position, action);
            if (index.range.Contain(position)) return index.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            source.Operator(action);
            index.Operator(action);
            action(this);
        }
    }
    internal class ArraySubExpression : Expression
    {
        public readonly Expression source;
        public readonly BracketExpression indies;
        public override bool Valid => true;
        public ArraySubExpression(TextRange range, LocalContextSnapshoot snapshoot, Expression source, BracketExpression indies, Manager.KernelManager manager) : base(range, source.tuple, snapshoot)
        {
            this.source = source;
            this.indies = indies;
            attribute = ExpressionAttribute.Value | ExpressionAttribute.Array;
        }
        public override void Read(ExpressionParameter parameter)
        {
            source.Read(parameter);
            indies.Read(parameter);
        }
        public override bool Operator(TextPosition position, ExpressionOperator action)
        {
            if (source.range.Contain(position)) return source.Operator(position, action);
            if (indies.range.Contain(position)) return indies.Operator(position, action);
            return action(this);
        }
        public override bool BreadthFirstOperator(TextPosition position, ExpressionOperator action)
        {
            if (action(this)) return true;
            if (source.range.Contain(position)) return source.BreadthFirstOperator(position, action);
            if (indies.range.Contain(position)) return indies.BreadthFirstOperator(position, action);
            return false;
        }
        public override void Operator(Action<Expression> action)
        {
            source.Operator(action);
            indies.Operator(action);
            action(this);
        }
    }
}
