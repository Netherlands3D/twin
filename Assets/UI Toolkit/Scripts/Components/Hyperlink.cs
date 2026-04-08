using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Hyperlink : ChangePointerStyleElement
    {
        private const string underlineStartTag = "<u>";
        private const string underlineEndTag = "</u>";
        
        private Label label;
        [UxmlAttribute] public string url { get; set; }
        [UxmlAttribute] public string text {get => label.text; set => label.text = value; }
        
        public Hyperlink() : base()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        
            label = this.Q<Label>();
            if (text == string.Empty)
                text = url;
            
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
        
        public void Click()
        {
            Application.OpenURL(url);
        }
    }
}