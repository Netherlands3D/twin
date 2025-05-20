using Netherlands3D.Twin.Layers;
using System.Collections.Generic;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles
{
    /// <summary>
    /// Resolves and applies styling rules (selectors → symbolizers) for a given <see cref="LayerFeature"/>,
    /// merging all matching <see cref="LayerStyle"/> rules much like a CSS style resolver.
    /// </summary>
    public class StyleResolver
    {
        /// <summary>
        /// Gets the singleton instance of the <see cref="StyleResolver"/>.
        /// </summary>
        public static StyleResolver Instance
        {
            get
            {
                if (instance == null) instance = new StyleResolver();

                return instance;
            } 
        }

        private static StyleResolver instance;
        private readonly ExpressionEvaluator expressionEvaluator = new();

        /// <summary>
        /// Evaluates all style rules in <paramref name="styles"/> against the given <paramref name="feature"/>,
        /// and returns a new <see cref="Symbolizer"/> containing the merged styling for that feature.
        /// </summary>
        /// <param name="feature">The feature whose attributes are tested against style selectors.</param>
        /// <param name="styles">A dictionary of style sheets (by name) containing styling rules.</param>
        /// <returns>
        /// A <see cref="Symbolizer"/> representing the combined styling of all matching rules.
        /// </returns>
        public Symbolizer GetStyling(LayerFeature feature, Dictionary<string, LayerStyle> styles)
        {
            var symbolizer = new Symbolizer();
            var context = new ExpressionContext(feature);
            
            CopyStylingInto(symbolizer, styles, context);
            
            return symbolizer;
        }

        /// <summary>
        /// Iterates over each <see cref="LayerStyle"/> in <paramref name="styles"/> and copies its
        /// styling rules into the provided <paramref name="symbolizer"/>, based on the given <paramref name="context"/>.
        /// </summary>
        /// <param name="symbolizer">The target <see cref="Symbolizer"/> to merge matching rules into.</param>
        /// <param name="styles">A collection of <see cref="LayerStyle"/> objects whose rules will be applied.</param>
        /// <param name="context">An <see cref="ExpressionContext"/> for evaluating selectors against a context.</param>
        private void CopyStylingInto(Symbolizer symbolizer, Dictionary<string, LayerStyle> styles, ExpressionContext context)
        {
            foreach (var style in styles.Values)
            {
                CopyStylingInto(symbolizer, style, context);
            }
        }

        /// <summary>
        /// Copies all matching rules from a single <paramref name="style"/> into the <paramref name="symbolizer"/>.
        /// </summary>
        /// <param name="symbolizer">The symbolizer to merge matching rule symbolizers into.</param>
        /// <param name="style">A <see cref="LayerStyle"/> containing one or more styling rules.</param>
        /// <param name="context">An <see cref="ExpressionContext"/> for evaluating selectors against a context.</param>
        private void CopyStylingInto(Symbolizer symbolizer, LayerStyle style, ExpressionContext context)
        {
            foreach (var rule in style.StylingRules.Values)
            {
                // if the rule's selector does not match the given attributes - then this symbology does not apply
                if (!Resolve(rule.Selector, context)) continue;

                Symbolizer.Merge(symbolizer, rule.Symbolizer);
            }
        }

        /// <summary>
        /// Evaluates a selector expression against the given context.
        /// </summary>
        /// <param name="selector">An <see cref="IExpression"/> representing the rule’s selector.</param>
        /// <param name="context">The <see cref="ExpressionContext"/> containing feature attributes.</param>
        /// <returns><c>true</c> if the selector matches; otherwise, <c>false</c>.</returns>
        private bool Resolve(IExpression selector, ExpressionContext context)
        {
            return expressionEvaluator.Evaluate(selector, context).AsBool();
        }
    }
}
