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
        [SerializeField] private TMP_Dropdown credentialTypeDropdown;
        [SerializeField] private Transform serverErrorFeedback;
        
        [Tooltip("KeyVault Scriptable Object")] [SerializeField] private KeyVault keyVault;

        public ILayerWithCredentials LayerWithCredentials { get; set; }

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

        private void OnEnable()
        {
            TryToDetermineCredentialsType(LayerWithCredentials.URL);
            LayerWithCredentials.OnURLChanged.AddListener(TryToDetermineCredentialsType);
        }

        private void OnDisable()
        {
            LayerWithCredentials.OnURLChanged.RemoveListener(TryToDetermineCredentialsType);
        }

        private void TryToDetermineCredentialsType(string newURL)
        {
            credentialType = keyVault.DetermineCredentialType(newURL);
            Debug.Log("Determined credential type: " + credentialType);

            //Update dropdown to reflect credential type (Just key for now, for Google api)
            if(credentialType == CredentialType.Key)
            {
                credentialTypeDropdown.value = (int)credentialType;
                keyTokenOrCodeLabel.text = "Sleutel";
            }
            else{
                keyTokenOrCodeLabel.text = defaultLabelText;
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
