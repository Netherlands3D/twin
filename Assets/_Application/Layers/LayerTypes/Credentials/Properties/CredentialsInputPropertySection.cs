using System;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Twin.UI;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties
{
    public class CredentialsInputPropertySection : MonoBehaviour, ICredentialsPropertySection
    {
        private ICredentialHandler handler;
        
        [SerializeField] private GameObject inputPanel;
        [SerializeField] private GameObject errorMessage;

        [SerializeField] private TMP_InputField userNameInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField keyTokenOrCodeInputField;
        [SerializeField] private TMP_Dropdown credentialTypeDropdown;
        private bool skipFirstCredentialErrorMessage = true;
        private TMP_InputField inputFieldToUseForPasswordOrKey;
        
        public ICredentialHandler Handler
        {
            get => handler;
            set
            {
                handler?.OnAuthorizationHandled.RemoveListener(OnCredentialsHandled);

                handler = value;
                skipFirstCredentialErrorMessage = true;

                handler.OnAuthorizationHandled.AddListener(OnCredentialsHandled);
            }
        }

        private void OnCredentialsHandled(StoredAuthorization auth)
        {
            var accepted = auth is not FailedOrUnsupported;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                ShowCredentialsWarning(false);
                return;
            }
            
            ShowCredentialsWarning(!accepted);
            if (accepted)
            {
                inputPanel.gameObject.SetActive(false);
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
             ShowCredentialsWarning(false);
        }

        private void Start()
        {
            if(Handler == null) //we might want to set the handler explicitly, in which case we don't want to get it in the parent
                Handler = GetComponentInParent<ICredentialHandler>();
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
            handler.UserName = userNameInputField.text;
            handler.PasswordOrKeyOrTokenOrCode = inputFieldToUseForPasswordOrKey.text;

            handler.ApplyCredentials();
        }

        /// <summary>
        /// Set the authorization input type and update the UI.
        /// In the UI, there are 2 panels: username/password and key input. We need to use the input field to set the credentials for the KeyVault based on which one is visible (controlled by the dropdown). 
        /// </summary>
        public void SetAuthorizationInputType(int dropdownValue)
        {
            if (dropdownValue == 0)
            {
                inputFieldToUseForPasswordOrKey = passwordInputField;
            }
            else
            {
                inputFieldToUseForPasswordOrKey = keyTokenOrCodeInputField;
            }
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

        private void OnDestroy()
        {
            handler?.OnAuthorizationHandled.RemoveListener(OnCredentialsHandled);
        }
    }
}