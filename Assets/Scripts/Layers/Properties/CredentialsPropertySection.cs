using System.Collections;
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
        [SerializeField] private Transform serverErrorFeedback;

        public ILayerWithCredentials LayerWithCredentials { get; set; }

        public enum CredentialType
        {
            UsernamePassword = 0,
            KeyTokenOrCode = 1,
            None = 2,
        }

        public CredentialType credentialType = CredentialType.None;

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
                LayerWithCredentials.ClearCredentials();
                yield break;
            }

            // Try input as bearer token
            var bearerTokenRequest = UnityWebRequest.Get(LayerWithCredentials.URL);
            bearerTokenRequest.SetRequestHeader("Authorization", "Bearer " + keyTokenOrCodeInputField.text);
            yield return bearerTokenRequest.SendWebRequest();
            if(bearerTokenRequest.result == UnityWebRequest.Result.Success)
            {
                LayerWithCredentials.SetToken(keyTokenOrCodeInputField.text);
                yield break;
            }
            
            // Try input as 'key' query parameter (remove a possible existing key query parameter and add the new one)
            var uriBuilder = new UriBuilder(LayerWithCredentials.URL);
            var queryParameters = new NameValueCollection();
            uriBuilder.TryParseQueryString(queryParameters);
            uriBuilder.RemoveQueryParameter("key"); 
            uriBuilder.AddQueryParameter("key", keyTokenOrCodeInputField.text);
            var keyRequestUrl = UnityWebRequest.Get(uriBuilder.Uri);
            yield return keyRequestUrl.SendWebRequest();
            if(keyRequestUrl.result == UnityWebRequest.Result.Success)
            {
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
                LayerWithCredentials.SetCode(keyTokenOrCodeInputField.text);
                yield break;
            }

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
