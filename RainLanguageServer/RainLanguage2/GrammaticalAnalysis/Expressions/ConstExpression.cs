using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainLanguageServer.RainLanguage2.GrammaticalAnalysis.Expressions
{
    internal class ConstExpression : Expression
    {
        public override bool Valid => true;
        public ConstExpression(TextRange range, Tuple tuple) : base(range, tuple)
        {
            attribute = ExpressionAttribute.Constant;
        }
    }
    internal class ConstNullExpression(TextRange range) : ConstExpression(range, NULL) { }
    internal class ConstHandleNullExpression(TextRange range, Type type) : ConstExpression(range, type) { }
    internal class ConstEntityNullExpression(TextRange range, Manager.KernelManager manager) : ConstExpression(range, manager.ENTITY) { }
}
