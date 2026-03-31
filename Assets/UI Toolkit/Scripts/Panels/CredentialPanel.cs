using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using TextField = UnityEngine.UIElements.TextField;

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
        
        private TextField keyField;
        public TextField KeyField => keyField ??= this.Q<TextField>("KeyField");

        private VisualElement warning;
        private VisualElement update;
        private VisualElement Warning => warning ??= this.Q<VisualElement>("MessageTitleWarning");
        private VisualElement Update => update ??= this.Q<VisualElement>("MessageTitleUpdate");
        
        private ContentContainer content => this.Q<ContentContainer>();

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
            
            InitializeDropdown();
            
            SetContentState(ContentState.Warning);
            Button.clicked += () =>
            {
                if (contentState == ContentState.Warning)
                    SetContentState(ContentState.Key);
                else
                {
                    OnConfirm?.Invoke();
                    SetContentState(ContentState.Warning);
                    Hide();
                }
            };
        }

        private void InitializeDropdown()
        {
            var list = Enum.GetValues(typeof(ContentState))
                .Cast<ContentState>()
                .Skip(1) //skip warning
                .Select(e => e.ToString())
                .ToList();
            
            content.SetDropdownValues(list);
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
                    content.ShowDropDown = false;
                    content.ShowHelpIcon = true;
                    break;
                case ContentState.Key:
                    Update.SetEnabled(true);
                    Warning.SetEnabled(false);
                    Button.LabelText = "Bevestigen";
                    Button.ShowIcon =  Button.ButtonStyle.Normal;
                    content.ShowDropDown = true;
                    content.ShowHelpIcon = false;
                    break;
                case ContentState.UsernameAndPassword:
                    Update.SetEnabled(true);
                    Warning.SetEnabled(false);
                    Button.LabelText = "Bevestigen";
                    Button.ShowIcon =  Button.ButtonStyle.Normal;
                    content.ShowDropDown = true;
                    content.ShowHelpIcon = false;
                    break;
            }
        }
        
        public void Show() => OnShow?.Invoke();
        public void Hide() => OnHide?.Invoke();
    }
}