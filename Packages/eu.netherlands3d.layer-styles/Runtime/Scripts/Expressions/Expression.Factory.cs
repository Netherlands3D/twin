namespace Netherlands3D.LayerStyles.Expressions
{
    public partial class Expression
    {
        // Types & assertions
        public static Expression Literal(params object[] values) => new(Operators.Literal, values);
        public static Expression Array(params object[] values) => new(Operators.Array, values);
        public static Expression Boolean(params object[] values) => new(Operators.Boolean, values);
        public static Expression Number(params object[] values) => new(Operators.Number, values);
        public static Expression NumberFormat(object number, object options) => new(Operators.NumberFormat, number, options);
        public static Expression Object(params object[] values) => new(Operators.Object, values);
        public static Expression String(params object[] values) => new(Operators.String, values);

        // Conversions
        public static Expression ToBoolean(object v) => new(Operators.ToBoolean, v);
        public static Expression ToColor(object v) => new(Operators.ToColor, v);
        public static Expression ToNumber(object v) => new(Operators.ToNumber, v);
        public static Expression ToString(object v) => new(Operators.ToString, v);
        public static Expression TypeOf(object v) => new(Operators.TypeOf, v);

        // Feature lookup
        public static Expression Get(string key) => new(Operators.Get, key);

        // Logic
        public static Expression Not(object v) => new(Operators.Not, v);
        public static Expression EqualTo(object a, object b) => new(Operators.EqualTo, a, b);
        public static Expression NotEqual(object a, object b) => new(Operators.NotEqual, a, b);
        public static Expression LessThan(object a, object b) => new(Operators.LessThan, a, b);
        public static Expression LessThanOrEqual(object a, object b) => new(Operators.LessThanOrEqual, a, b);
        public static Expression GreaterThan(object a, object b) => new(Operators.GreaterThan, a, b);
        public static Expression GreaterThanOrEqual(object a, object b) => new(Operators.GreaterThanOrEqual, a, b);
        public static Expression All(params object[] clauses) => new(Operators.All, clauses);
        public static Expression Any(params object[] clauses) => new(Operators.Any, clauses);

        // Color constructors & converters
        public static Expression Hsl(object h, object s, object l) => new(Operators.Hsl, h, s, l);
        public static Expression Hsla(object h, object s, object l, object a) => new(Operators.Hsla, h, s, l, a);
        public static Expression Rgb(object r, object g, object b) => new(Operators.Rgb, r, g, b);
        public static Expression Rgba(object r, object g, object b, object a) => new(Operators.Rgba, r, g, b, a);
        public static Expression ToHsla(object color) => new(Operators.ToHsla, color);
        public static Expression ToRgba(object color) => new(Operators.ToRgba, color);

        // Math
        public static Expression Add(params object[] vals) => new(Operators.Add, vals);
        public static Expression Subtract(params object[] vals) => new(Operators.Subtract, vals);
        public static Expression Multiply(params object[] vals) => new(Operators.Multiply, vals);
        public static Expression Divide(params object[] vals) => new(Operators.Divide, vals);
        public static Expression Modulo(params object[] vals) => new(Operators.Modulo, vals);
        public static Expression Power(object @base, object exp) => new(Operators.Power, @base, exp);
        public static Expression Abs(object v) => new(Operators.Abs, v);
        public static Expression Acos(object v) => new(Operators.Acos, v);
        public static Expression Asin(object v) => new(Operators.Asin, v);
        public static Expression Atan(object v) => new(Operators.Atan, v);
        public static Expression Ceil(object v) => new(Operators.Ceil, v);
        public static Expression E() => new(Operators.E);
        public static Expression Floor(object v) => new(Operators.Floor, v);
        public static Expression Ln(object v) => new(Operators.Ln, v);
        public static Expression Ln2() => new(Operators.Ln2);
        public static Expression Log10(object v) => new(Operators.Log10, v);
        public static Expression Log2(object v) => new(Operators.Log2, v);
        public static Expression Max(params object[] vals) => new(Operators.Max, vals);
        public static Expression Min(params object[] vals) => new(Operators.Min, vals);
        public static Expression Pi() => new(Operators.Pi);
        public static Expression Random() => new(Operators.Random);
        public static Expression Round(object v) => new(Operators.Round, v);
        public static Expression Sin(object v) => new(Operators.Sin, v);
        public static Expression Sqrt(object v) => new(Operators.Sqrt, v);
        public static Expression Tan(object v) => new(Operators.Tan, v);
    }
}