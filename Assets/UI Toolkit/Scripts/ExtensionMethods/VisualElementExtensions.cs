using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.ExtensionMethods
{
    public static class VisualElementExtensions
    {
        public static void CloneComponentTree(this VisualElement component, string path = "")
        {
            if (!string.IsNullOrEmpty(path)) path += "/";
            
            var asset = Resources.Load<VisualTreeAsset>($"UI/{path}{component.GetType().Name}");
            asset.CloneTree(component);
        }
        
        public static void AddComponentStylesheet(this VisualElement component, string path = "")
        {
            if (!string.IsNullOrEmpty(path)) path += "/";
            
            var styleSheet = Resources.Load<StyleSheet>($"UI/{path}{component.GetType().Name}-style");
            component.styleSheets.Add(styleSheet);
        }

        public static void RemoveFromClassListStartingWith(this VisualElement element, string prefix)
        {
            var classNames = element.GetClasses().Where(s => s.StartsWith(prefix)).ToList();
            foreach(var className in classNames)
            {
                element.RemoveFromClassList(className);
            }
        }

        public static void ReplacePrefixedValueInClassList(this VisualElement element, string prefix, string value)
        {
            element.RemoveFromClassListStartingWith(prefix);
            element.AddToClassList(prefix + value);
        }

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