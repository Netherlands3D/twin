using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.LayerStyles.Expressions.Operations
{
    /// <summary>
    /// Implements the Mapbox <c>number-format</c> expression operator, which formats
    /// a numeric value into a string according to locale, currency, and fraction-digit options.
    /// </summary>
    /// <seealso href="https://docs.mapbox.com/style-spec/reference/expressions/#types-number-format">
    ///   Mapbox “number-format” expression reference
    /// </seealso>
    internal static class NumberFormatOperation
    {
        /// <summary>The Mapbox operator string for “number-format”.</summary>
        public const string Code = "number-format";

        /// <summary>
        /// Evaluates the <c>number-format</c> expression by formatting its first numeric
        /// operand per the options object in the second operand.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> whose operands are [value, optionsObject].</param>
        /// <param name="context">The <see cref="ExpressionContext"/> providing any runtime data.</param>
        /// <returns>The formatted number as a <see cref="string"/>.</returns>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if operand count is not 2 or the second operand is not a valid options object.
        /// </exception>
        public static string Evaluate(Expression expression, ExpressionContext context)
        {
            Operations.GuardNumberOfOperands(Code, expression, expected: 2);

            double value = Operations.GetNumericOperand(Code, "value", expression, 0, context);

            return Format(value, GetOptions(Code, expression));
        }

        /// <summary>
        /// Retrieves and validates the raw options <see cref="JObject"/> from the second operand.
        /// </summary>
        private static Options GetOptions(string code, Expression expression)
        {
            object raw = expression.Operands[1];
            if (raw is not JObject opts)
            {
                throw new InvalidOperationException(
                    $"\"{code}\": second operand must be an object with formatting options, got {raw?.GetType().Name}."
                );
            }

            return new Options(opts);
        }
        
        /// <summary>
        /// Formats <paramref name="number"/> according to these settings.
        /// </summary>
        /// <param name="number">The number to format.</param>
        /// <returns>A locale- and option-aware formatted string.</returns>
        private static string Format(double number, Options options)
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(options.Locale);
            NumberFormatInfo nfi = (NumberFormatInfo)culture.NumberFormat.Clone();

            if (options.MinFraction.HasValue) nfi.NumberDecimalDigits = options.MinFraction.Value;
            if (options.MaxFraction.HasValue) nfi.NumberDecimalDigits = options.MaxFraction.Value;

            string specifier = options.Currency != null ? "C" : "N";

            if (options.Currency != null)
            {
                // Use the culture’s currency format, but override the symbol:
                NumberFormatInfo cfi = (NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo));
                cfi.CurrencySymbol = options.Currency;
                nfi = cfi;
            }

            return number.ToString(specifier, nfi);
        }

        /// <summary>
        /// Strongly‐typed representation of the <c>number-format</c> options.
        /// </summary>
        private readonly struct Options
        {
            /// <summary>Locale identifier, e.g. “en-US”.</summary>
            public readonly string Locale;

            /// <summary>Currency code, e.g. “USD”, or <c>null</c> for plain numeric.</summary>
            public readonly string Currency;

            /// <summary>Minimum fraction digits, or <c>null</c> to use default.</summary>
            public readonly int? MinFraction;

            /// <summary>Maximum fraction digits, or <c>null</c> to use default.</summary>
            public readonly int? MaxFraction;

            /// <summary>
            /// Constructs an <see cref="Options"/> from a JSON object.
            /// </summary>
            /// <param name="opts">The <see cref="JObject"/> containing formatting fields.</param>
            public Options(JObject opts)
            {
                Locale = opts.Value<string>("locale") ?? CultureInfo.InvariantCulture.Name;
                Currency = opts.Value<string>("currency");
                MinFraction = opts.Value<int?>("min-fraction-digits");
                MaxFraction = opts.Value<int?>("max-fraction-digits");
            }
        }
    }
}