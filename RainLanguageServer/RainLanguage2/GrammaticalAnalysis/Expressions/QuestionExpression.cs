using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class QuestionExpression : Expression
    {
        public readonly Expression condition;
        public readonly Expression left;
        public readonly Expression? right;
        public override bool Valid => left.Valid;

        public QuestionExpression(TextRange range, Expression condition, Expression left, Expression? right) : base(range, left.tuple)
        {
            this.condition = condition;
            this.left = left;
            this.right = right;
            attribute = left.attribute & ~ExpressionAttribute.Assignable;
        }
    }
}
