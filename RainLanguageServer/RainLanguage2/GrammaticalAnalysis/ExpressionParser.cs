
using RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis
{
    internal class ExpressionParser(Manager manager, Context context, LocalContext localContext, MessageCollector collector, bool destructor)
    {
        public readonly Manager manager = manager;
        public readonly Context context = context;
        public readonly LocalContext localContext = localContext;
        public readonly MessageCollector collector = collector;
        public readonly bool destructor = destructor;
        public Expression Parse(TextRange range)
        {
            range = range.Trim;
            if (range.Count == 0) return new TupleExpression(range);
            if (TryParseBracket(range, out var result)) return result;
            if (TryParseTuple(SplitFlag.Semicolon, LexicalType.Semicolon, range, out result)) return result;
            var lexical = ExpressionSplit.Split(range, SplitFlag.Lambda | SplitFlag.Assignment | SplitFlag.Question, out var left, out var right, collector);
            if (lexical.type == LexicalType.Lambda) return ParseLambda(left, lexical.anchor, right);
            else if (lexical.type == LexicalType.Question) return ParseQuestion(left, lexical.anchor, right);
            else if (lexical.type != LexicalType.Unknow) return ParseAssignment(left, lexical, right);
            if (TryParseTuple(SplitFlag.Comma, LexicalType.Comma, range, out result)) return result;
            lexical = ExpressionSplit.Split(range, SplitFlag.QuestionNull, out left, out right, collector);
            if (lexical.type == LexicalType.QuestionNull) return ParseQuestionNull(left, lexical.anchor, right);
            return ParseExpression(range);
        }
        private Expression ParseExpression(TextRange range)
        {

        }
        private Expression ParseQuestionNull(TextRange left, TextRange symbol, TextRange right)
        {
            var leftExpression = Parse(left);
            var rightExpression = Parse(right);
            if (!leftExpression.attribute.ContainAll(ExpressionAttribute.Value))
            {
                collector.Add(left, ErrorLevel.Error, "表达式不是个值");
                leftExpression = leftExpression.ToInvalid();
            }
            else if (leftExpression.tuple[0] != manager.kernelManager.ENTITY && !leftExpression.tuple[0].Managed)
            {
                collector.Add(left, ErrorLevel.Error, "不是可以为空的类型");
                rightExpression = rightExpression.ToInvalid();
            }
            rightExpression = AssignmentConvert(rightExpression, leftExpression.tuple);
            return new QuestionNullExpression(symbol, leftExpression, rightExpression);
        }
        private Expression ParseAssignment(TextRange left, Lexical symbol, TextRange right)
        {

        }
        private Expression ParseQuestion(TextRange condition, TextRange symbol, TextRange value)
        {
            condition = condition.Trim;
            var conditionExpression = Parse(condition);
            if (!conditionExpression.attribute.ContainAny(ExpressionAttribute.Value))
            {
                collector.Add(condition, ErrorLevel.Error, "不是个值");
                conditionExpression = conditionExpression.ToInvalid();
            }
            if (ExpressionSplit.Split(value, SplitFlag.Colon, out var left, out var right, collector).type != LexicalType.Unknow)
                return new QuestionExpression(condition & value, conditionExpression, Parse(left), Parse(right));
            else
                return new QuestionExpression(condition & value, conditionExpression, Parse(value), null);
        }
        private Expression ParseLambda(TextRange parameters, TextRange symbol, TextRange body)
        {
            parameters = parameters.Trim;
            while (ExpressionSplit.Split(parameters, SplitFlag.Bracket0, out var left, out var right, collector).type == LexicalType.BracketRight0 && left.start == parameters.start && right.end == parameters.end)
                parameters = (left.end & right.start).Trim;
            var list = new List<TextRange>();
            while (ExpressionSplit.Split(parameters, SplitFlag.Comma | SplitFlag.Semicolon, out var left, out var right, collector).type != LexicalType.Unknow)
            {
                if (TryParseLambdaParameter(left, out left)) list.Add(left);
                parameters = right.Trim;
            }
            if (TryParseLambdaParameter(parameters, out parameters)) list.Add(parameters);
            return new BlurryLambdaExpression(parameters & body, list, symbol, body);
        }
        private bool TryParseLambdaParameter(TextRange range, out TextRange parameter)
        {
            if (Lexical.TryAnalysis(range, 0, out var lexical, collector))
            {
                parameter = lexical.anchor;
                if (lexical.type == LexicalType.Word)
                {
                    if (Lexical.TryAnalysis(range, lexical.anchor.end, out lexical, collector))
                        collector.Add(lexical.anchor, ErrorLevel.Error, "意外的词条");
                }
                else collector.Add(range, ErrorLevel.Error, "无效的词条");
                return true;
            }
            parameter = default;
            return false;
        }
        private bool TryParseTuple(SplitFlag flag, LexicalType type, TextRange range, [MaybeNullWhen(false)] out Expression expression)
        {
            if (ExpressionSplit.Split(range, flag, out var left, out var right, collector).type == type)
            {
                TextRange remainder;
                var expressions = new List<Expression>();
                do
                {
                    remainder = right;
                    expressions.Add(Parse(left));
                }
                while (ExpressionSplit.Split(remainder, flag, out left, out right, collector).type == type);
                if (remainder.Valid) expressions.Add(Parse(remainder));
                expression = TupleExpression.Create(expressions, collector);
                return true;
            }
            expression = default;
            return false;
        }
        private bool TryParseBracket(TextRange range, [MaybeNullWhen(false)] out Expression expression)
        {
            expression = default;
            if (range.Count == 0) return false;
            var lexical = ExpressionSplit.Split(range, SplitFlag.Bracket0, out var left, out var right, collector);
            if (lexical.type == LexicalType.BracketRight0 && left.start == range.start && left.end == range.end)
            {
                expression = new BracketExpression(left, right, Parse(left.end & right.start));
                return true;
            }
            return false;
        }
    }
}
