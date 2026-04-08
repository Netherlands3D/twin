using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI_Toolkit.Scripts;
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
        public Action OnFailed;
        
        private Button button;
        private Button Button => button ??= this.Q<Button>("RetryButton");
        
        private TextField codeField;
        public TextField CodeField => codeField ??= this.Q<TextField>("CodeField");
        
        private TextField usernameField;
        public TextField UserNameField => usernameField ??= this.Q<TextField>("UsernameField");

        private VisualElement warning;
        private VisualElement code;
        private VisualElement username;
        private VisualElement Warning => warning ??= this.Q<VisualElement>("MessageTitleWarning");
        private VisualElement Code => code ??= this.Q<VisualElement>("MessageTitleCode");
        private VisualElement UserName => username ??= this.Q<VisualElement>("MessageTitleUserName");
        
        private ContentContainer content => this.Q<ContentContainer>();
        
        private ErrorPanel errorPanel;
        private ErrorPanel ErrorPanel =>  errorPanel ??= this.Q<ErrorPanel>();

        private enum ContentState
        {
            Warning,
            Key,
            UsernameAndPassword
        }
        
        private ContentState contentState;
        
        private readonly Dictionary<int, (ContentState state, IconImage icon)> dropDownValues = new()
        {
            { 0, (ContentState.Key, IconImage.KeyTokenCode) },
            { 1, (ContentState.UsernameAndPassword, IconImage.UsernamePassword) }
        };
        
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
                    if (string.IsNullOrEmpty(CodeField.value) || string.IsNullOrWhiteSpace(CodeField.value))
                    {
                        ErrorPanel.Show();
                        return;
                    }
                    OnConfirm?.Invoke();
                    ResetState();
                    Hide();
                }
            };
        }

        private void InitializeDropdown()
        {
            content.SetDropdownValues(dropDownValues.Values.Select(x => x.icon).ToList());
            content.AddDropDownListener(SetContentState);
        }

        private void SetContentState(int state)
        {
            if (!dropDownValues.TryGetValue(state, out var mapping))
                return;
            
            SetContentState(mapping.state);
        }

        private void SetContentState(ContentState state)
        {
            contentState = state;
            
            //update the dropdownvalue if the content is set to a valid value
            int index = -1;
            foreach (KeyValuePair<int, (ContentState state, IconImage icon)> kv in dropDownValues)
                if(kv.Value.state == state)
                    index = kv.Key;
            
            if(dropDownValues.Keys.Contains(index))
                content.SetDropdownValue(index);
           
            switch (state)
            {
                case ContentState.Warning:
                    Warning.SetEnabled(true);
                    Code.SetEnabled(false);
                    UserName.SetEnabled(false);
                    Button.LabelText = "Update";
                    Button.ShowIcon = Button.ButtonStyle.WithIcon;
                    content.ShowDropDown = false;
                    content.ShowHelpIcon = true;
                    break;
                case ContentState.Key:
                    Warning.SetEnabled(false);
                    Code.SetEnabled(true);
                    Code.Q<Label>().text = "Wachtwoord of code";
                    UserName.SetEnabled(false);
                    Button.LabelText = "Bevestigen";
                    Button.ShowIcon =  Button.ButtonStyle.Normal;
                    content.ShowDropDown = true;
                    content.ShowHelpIcon = false;
                    
                    break;
                case ContentState.UsernameAndPassword:
                    Warning.SetEnabled(false);
                    Code.SetEnabled(true);
                    Code.Q<Label>().text = "Wachtwoord";
                    UserName.SetEnabled(true);
                    Button.LabelText = "Bevestigen";
                    Button.ShowIcon =  Button.ButtonStyle.Normal;
                    content.ShowDropDown = true;
                    content.ShowHelpIcon = false;
                    break;
            }
        }
        
        public void Show() => OnShow?.Invoke();
        public void Hide() => OnHide?.Invoke();
        
        public void ResetState() => SetContentState(ContentState.Warning);
    }
}