using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Hyperlink : UnityEngine.UIElements.Label
    {
        [UxmlAttribute] public string url { get; set; }
        private const string underlineStartTag = "<u>";
        private const string underlineEndTag = "</u>";

        public Hyperlink()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterCallback<ClickEvent>(OnClick);
            RegisterCallback<PointerOverEvent>(OnPointerOver);
            RegisterCallback<PointerOutEvent>(OnPointerOut);
        }

        private void OnPointerOver(PointerOverEvent evt)
        {
            text = AddUnderline(text);
        }

        private void OnPointerOut(PointerOutEvent evt)
        {
            text = RemoveUnderline(text);
        }

        private string RemoveUnderline(string s)
        {
            if (s.StartsWith(underlineStartTag))
            {
                return s.Substring(underlineStartTag.Length, s.Length - underlineStartTag.Length - underlineEndTag.Length);
            }

            return s;
        }

        private string AddUnderline(string s)
        {
            return underlineStartTag + s + underlineEndTag;
        }

        private void OnClick(ClickEvent evt)
        {
            Application.OpenURL(url);
        }
    }
}