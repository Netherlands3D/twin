using System;
using Netherlands3D.UI.Components;
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

        private ContentContainer content;
        private ContentContainer Content => content ??= this.Q<ContentContainer>();
        
        private Label errorMessage;
        private Label ErrorMessage => errorMessage ??= this.Q<Label>("ErrorMessage");
        
        [UxmlAttribute("text")]
        public string HeaderText
        {
            get => Content.HeaderText;
            set => Content.HeaderText = value;
        }
        
        [UxmlAttribute("message")]
        public string Message
        {
            get => ErrorMessage.text;
            set => ErrorMessage.text = value;
        }

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
