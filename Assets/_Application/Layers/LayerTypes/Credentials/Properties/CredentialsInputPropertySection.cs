using Netherlands3D.Credentials;
using Netherlands3D.Twin.UI;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties
{
    public class CredentialsInputPropertySection : MonoBehaviour, ILayerCredentialInterface
    {
        private LayerCredentialsHandler handler;

        [SerializeField] private GameObject inputPanel;
        [SerializeField] private GameObject errorMessage;

        [SerializeField] private TMP_InputField userNameInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField keyTokenOrCodeInputField;
        [SerializeField] private TMP_Dropdown credentialTypeDropdown;
        private bool skipFirstCredentialErrorMessage = true;

        public LayerCredentialsHandler Handler
        {
            get => handler;
            set
            {
                if (handler)
                    handler.CredentialsAccepted.RemoveListener(OnCredentialsAccepted);

                handler = value;

                skipFirstCredentialErrorMessage = true;

                handler.CredentialsAccepted.AddListener(OnCredentialsAccepted);
            }
        }

        private void OnCredentialsAccepted(bool accepted)
        {
            ShowCredentialsWarning(!accepted);
            if (accepted)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            ShowCredentialsWarning(false);
        }

        public void ShowCredentialsWarning(bool show)
        {
            inputPanel.SetActive(!show);
            errorMessage.SetActive(show);

            if (!show)
                SetAuthorizationInputType(credentialTypeDropdown.value);
        }
        
        /// <summary>
        /// Apply the credentials input fields and start checking our authorization vault
        /// </summary>
        public void ApplyCredentials()
        {
            switch (handler.AuthorizationType)
            {
                case AuthorizationType.UsernamePassword:
                    handler.UserName = userNameInputField.text;
                    handler.PasswordOrKeyOrTokenOrCode = passwordInputField.text;
                    break;
                case AuthorizationType.InferableSingleKey:
                    handler.PasswordOrKeyOrTokenOrCode = keyTokenOrCodeInputField.text.Trim();
                    break;
            }

            handler.ApplyCredentials();
        }

        /// <summary>
        /// Set the authorization input type and update the UI
        /// </summary>
        public void SetAuthorizationInputType(int type)
        {
            handler.SetAuthorizationInputType((AuthorizationType)type);
        }

        public void SetAuthorizationInputType(AuthorizationType type)
        {
            handler.SetAuthorizationInputType(type);

            credentialTypeDropdown.value = (int)type;

            //Similar values are not reapplied, so make sure to the dropdown items appear
            if (credentialTypeDropdown.TryGetComponent(out DropdownSelection dropdownSelection))
                dropdownSelection.DropdownSelectItem(credentialTypeDropdown.value);
        }

        /// <summary>
        /// Fill the inputs with predefined values
        /// </summary>
        
        //Todo: This function is no longer used. This does cause the currently used key to not show up when re-opening the properties panel, this should be re-implemented if deemed a useful feature.
        public void SetInputFieldsValues(string username = "", string password = "", string key = "")
        {
            if (username.Length > 0) userNameInputField.text = username;
            if (password.Length > 0) passwordInputField.text = password;
            if (key.Length > 0) keyTokenOrCodeInputField.text = key;
        }
    }
}