using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Specialized;
using Netherlands3D.Web;

namespace Netherlands3D.Twin
{
    public enum AuthorizationType
    {
        None = -1,
        UsernamePassword = 0,
        ToBeDetermined = 1, //Single field key, token or code (we dont know specifically yet)
        Key = 2,
        Token = 3,
        Code = 4,
        Unknown = 5
    }

    [CreateAssetMenu(fileName = "KeyVault", menuName = "ScriptableObjects/KeyVault", order = 1)]
    public partial class KeyVault : ScriptableObject
    {
        [TextArea(3, 10)]
        public string Description = "";
        public List<StoredAuthorization> storedAuthorizations = new();
        public List<KnownUrlAuthorizationType> knownUrlAuthorizationTypes = new()
        {
            new KnownUrlAuthorizationType() { baseUrl = "https://tile.googleapis.com/v1/3dtiles/root.json", authorizationType = AuthorizationType.Key },
            new KnownUrlAuthorizationType() { baseUrl = "https://api.pdok.nl/kadaster/3d-basisvoorziening/ogc/v1_0/collections/gebouwen/3dtiles/tileset.json", authorizationType = AuthorizationType.None }
        };

        public bool log = false;
        private MonoBehaviour coroutineMonoBehaviour;
        public UnityEvent<string,AuthorizationType> OnAuthorizationTypeDetermined = new();

        public AuthorizationType GetKnownAuthorizationTypeForURL(string url)
        {
            // Check if the url is known, like Google Maps or PDOK
            foreach (var knownUrlAuthorizationType in knownUrlAuthorizationTypes)
            {
                if (url.StartsWith(knownUrlAuthorizationType.baseUrl))
                {
                    return knownUrlAuthorizationType.authorizationType;
                }
            }

            // Check our own saved credentials.
            foreach (var storedAuthorization in storedAuthorizations)
            {
                if (url.StartsWith(storedAuthorization.url))
                {
                    return storedAuthorization.authorizationType;
                }
            }

            return AuthorizationType.None;
        }


        /// <summary>
        /// Try to find the specific type of credential (key, token or code) that is needed for the layer
        /// </summary>
        public void TryToFindSpecificCredentialType(string url, string key)
        {
            //Only allow one simultaneous coroutine for now
            if(coroutineMonoBehaviour != null)
                Destroy(coroutineMonoBehaviour.gameObject);

            var coroutineGameObject = new GameObject("KeyVaultCoroutines");
            coroutineMonoBehaviour = coroutineGameObject.AddComponent<KeyVaultCoroutines>();
            coroutineMonoBehaviour.StartCoroutine(FindSpecificAuthorizationType(url, key));
        }

        private IEnumerator FindSpecificAuthorizationType(string url, string key)
        {
            // Try a request without credentials
            var noCredentialsRequest = UnityWebRequest.Get(url);
            yield return noCredentialsRequest.SendWebRequest();
            if(noCredentialsRequest.result == UnityWebRequest.Result.Success)
            {
                if(log) Debug.Log("Found no credentials needed for this layer: " + url);
                OnAuthorizationTypeDetermined.Invoke(url,AuthorizationType.None);
                yield break;
            }

            // No key provided, but credentials are needed
            if(key == "")
            {
                Debug.Log("No key provided for this layer: " + url);
                OnAuthorizationTypeDetermined.Invoke(url,AuthorizationType.ToBeDetermined);
                yield break;
            }

            // Try input key as bearer token
            var bearerTokenRequest = UnityWebRequest.Get(url);
            bearerTokenRequest.SetRequestHeader("Authorization", "Bearer " + key);
            yield return bearerTokenRequest.SendWebRequest();
            if(bearerTokenRequest.result == UnityWebRequest.Result.Success)
            {
                if(log) Debug.Log("Found bearer token needed for this layer: " + url);
                OnAuthorizationTypeDetermined.Invoke(url,AuthorizationType.Token);
                yield break;
            }
            
            // Try input key as 'key' query parameter (remove a possible existing key query parameter and add the new one)
            var uriBuilder = new UriBuilder(url);
            var queryParameters = new NameValueCollection();
            uriBuilder.TryParseQueryString(queryParameters);
            uriBuilder.AddQueryParameter("key", key);
            var keyRequestUrl = UnityWebRequest.Get(uriBuilder.Uri);
            yield return keyRequestUrl.SendWebRequest();
            if(keyRequestUrl.result == UnityWebRequest.Result.Success)
            {
                if(log) Debug.Log("Found key needed for this layer: " + url);
                OnAuthorizationTypeDetermined.Invoke(url,AuthorizationType.Key);
                yield break;
            }

            // Try input key as 'code' query parameter (remove a possible existing code query parameter and add the new one)
            uriBuilder.RemoveQueryParameter("key");
            uriBuilder.RemoveQueryParameter("code");
            uriBuilder.AddQueryParameter("code", key);
            var codeRequestUrl = UnityWebRequest.Get(uriBuilder.Uri);
            yield return codeRequestUrl.SendWebRequest();
            if(codeRequestUrl.result == UnityWebRequest.Result.Success)
            {
                if(log) Debug.Log("Found code needed for this layer: " + url);
                OnAuthorizationTypeDetermined.Invoke(url,AuthorizationType.Code);
                yield break;
            }

            Debug.Log("No credential type worked to get access for this layer: " + url);

            // Nothing worked, return unknown
            OnAuthorizationTypeDetermined.Invoke(url,AuthorizationType.Unknown);
        }
    }

    public class KeyVaultCoroutines : MonoBehaviour {}
}