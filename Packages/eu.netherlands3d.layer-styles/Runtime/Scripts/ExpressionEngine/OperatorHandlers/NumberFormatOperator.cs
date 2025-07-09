using System;
using System.Globalization;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.LayerStyles.ExpressionEngine.OperatorHandlers
{
    /// <summary>
    /// ["number-format", number, options] → format the number into a string per the given options. :contentReference[oaicite:1]{index=1}
    /// </summary>
    internal static class NumberFormatOperator
    {
        public const string Code = "number-format";

        public static string Evaluate(Expression expr, ExpressionContext ctx)
        {
            // 1) get the number
            var n = Convert.ToDouble(
                ExpressionEvaluator.Evaluate(expr, 0, ctx),
                CultureInfo.InvariantCulture
            );

            // 2) get the options object from the second operand
            var rawOpts = expr.Operands[1];
            if (rawOpts is not Newtonsoft.Json.Linq.JObject opts)
            {
                throw new InvalidOperationException(
                    "\"number-format\": second operand must be an object with formatting options."
                );
            }

            // 3) pull out known options
            var locale = opts.Value<string>("locale") ?? CultureInfo.InvariantCulture.Name;
            var currency = opts.Value<string>("currency");
            var minFD = opts.Value<int?>("min-fraction-digits");
            var maxFD = opts.Value<int?>("max-fraction-digits");

            var culture = CultureInfo.GetCultureInfo(locale);
            var fmt = currency != null
                ? "C" // currency format
                : "N"; // number format

            // set fraction digits
            var nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
            if (minFD.HasValue) nfi.NumberDecimalDigits = minFD.Value;
            if (maxFD.HasValue) nfi.NumberDecimalDigits = maxFD.Value;

            // use currency code if asked
            if (currency != null)
            {
                nfi = (NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo));
                nfi.CurrencySymbol = currency;
            }

            return n.ToString(fmt, nfi);
        }
    }
}