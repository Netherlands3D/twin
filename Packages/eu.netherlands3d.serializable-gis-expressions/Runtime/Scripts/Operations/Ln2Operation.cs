using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>ln2</c> expression operator, which returns
    /// the constant natural logarithm of 2.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#ln2">
    ///   Mapbox “ln2” expression reference
    /// </seealso>
    public static class Ln2Operation
    {
        /// <summary>The Mapbox operator string for “ln2”.</summary>
        public const string Code = "ln2";

        /// <summary>
        /// Evaluates the <c>ln2</c> expression by returning the constant <c>Math.Log(2.0)</c>.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> (must have zero operands).</param>
        /// <returns>The constant <c>Math.Log(2.0)</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the expression has any operands.</exception>
        public static double Evaluate(Expression expression)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 0);

            return Math.Log(2.0);
        }
    }
}