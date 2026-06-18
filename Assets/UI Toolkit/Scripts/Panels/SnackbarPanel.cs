using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class SnackbarPanel : VisualElement
    {
        public UnityEvent OnClose = new();
        public UnityEvent OnOpen = new();
        
        private Label text;

        public SnackbarPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            text = this.Q<Label>();
        }

        public void Show(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }

        public void SetText(string newText)
        {
            text.text = newText;
        }

        public void SetTextColor(Color color)
        {
            text.style.color = color;
        }
    }
}