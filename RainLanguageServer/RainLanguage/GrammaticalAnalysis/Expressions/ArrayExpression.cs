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

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (type.range.Contain(position)) return type.OnHover(manager, position, out info);
            if (length.range.Contain(position)) return length.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (type.range.Contain(position)) return type.OnHighlight(manager, position, infos);
            if (length.range.Contain(position)) return length.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (type.range.Contain(position)) return type.TryGetDefinition(manager, position, out definition);
            if (length.range.Contain(position)) return length.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (type.range.Contain(position)) return type.FindReferences(manager, position, references);
            if (length.range.Contain(position)) return length.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            type.CollectSemanticToken(manager, collector);
            length.CollectSemanticToken(manager, collector);
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

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (type != null && type.range.Contain(position)) return type.OnHover(manager, position, out info);
            if (elements.range.Contain(position)) return elements.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (type != null && type.range.Contain(position)) return type.OnHighlight(manager, position, infos);
            if (elements.range.Contain(position)) return elements.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (type != null && type.range.Contain(position)) return type.TryGetDefinition(manager, position, out definition);
            if (elements.range.Contain(position)) return elements.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (type != null && type.range.Contain(position)) return type.FindReferences(manager, position, references);
            if (elements.range.Contain(position)) return elements.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            type?.CollectSemanticToken(manager, collector);
            elements.CollectSemanticToken(manager, collector);
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

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (array.range.Contain(position)) return array.OnHover(manager, position, out info);
            if (index.range.Contain(position)) return index.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (array.range.Contain(position)) return array.OnHighlight(manager, position, infos);
            if (index.range.Contain(position)) return index.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (array.range.Contain(position)) return array.TryGetDefinition(manager, position, out definition);
            if (index.range.Contain(position)) return index.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (array.range.Contain(position)) return array.FindReferences(manager, position, references);
            if (index.range.Contain(position)) return index.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            array.CollectSemanticToken(manager, collector);
            index.CollectSemanticToken(manager, collector);
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

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (source.range.Contain(position)) return source.OnHover(manager, position, out info);
            if (index.range.Contain(position)) return index.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (source.range.Contain(position)) return source.OnHighlight(manager, position, infos);
            if (index.range.Contain(position)) return index.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (source.range.Contain(position)) return source.TryGetDefinition(manager, position, out definition);
            if (index.range.Contain(position)) return index.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (source.range.Contain(position)) return source.FindReferences(manager, position, references);
            if (index.range.Contain(position)) return index.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            source.CollectSemanticToken(manager, collector);
            index.CollectSemanticToken(manager, collector);
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

        public override bool OnHover(Manager manager, TextPosition position, out HoverInfo info)
        {
            if (source.range.Contain(position)) return source.OnHover(manager, position, out info);
            if (indies.range.Contain(position)) return indies.OnHover(manager, position, out info);
            info = default;
            return false;
        }

        public override bool OnHighlight(Manager manager, TextPosition position, List<HighlightInfo> infos)
        {
            if (source.range.Contain(position)) return source.OnHighlight(manager, position, infos);
            if (indies.range.Contain(position)) return indies.OnHighlight(manager, position, infos);
            return false;
        }

        public override bool TryGetDefinition(Manager manager, TextPosition position, out TextRange definition)
        {
            if (source.range.Contain(position)) return source.TryGetDefinition(manager, position, out definition);
            if (indies.range.Contain(position)) return indies.TryGetDefinition(manager, position, out definition);
            definition = default;
            return false;
        }

        public override bool FindReferences(Manager manager, TextPosition position, List<TextRange> references)
        {
            if (source.range.Contain(position)) return source.FindReferences(manager, position, references);
            if (indies.range.Contain(position)) return indies.FindReferences(manager, position, references);
            return false;
        }

        public override void CollectSemanticToken(Manager manager, SemanticTokenCollector collector)
        {
            source.CollectSemanticToken(manager, collector);
            indies.CollectSemanticToken(manager, collector);
        }
    }
}
