namespace Netherlands3D.LayerStyles
{
    /// <summary>
    /// https://docs.ogc.org/DRAFTS/18-067r4.html#_expressions
    /// </summary>
    public abstract class Expression
    {
        /// <summary>
        /// Resolve the expression by performing any action necessary, optionally including information from
        /// context that can be passed to more complex expressions.
        ///
        /// This is an implementation of the Interpreter design pattern (https://sourcemaking.com/design_patterns/interpreter)
        /// where the method that 'does the work' is called "Resolve" because in common (developer) language you say
        /// that you "resolve" an expression.
        /// </summary>
        /// <param name="context">An object containing information on the current visualisation, dataLayer and/or feature</param>
        public abstract object Resolve(ExpressionContext context);
    }
}