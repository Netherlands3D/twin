using System;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using static Netherlands3D.UI.Components.Toggle;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class CredentialPanel : VisualElement
    {
        public Action OnShow;
        public Action OnHide;
        public Action OnConfirm;
        
        private Button button;
        private Button Button => button ??= this.Q<Button>("RetryButton");

        private VisualElement warning;
        private VisualElement update;
        private VisualElement Warning => warning ??= this.Q<VisualElement>("MessageTitleWarning");
        private VisualElement Update => update ??= this.Q<VisualElement>("MessageTitleUpdate");

        private enum ContentState
        {
            Warning,
            Key,
            UsernameAndPassword
        }
        
        private ContentState contentState;
        
        public CredentialPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            OnShow += () => EnableInClassList("active", true);
            OnHide += () => EnableInClassList("active", false);
            
            
            SetContentState(ContentState.Warning);
            Button.clicked += () =>
            {
                if (contentState == ContentState.Warning)
                    SetContentState(ContentState.Key);
                else
                {
                    OnConfirm?.Invoke();
                    Hide();
                }
            };
        }

        private void SetContentState(ContentState state)
        {
            contentState = state;
            switch (state)
            {
                case ContentState.Warning:
                    Warning.SetEnabled(true);
                    Update.SetEnabled(false);
                    Button.LabelText = "Update";
                    Button.ShowIcon = Button.ButtonStyle.WithIcon;
                    break;
                case ContentState.Key:
                    Update.SetEnabled(true);
                    Warning.SetEnabled(false);
                    Button.LabelText = "Bevestigen";
                    Button.ShowIcon =  Button.ButtonStyle.Normal;
                    break;
                case ContentState.UsernameAndPassword:
                    Update.SetEnabled(true);
                    Warning.SetEnabled(false);
                    Button.LabelText = "Bevestigen";
                    Button.ShowIcon =  Button.ButtonStyle.Normal;
                    break;
            }
        }
        
        public void Show() => OnShow?.Invoke();
        public void Hide() => OnHide?.Invoke();
    }
}