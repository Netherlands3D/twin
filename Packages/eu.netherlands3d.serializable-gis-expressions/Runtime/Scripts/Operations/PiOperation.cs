using System;

namespace Netherlands3D.SerializableGisExpressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>pi</c> expression operator, which returns
    /// the mathematical constant π.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#pi">
    ///   Mapbox “pi” expression reference
    /// </seealso>
    internal static class PiOperation
    {
        /// <summary>The Mapbox operator code for “pi”.</summary>
        public const string Code = "pi";

        /// <summary>
        /// Evaluates the <c>pi</c> expression by returning the constant π.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> which must have no operands.</param>
        /// <returns>The constant π as a <see cref="double"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if any operands are provided.</exception>
        public static double Evaluate(Expression expression)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 0);
            
            return Math.PI;
        }
    }
}