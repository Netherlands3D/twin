using System;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>e</c> expression operator, which returns
    /// the mathematical constant e (the base of the natural logarithm).
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#e">
    ///   Mapbox “e” expression reference
    /// </seealso>
    public static class EOperation
    {
        /// <summary>The Mapbox operator string for “e”.</summary>
        public const string Code = "e";

        /// <summary>
        /// Evaluates the <c>e</c> expression, returning the constant e.
        /// </summary>
        /// <returns>The constant <c>e</c> (~2.71828), as a <see cref="double"/>.</returns>
        public static double Evaluate()
        {
            return Math.E;
        }
    }
}