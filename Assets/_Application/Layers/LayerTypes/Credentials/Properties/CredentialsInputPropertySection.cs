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
        private ICredentialHandlerPanel handlerPanel;
        
        [SerializeField] private GameObject inputPanel;
        [SerializeField] private GameObject errorMessage;

        [SerializeField] private TMP_InputField userNameInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField keyTokenOrCodeInputField;
        [SerializeField] private TMP_Dropdown credentialTypeDropdown;
        private bool skipFirstCredentialErrorMessage = true;
        private TMP_InputField inputFieldToUseForPasswordOrKey;

        [SerializeField] private bool visibleOnAwake; //todo: find a better way to do this

        public ICredentialHandlerPanel HandlerPanel
        {
            get => handlerPanel;
            set
            {
                handlerPanel?.OnAuthorizationHandled.RemoveListener(OnCredentialsAccepted);

                handlerPanel = value;
                skipFirstCredentialErrorMessage = true;

                handlerPanel.OnAuthorizationHandled.AddListener(OnCredentialsAccepted);
            }
        }

        private void OnCredentialsAccepted(StoredAuthorization auth)
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

        private void Awake()
        {
            gameObject.SetActive(visibleOnAwake); //always start disabled because we assume we don't need credentials, but we need to assign our handler above
        }

        private void OnEnable()
        {
             ShowCredentialsWarning(false);
        }

        private void Start()
        {
            if(HandlerPanel == null) //we might want to set the handler explicitly, in which case we don't want to get it in the parent
                HandlerPanel = GetComponentInParent<ICredentialHandlerPanel>();
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
            handlerPanel.UserName = userNameInputField.text;
            handlerPanel.PasswordOrKeyOrTokenOrCode = inputFieldToUseForPasswordOrKey.text;

            handlerPanel.ApplyCredentials();
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

        public void SetAuthorizationInputType(AuthorizationType type) //todo make property panel match StoredAuthorization type 
        {
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

        private void OnDestroy()
        {
            handlerPanel?.OnAuthorizationHandled.RemoveListener(OnCredentialsAccepted);
        }
    }
}