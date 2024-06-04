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

        public ILayerWithCredentials Layer { get; set; }

        public enum CredentialType
        {
            None = 0,
            KeyTokenOrCode = 1,
            UsernamePassword = 2,
            Key = 3,
            Token = 4,
            Code = 5
        }

        public CredentialType credentialType = CredentialType.UsernamePassword;

        private string url = "";
        public string Url { get => Url; set => Url = value; }    

        private Coroutine findSpecificTypeCoroutine;    

        public void ApplyCredentials()
        {
            serverErrorFeedback.gameObject.SetActive(false);

            switch(credentialType)
            {
                case CredentialType.UsernamePassword:
                    Layer.SetCredentials(userNameInputField.text, passwordInputField.text);
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
            //Try request without credentials
            var noCredentialsRequest = UnityWebRequest.Get(Url);
            yield return noCredentialsRequest.SendWebRequest();
            if(noCredentialsRequest.result == UnityWebRequest.Result.Success)
            {
                Layer.SetCredentials("", "");
                Layer.SetKey("");
                Layer.SetToken("");
                Layer.SetCode("");
                yield break;
            }

            //Try input as token
            var bearerTokenRequest = UnityWebRequest.Get(Url);
            bearerTokenRequest.SetRequestHeader("Authorization", "Bearer " + keyTokenOrCodeInputField.text);
            yield return bearerTokenRequest.SendWebRequest();
            if(bearerTokenRequest.result == UnityWebRequest.Result.Success)
            {
                Layer.SetToken(keyTokenOrCodeInputField.text);
                yield break;
            }
            
            //Try input as 'key' query parameter (remove a possible existing key query parameter and add the new one)
            var uriBuilder = new UriBuilder(Url);
            var queryParameters = new NameValueCollection();
            uriBuilder.TryParseQueryString(queryParameters);
            uriBuilder.RemoveQueryParameter("key"); 
            uriBuilder.AddQueryParameter("key", keyTokenOrCodeInputField.text);
            var keyRequestUrl = UnityWebRequest.Get(uriBuilder.Uri);
            yield return keyRequestUrl.SendWebRequest();
            if(keyRequestUrl.result == UnityWebRequest.Result.Success)
            {
                Layer.SetKey(keyTokenOrCodeInputField.text);
                yield break;
            }

            //Try input as 'code' query parameter (remove a possible existing code query parameter and add the new one)
            uriBuilder.RemoveQueryParameter("key");
            uriBuilder.RemoveQueryParameter("code");
            uriBuilder.AddQueryParameter("code", keyTokenOrCodeInputField.text);
            var codeRequestUrl = UnityWebRequest.Get(uriBuilder.Uri);
            yield return codeRequestUrl.SendWebRequest();
            if(codeRequestUrl.result == UnityWebRequest.Result.Success)
            {
                Layer.SetCode(keyTokenOrCodeInputField.text);
                yield break;
            }

            //Nothing worked, show error
            serverErrorFeedback.gameObject.SetActive(true);
        }

        public void SetCredentialInputType(int type)
        {
            credentialType = (CredentialType)type;
            SetCredentialInputType(credentialType);
        }

        public void SetCredentialInputType(CredentialType type)
        {
            credentialType = type;

            if(credentialType == CredentialType.UsernamePassword)
            {
                userNameInputField.transform.parent.gameObject.SetActive(true);
                passwordInputField.transform.parent.gameObject.SetActive(true);
                keyTokenOrCodeInputField.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                userNameInputField.transform.parent.gameObject.SetActive(false);
                passwordInputField.transform.parent.gameObject.SetActive(false);
                keyTokenOrCodeInputField.transform.parent.gameObject.SetActive(true);
            }
        }
    }
}
