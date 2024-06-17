using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.LayerTypes;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Netherlands3D.Web;
using System;
using System.Collections.Specialized;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class CredentialsPropertySection : MonoBehaviour
    {
        [SerializeField] private TMP_InputField userNameInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField keyTokenOrCodeInputField;
        [SerializeField] private TMP_Text keyTokenOrCodeLabel;

        [SerializeField] private Transform headerDefault;
        [SerializeField] private Transform headerWithCredentialTypeDropdown;
        [SerializeField] private TMP_Dropdown credentialTypeDropdown;
        [SerializeField] private Transform errorMessage;

        [Header("Settings")]
        [SerializeField] private bool findKeyInVaultOnURLChange = true;

        [Tooltip("KeyVault Scriptable Object")] [SerializeField] private KeyVault keyVault;
        private AuthorizationType authorizationType = AuthorizationType.Public;
        private AuthorizationType lastAppliedAuthorizationType = AuthorizationType.Public;
        private StoredAuthorization storedAuthorization;

        private ILayerWithCredentials layerWithCredentials;
        public ILayerWithCredentials LayerWithCredentials { 
            get
            {
                return layerWithCredentials;
            }
            set
            {
                if(layerWithCredentials != null)
                    layerWithCredentials.OnURLChanged.RemoveListener(UrlHasChanged);

                layerWithCredentials = value;

                if(layerWithCredentials != null){
                    layerWithCredentials.OnURLChanged.AddListener(UrlHasChanged);
                    UrlHasChanged(layerWithCredentials.URL);
                }
            } 
        }

        private void OnEnable() {
            keyVault.OnAuthorizationTypeDetermined.AddListener(OnCredentialTypeDetermined);
        }

        private void OnDisable()
        {
            keyVault.OnAuthorizationTypeDetermined.RemoveListener(OnCredentialTypeDetermined);

            if(LayerWithCredentials != null)
                LayerWithCredentials.OnURLChanged.RemoveListener(UrlHasChanged);
        }

        public void ShowAuthorizationFailedFeedback()
        {
            //For now a standard text is shown.
            errorMessage.gameObject.SetActive(true);
        }

        public void CloseFailedFeedback()
        {
            errorMessage.gameObject.SetActive(false);

            //Gob back to our last applied type
            authorizationType = lastAppliedAuthorizationType;
            SetAuthorizationInputType(authorizationType);
        }

        private void UrlHasChanged(string newURL)
        {
            //New url. If we already got this one in the vault, apply the credentials
            if(findKeyInVaultOnURLChange)
            {
                storedAuthorization = keyVault.GetStoredAuthorization(newURL); 

                if(storedAuthorization != null)
                {
                    authorizationType = storedAuthorization.authorizationType;
                    userNameInputField.text = storedAuthorization.username;
                    passwordInputField.text = storedAuthorization.password;
                    keyTokenOrCodeInputField.text = storedAuthorization.key;    
                }
                else
                {
                    authorizationType = keyVault.GetKnownAuthorizationTypeForURL(newURL);
                }
                
                SetAuthorizationInputType(authorizationType);
            }
        }

        /// <summary>
        /// Apply the credentials input fields and start checking our authorization vault
        /// </summary>
        public void ApplyCredentials()
        {
            errorMessage.gameObject.SetActive(false);

            lastAppliedAuthorizationType = authorizationType;

            switch(authorizationType)
            {
                case AuthorizationType.UsernamePassword:
                    keyVault.TryBasicAuthentication(
                        LayerWithCredentials.URL, 
                        userNameInputField.text, 
                        passwordInputField.text
                        );
                    break;
                case AuthorizationType.SingleFieldGenericKey:
                    keyVault.TryToFindSpecificCredentialType(
                        LayerWithCredentials.URL,
                        keyTokenOrCodeInputField.text
                    );
                    break;
            }
        }

        private void OnCredentialTypeDetermined(string url, AuthorizationType type)
        {
            Debug.Log("Vault determined credential type: " + type + " for url: " + url);
            var layerUrl = layerWithCredentials.URL.TrimEnd('?', '&');
            if(url != layerUrl) return;

            credentialTypeDropdown.value = (int)type;
            authorizationType = type;

            switch(type)
            {
                case AuthorizationType.UsernamePassword:
                    layerWithCredentials.SetCredentials(userNameInputField.text, passwordInputField.text);
                    break;
                case AuthorizationType.BearerToken:
                    layerWithCredentials.SetBearerToken(keyTokenOrCodeInputField.text);
                    break;
                case AuthorizationType.Key:
                    layerWithCredentials.SetKey(keyTokenOrCodeInputField.text);
                    break;
                case AuthorizationType.Code:
                    layerWithCredentials.SetCode(keyTokenOrCodeInputField.text);
                    break;
                case AuthorizationType.Token:
                    layerWithCredentials.SetToken(keyTokenOrCodeInputField.text);
                    break;
                case AuthorizationType.Public:
                    layerWithCredentials.ClearCredentials();
                    break;
                case AuthorizationType.Unknown:
                    ShowAuthorizationFailedFeedback();
                    break;
            }
        }

        /// <summary>
        /// Set the authorization input type and update the UI
        /// </summary>
        public void SetAuthorizationInputType(int type)
        {
            authorizationType = (AuthorizationType)type;
            Debug.Log("Force AuthorizationType to: " + authorizationType);

            SetAuthorizationInputType(authorizationType);
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

        /// <summary>
        /// Set the authorization input type and update the UI
        /// </summary>
        public void SetAuthorizationInputType(AuthorizationType type)
        {
            if(
                type == AuthorizationType.Key 
             || type == AuthorizationType.Token
             || type == AuthorizationType.BearerToken 
             || type == AuthorizationType.Code
            )
            type = AuthorizationType.SingleFieldGenericKey;

            credentialTypeDropdown.value = (int)type;
            authorizationType = type;
        }
    }
}
