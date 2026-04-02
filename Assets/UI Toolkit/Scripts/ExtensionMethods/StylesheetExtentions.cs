using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.ExtensionMethods
{
    
    public static class StylesheetExtentions
    {
        // https://issuetracker.unity3d.com/issues/customstyleproperty-cant-be-retrieved-when-its-value-is-set-in-pixels
        public readonly struct PixelProperty
        {
            private readonly CustomStyleProperty<string> _property;

            public PixelProperty(string name) => _property = new CustomStyleProperty<string>(name);

            public bool TryGet(ICustomStyle style, out float value)
            {
                value = float.NaN;
                if (!style.TryGetValue(_property, out var str) || str.EndsWith("%"))
                    return false;

                if (str.EndsWith("px"))
                    str = str[..^2];

                return float.TryParse(str, out value);
            }
        }
        
        public static bool TryGet(this ICustomStyle style, PixelProperty property, out float value) => property.TryGet(style, out value);
    }
}
