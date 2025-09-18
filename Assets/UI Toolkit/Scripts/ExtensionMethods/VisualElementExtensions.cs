using System.Text;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.ExtensionMethods
{
    public static class VisualElementExtensions
    {
        public static void SetFieldValueAndReplaceClassName<T>(
            this VisualElement visualElement, 
            ref T field, 
            T newValue, 
            string prefix = ""
        ) {
            visualElement.RemoveFromClassList(prefix + field.ToString().ToKebabCase());
            field = newValue;
            visualElement.AddToClassList(prefix + field.ToString().ToKebabCase());
        }
    
        public static string ToKebabCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var sb = new StringBuilder();
            char? prev = null;

            foreach (var c in input)
            {
                if (char.IsUpper(c))
                {
                    // Add dash if previous wasn't start or dash
                    if (prev.HasValue && prev != '-' && !char.IsUpper(prev.Value) && !char.IsDigit(prev.Value))
                        sb.Append('-');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else if (char.IsDigit(c))
                {
                    // Add dash if previous was a letter
                    if (prev.HasValue && char.IsLetter(prev.Value) && prev != '-')
                        sb.Append('-');
                    sb.Append(c);
                }
                else if (c == ' ' || c == '_' || c == '-')
                {
                    if (prev != '-')
                        sb.Append('-'); // collapse consecutive dashes/spaces/underscores
                }
                else
                {
                    // lowercase normal letter
                    if (prev.HasValue && char.IsDigit(prev.Value) && char.IsLetter(c))
                        sb.Append('-');
                    sb.Append(char.ToLowerInvariant(c));
                }

                prev = sb[^1];
            }

            // Trim trailing dash if present
            return sb.ToString().Trim('-');
        }
    }
}