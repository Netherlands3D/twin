namespace Netherlands3D.LayerStyles.Expressions
{
    /// <summary>
    /// Serves as the evaluation context (or “blackboard”) for expression evaluation,
    /// providing variables and data that selector expressions can reference.
    /// <para>
    /// By isolating all contextual data in this class, we gain forward compatibility:
    /// when new metadata is needed (for example, to support timelines and expose
    /// a <c>datetime</c> property), it can be added here without changing the
    /// signatures of <c>Resolve</c> or other evaluator methods.
    /// </para>
    /// </summary>
    public class ExpressionContext
    {
        /// <summary>
        /// Gets the feature whose attributes are used as variables in expression evaluation.
        /// Expressions can read any property of this <see cref="Feature"/> to decide
        /// whether a selector matches or to compute style values.
        /// </summary>
        public IFeatureForExpression Feature { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionContext"/> class
        /// for the specified <paramref name="feature"/>.
        /// <para>
        /// Keeping the feature and any future context data here—for example, a timestamp,
        /// user settings, or other runtime values—provides clarity of purpose (analogous
        /// to Unity’s behavior blackboard) and allows us to extend what expressions can
        /// access without breaking existing APIs.
        /// </para>
        /// </summary>
        /// <param name="feature">The <see cref="Feature"/> whose properties
        /// will be available to selector expressions.</param>
        public ExpressionContext(IFeatureForExpression feature)
        {
            this.Feature = feature;
        }
    }
}