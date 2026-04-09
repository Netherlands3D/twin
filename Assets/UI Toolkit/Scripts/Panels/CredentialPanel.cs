using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Credentials;
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
        public ICredentialHandler handler { get; set; }
        
        private Button warningButton;
        private Button WarningButton => warningButton ??= this.Q<Button>("WarningButton");
        private Button credentialButton;
        private Button CredentialButton => credentialButton ??= this.Q<Button>("CredentialButton");

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

        private ContentContainer warningContent;
        private ContentContainer credentialContent;

        private ErrorPanel errorPanel;
        private ErrorPanel ErrorPanel => errorPanel ??= this.Q<ErrorPanel>();

        private enum ContentState
        {
            Warning,
            Key,
            UsernameAndPassword
        }

        private readonly Dictionary<int, (ContentState state, IconImage icon)> dropDownValues = new()
        {
            { 0, (ContentState.Key, IconImage.KeyTokenCode) },
            { 1, (ContentState.UsernameAndPassword, IconImage.UsernamePassword) }
        };

        public CredentialPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            warningContent = this.Q<ContentContainer>("WarningContent");
            credentialContent = this.Q<ContentContainer>("CredentialContent");

            InitializeDropdown();

            SetContentState(ContentState.Warning);
            WarningButton.clicked += () =>
            {
                SetContentState(ContentState.Key);
            };
            CredentialButton.clicked += () =>
            {
                if (string.IsNullOrEmpty(CodeField.value) || string.IsNullOrWhiteSpace(CodeField.value))
                {
                    ErrorPanel.Show();
                    return;
                }

                handler.UserName = UserNameField.value;
                handler.PasswordOrKeyOrTokenOrCode = CodeField.value;
                handler.ApplyCredentials();
                ResetState();
                SetEnabled(false);
            };
        }

        private void InitializeDropdown()
        {
            credentialContent.SetDropdownValues(dropDownValues.Values.Select(x => x.icon).ToList());
            credentialContent.AddDropDownListener(SetContentState);
        }

        private void SetContentState(int state)
        {
            if (!dropDownValues.TryGetValue(state, out var mapping))
                return;

            SetContentState(mapping.state);
        }

        private void SetContentState(ContentState state)
        {
            //update the dropdownvalue if the content is set to a valid value
            int index = -1;
            foreach (KeyValuePair<int, (ContentState state, IconImage icon)> kv in dropDownValues)
                if (kv.Value.state == state)
                    index = kv.Key;

            if (dropDownValues.Keys.Contains(index))
                credentialContent.SetDropdownValue(index);

            warningContent.SetEnabled(state == ContentState.Warning);
            credentialContent.SetEnabled(state != ContentState.Warning);
            switch (state)
            {
                case ContentState.Key:
                    Code.Q<Label>().text = "Wachtwoord of code";
                    UserName.SetEnabled(false);
                    break;
                case ContentState.UsernameAndPassword:
                    Code.Q<Label>().text = "Wachtwoord";
                    UserName.SetEnabled(true);
                    break;
            }
        }

        public void ResetState() => SetContentState(ContentState.Warning);
    }
}