using System.Collections.Generic;
using System.Linq;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles
{
    /// <summary>
    /// The expression context provides a list of expressions -serving as variables- to evaluate another expression
    /// against.
    ///
    /// This object can be passed to one or more expressions as a source of variables. By storing each variable as an
    /// expression, it is even possible to build complicated computed properties or just simple literal expressions
    /// in exactly the same way - hidden from the casual developer.
    ///
    /// In this class there are convenience methods - specifically "Add" methods in various varieties - that will
    /// make it easier for the developer using this to ignore the fact each item is an expression. 
    /// </summary>
    public class ExpressionContext : Dictionary<string, Expression>
    {
        public void Add(string key, string value)
        {
            Add(key, new TextExpression(value));
        }

        public void Add(string key, IEnumerable<string> values)
        {
            var stringsAsLiterals = values.Select(s => new TextExpression(s));

            Add(key, new ArrayLiteralExpression(stringsAsLiterals));
        }

        public void Add(string key, bool value)
        {
            Add(key, new BoolExpression(value));
        }
    }
}