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
        [SerializeField] private Transform serverErrorFeedback;

        [Tooltip("KeyVault Scriptable Object")] [SerializeField] private KeyVault keyVault;
        private AuthorizationType authorizationType = AuthorizationType.Public;

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

        private void UrlHasChanged(string newURL)
        {
            authorizationType = keyVault.GetKnownAuthorizationTypeForURL(newURL);
        }

        public void ApplyCredentials()
        {
            serverErrorFeedback.gameObject.SetActive(false);

            switch(authorizationType)
            {
                case AuthorizationType.UsernamePassword:
                    LayerWithCredentials.SetCredentials(userNameInputField.text, passwordInputField.text);
                    break;
                case AuthorizationType.ToBeDetermined:
                Debug.Log("Try to find specific credential type for: " + LayerWithCredentials.URL + " with key: " + keyTokenOrCodeInputField.text);
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
                case AuthorizationType.Key:
                    layerWithCredentials.SetKey(keyTokenOrCodeInputField.text);
                    break;
                case AuthorizationType.Token:
                    layerWithCredentials.SetToken(keyTokenOrCodeInputField.text);
                    break;
                case AuthorizationType.Code:
                    layerWithCredentials.SetCode(keyTokenOrCodeInputField.text);
                    break;
                case AuthorizationType.Public:
                    layerWithCredentials.ClearCredentials();
                    break;
            }
        }

        public void SetAuthorizationInputType(int type)
        {
            authorizationType = (AuthorizationType)type;
            Debug.Log("Force AuthorizationType to: " + authorizationType);
        }

        public void SetAuthorizationInputType(AuthorizationType type)
        {
            credentialTypeDropdown.value = (int)type;
            authorizationType = type;
        }
    }
}
