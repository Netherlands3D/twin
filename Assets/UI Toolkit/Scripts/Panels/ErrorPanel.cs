using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class ErrorPanel : VisualElement
    {
        public Action OnShow;
        public Action OnHide;
        
        private Button retryButton;
        public Button RetryButton => retryButton ??= this.Q<Button>("RetryButton");

        public ErrorPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            OnShow += () => EnableInClassList("active", true);
            OnHide += () => EnableInClassList("active", false);
            RetryButton.clicked += Hide;
        }

        ~ErrorPanel()
        {
            RetryButton.clicked -= Hide;
        }

        
        public void Show() => OnShow?.Invoke();
        public void Hide() => OnHide?.Invoke();
    }
}
