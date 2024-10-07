using System;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
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
                // OnCredentialsAccepted(handler.HasValidCredentials);

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

        // private void Awake()
        // {
        //     handler = GetComponent<LayerCredentialsHandler>();
        //     if (!handler)
        //         handler = gameObject.AddComponent<LayerCredentialsHandler>();
        // }

        private void OnEnable()
        {
            ShowCredentialsWarning(false);
        }

        public void ShowCredentialsWarning(bool show)
        {
            print("shjowing warning:" + show);
            //First failure from server may be ignored, because we want to give the user a chance to fill in the credentials via the credentials panel
            // if (skipFirstCredentialErrorMessage)
            // {
            //     print("skipping");
            //     skipFirstCredentialErrorMessage = false;
            //     return;
            // }

            //For now a standard text is shown.
            inputPanel.SetActive(!show);
            errorMessage.SetActive(show);

            if (!show)
                SetAuthorizationInputType(credentialTypeDropdown.value);
        }

        // public void CloseFailedFeedback()
        // {
        //     inputPanel.SetActive(true);
        //     errorMessage.gameObject.SetActive(false);
        // }

        /// <summary>
        /// Apply the credentials input fields and start checking our authorization vault
        /// </summary>
        public void ApplyCredentials()
        {
            print(" applying");

            // ShowCredentialsWarning(false);

            switch (handler.AuthorizationType)
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
            if (username.Length > 0) userNameInputField.text = username;
            if (password.Length > 0) passwordInputField.text = password;
            if (key.Length > 0) keyTokenOrCodeInputField.text = key;
        }
    }
}