using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class ErrorPanelContent : VisualElement
    {
        const string hiddenUssClassName = "hidden";
        
        public UnityEvent OnShow = new();
        public UnityEvent OnHide = new();
        
        private Button retryButton;
        public Button RetryButton => retryButton ??= this.Q<Button>("RetryButton");

        private Label errorMessage;
        private Label ErrorMessage => errorMessage ??= this.Q<Label>("ErrorMessage");
        
        [UxmlAttribute("message")]
        public string Message
        {
            get => ErrorMessage.text;
            set => ErrorMessage.text = value;
        }

        public ErrorPanelContent()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            // OnShow += () => SetEnabled(true);
            // OnHide += () => SetEnabled(false);
            RetryButton.clicked += Hide;
        }

        ~ErrorPanelContent()
        {
            RetryButton.clicked -= Hide;
        }

        
        public void Show()
        {
            RemoveFromClassList(hiddenUssClassName);
            OnShow.Invoke();
        }

        public void Hide()
        { 
            AddToClassList(hiddenUssClassName);
            OnHide.Invoke();
        }
    }
}