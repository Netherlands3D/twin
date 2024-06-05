using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.LayerTypes;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Netherlands3D.Web;
using System;
using System.Collections.Specialized;

namespace Netherlands3D.Twin
{
    public class CredentialsPropertySection : MonoBehaviour
    {
        [SerializeField] private TMP_InputField userNameInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField keyTokenOrCodeInputField;
        [SerializeField] private TMP_Text keyTokenOrCodeLabel;
        private string defaultLabelText = "";
        [SerializeField] private Transform headerDefault;
        [SerializeField] private Transform headerWithCredentialTypeDropdown;
        [SerializeField] private TMP_Dropdown credentialTypeDropdown;
        [SerializeField] private Transform serverErrorFeedback;

        [Tooltip("KeyVault Scriptable Object")] [SerializeField] private KeyVault keyVault;

        private ILayerWithCredentials layerWithCredentials;
        public ILayerWithCredentials LayerWithCredentials { 
            get
            {
                return layerWithCredentials;
            }
            set
            {
                if(layerWithCredentials != null)
                    layerWithCredentials.OnURLChanged.RemoveListener(TryToDetermineCredentialsType);

                layerWithCredentials = value;

                if(layerWithCredentials != null){
                    layerWithCredentials.OnURLChanged.AddListener(TryToDetermineCredentialsType);
                    TryToDetermineCredentialsType(layerWithCredentials.URL);
                }
            } 
        }

        private CredentialType credentialType = CredentialType.None;
        private Coroutine findSpecificTypeCoroutine;    

        public void ApplyCredentials()
        {
            serverErrorFeedback.gameObject.SetActive(false);

            switch(credentialType)
            {
                case CredentialType.UsernamePassword:
                    LayerWithCredentials.SetCredentials(userNameInputField.text, passwordInputField.text);
                    break;
                case CredentialType.KeyTokenOrCode:
                    if(findSpecificTypeCoroutine != null)
                        StopCoroutine(findSpecificTypeCoroutine);

                    findSpecificTypeCoroutine = StartCoroutine(TryToFindSpecificType());
                    break;
            }
        }

        private void Awake()
        {
            defaultLabelText = keyTokenOrCodeLabel.text;
        }

        private void OnDisable()
        {
            if(LayerWithCredentials != null)
                LayerWithCredentials.OnURLChanged.RemoveListener(TryToDetermineCredentialsType);
        }

        private void TryToDetermineCredentialsType(string newURL)
        {
            credentialType = keyVault.DetermineCredentialType(newURL);
            Debug.Log("Determined credential type: " + credentialType);

            //Below we may want to transform input to detected credential types in the future
            return;

            if(credentialType == CredentialType.Key)
            {
                credentialTypeDropdown.value = (int)credentialType;
                keyTokenOrCodeLabel.text = "Sleutel";

                headerDefault.gameObject.SetActive(true);
                headerWithCredentialTypeDropdown.gameObject.SetActive(false);
            }
            else{
                keyTokenOrCodeLabel.text = defaultLabelText;
                headerDefault.gameObject.SetActive(false);
                headerWithCredentialTypeDropdown.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Try to find the specific type of credential (key, token or code) that is needed for the layer
        /// </summary>
        private IEnumerator TryToFindSpecificType()
        {
            // Try request without credentials
            var noCredentialsRequest = UnityWebRequest.Get(LayerWithCredentials.URL);
            yield return noCredentialsRequest.SendWebRequest();
            if(noCredentialsRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Found no credentials needed for this layer: " + LayerWithCredentials.URL);
                LayerWithCredentials.ClearCredentials();
                yield break;
            }

            // Try input as bearer token
            var bearerTokenRequest = UnityWebRequest.Get(LayerWithCredentials.URL);
            bearerTokenRequest.SetRequestHeader("Authorization", "Bearer " + keyTokenOrCodeInputField.text);
            yield return bearerTokenRequest.SendWebRequest();
            if(bearerTokenRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Found bearer token needed for this layer: " + LayerWithCredentials.URL);
                LayerWithCredentials.SetToken(keyTokenOrCodeInputField.text);
                yield break;
            }
            
            // Try input as 'key' query parameter (remove a possible existing key query parameter and add the new one)
            var uriBuilder = new UriBuilder(LayerWithCredentials.URL);
            var queryParameters = new NameValueCollection();
            uriBuilder.TryParseQueryString(queryParameters);
            uriBuilder.AddQueryParameter("key", keyTokenOrCodeInputField.text);
            var keyRequestUrl = UnityWebRequest.Get(uriBuilder.Uri);
            yield return keyRequestUrl.SendWebRequest();
            if(keyRequestUrl.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Found key needed for this layer: " + LayerWithCredentials.URL);
                LayerWithCredentials.SetKey(keyTokenOrCodeInputField.text);
                yield break;
            }

            // Try input as 'code' query parameter (remove a possible existing code query parameter and add the new one)
            uriBuilder.RemoveQueryParameter("key");
            uriBuilder.RemoveQueryParameter("code");
            uriBuilder.AddQueryParameter("code", keyTokenOrCodeInputField.text);
            var codeRequestUrl = UnityWebRequest.Get(uriBuilder.Uri);
            yield return codeRequestUrl.SendWebRequest();
            if(codeRequestUrl.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Found code needed for this layer: " + LayerWithCredentials.URL);
                LayerWithCredentials.SetCode(keyTokenOrCodeInputField.text);
                yield break;
            }

            Debug.Log("No credential type worked to get access for this layer: " + LayerWithCredentials.URL);
            // Nothing worked, show error
            serverErrorFeedback.gameObject.SetActive(true);
        }

        public void SetCredentialInputType(int type)
        {
            credentialType = (CredentialType)type;
            Debug.Log("Set credential type to: " + credentialType);
        }
    }
}
