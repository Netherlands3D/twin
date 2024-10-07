using System;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class CredentialsInputPropertySection : MonoBehaviour
    {
        private LayerCredentialsHandler handler;
        
        [SerializeField] private TMP_InputField userNameInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField keyTokenOrCodeInputField;
        [SerializeField] private TMP_Text keyTokenOrCodeLabel;

        [SerializeField] private Transform headerDefault;
        [SerializeField] private Transform headerWithCredentialTypeDropdown;
        [SerializeField] private TMP_Dropdown credentialTypeDropdown;
        [SerializeField] private Transform errorMessage;
        private bool skipFirstCredentialErrorMessage = true;

        private void Awake()
        {
            handler = GetComponent<LayerCredentialsHandler>();
            if (!handler)
                handler = gameObject.AddComponent<LayerCredentialsHandler>();
        }
        
        private void OnEnable()
        {
            errorMessage.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
        }

        public void ShowCredentialsWarning()
        {
            //First failure from server may be ignored, because we want to give the user a chance to fill in the credentials via the credentials panel
            if(skipFirstCredentialErrorMessage)
            {
                skipFirstCredentialErrorMessage = false;
                return;
            }

            //For now a standard text is shown.
            errorMessage.gameObject.SetActive(true);
        }
        
        public void CloseFailedFeedback()
        {
            errorMessage.gameObject.SetActive(false);
        }

        /// <summary>
        /// Apply the credentials input fields and start checking our authorization vault
        /// </summary>
        public void ApplyCredentials()
        {
            errorMessage.gameObject.SetActive(false);
            
            switch(handler.AuthorizationType)
            {
                case AuthorizationType.UsernamePassword:
                    handler.UserName = userNameInputField.text;
                    handler.PasswordOrKeyOrTokenOrCode = passwordInputField.text;
                    break;
                case AuthorizationType.InferableSingleKey:
                    handler.PasswordOrKeyOrTokenOrCode = keyTokenOrCodeInputField.text;
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
        public void SetInputFieldsValues(string username = "", string password = "", string key = "")
        {
            if(username.Length > 0) userNameInputField.text = username;
            if(password.Length > 0) passwordInputField.text = password;
            if(key.Length > 0) keyTokenOrCodeInputField.text = key;
        }
    }
}
