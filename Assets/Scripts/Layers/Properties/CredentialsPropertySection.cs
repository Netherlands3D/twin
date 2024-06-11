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
        private CredentialType credentialType = CredentialType.None;

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
            keyVault.OnCredentialTypeDetermined.AddListener(OnCredentialTypeDetermined);
        }

        private void OnDisable()
        {
            keyVault.OnCredentialTypeDetermined.RemoveListener(OnCredentialTypeDetermined);

            if(LayerWithCredentials != null)
                LayerWithCredentials.OnURLChanged.RemoveListener(UrlHasChanged);
        }

        private void UrlHasChanged(string newURL)
        {
            credentialType = keyVault.GetKnownCredentialTypeForURL(newURL);
        }

        public void ApplyCredentials()
        {
            serverErrorFeedback.gameObject.SetActive(false);

            switch(credentialType)
            {
                case CredentialType.UsernamePassword:
                    LayerWithCredentials.SetCredentials(userNameInputField.text, passwordInputField.text);
                    break;
                case CredentialType.ToBeDetermined:
                    keyVault.TryToFindSpecificCredentialType(
                        LayerWithCredentials.URL,
                        keyTokenOrCodeInputField.text
                    );
                    break;
            }
        }

        private void OnCredentialTypeDetermined(string url, CredentialType type)
        {
            if(layerWithCredentials.URL != url)
                return;

            credentialTypeDropdown.value = (int)type;
            credentialType = type;

            switch(type)
            {
                case CredentialType.Key:
                    layerWithCredentials.SetKey(keyTokenOrCodeInputField.text);
                    break;
                case CredentialType.Token:
                    layerWithCredentials.SetToken(keyTokenOrCodeInputField.text);
                    break;
                case CredentialType.Code:
                    layerWithCredentials.SetCode(keyTokenOrCodeInputField.text);
                    break;
                case CredentialType.None:
                    layerWithCredentials.ClearCredentials();
                    break;
            }
        }

        public void SetCredentialInputType(int type)
        {
            credentialType = (CredentialType)type;
            Debug.Log("Set credential type to: " + credentialType);
        }
    }
}
