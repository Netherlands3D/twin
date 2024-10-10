namespace Netherlands3D.LayerStyles
{
    public abstract class Expression
    {
        /// <summary>
        /// Resolve the expression by performing any action necessary, optionally including information from
        /// context that can be passed to more complex expressions.
        /// </summary>
        /// <param name="context">An object containing information on the current visualisation, dataLayer and/or feature</param>
        /// <returns></returns>
        public abstract object Resolve(ExpressionContext context);
    }
}