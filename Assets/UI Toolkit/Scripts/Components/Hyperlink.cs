using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Hyperlink : UnityEngine.UIElements.Label
    {
        [UxmlAttribute] public string url { get; set; }

        public Hyperlink()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            RegisterCallback<ClickEvent>(OnClick);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            text = UnderlineText(text);
        }

        private string UnderlineText(string s)
        {
            return "<u>" + s +"</u>";
        }

        private void OnClick(ClickEvent evt)
        {
            Application.OpenURL(url);
        }
    }
}