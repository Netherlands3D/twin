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
    public enum CredentialType
    {
        None = -1,
        UsernamePassword = 0,
        KeyTokenOrCode = 1,
        Key = 2,
        Token = 3,
        Code = 4,
        Unknown = 5
    }

    [CreateAssetMenu(fileName = "KeyVault", menuName = "ScriptableObjects/KeyVault", order = 1)]
    public class KeyVault : ScriptableObject
    {
        [TextArea(3, 10)]
        public string Description = "";
        public List<StoredCredentials> storedCredentials = new();
        public List<KnownUrlCredentialType> knownUrlCredentialTypes = new()
        {
            new KnownUrlCredentialType() { baseUrl = "https://tile.googleapis.com/v1/3dtiles/root.json", credentialType = CredentialType.Key },
            new KnownUrlCredentialType() { baseUrl = "https://api.pdok.nl/kadaster/3d-basisvoorziening/ogc/v1_0/collections/gebouwen/3dtiles/tileset.json", credentialType = CredentialType.None }
        };

        public bool log = false;

        public UnityEvent<string,CredentialType> OnCredentialTypeDetermined = new();

        public CredentialType GetKnownCredentialTypeForURL(string url)
        {
            // Check if the url is known, like Google Maps or PDOK
            foreach (var knownUrlCredentialType in knownUrlCredentialTypes)
            {
                if (url.StartsWith(knownUrlCredentialType.baseUrl))
                {
                    return knownUrlCredentialType.credentialType;
                }
            }

            // Check our own saved credentials.
            foreach (var storedCredential in storedCredentials)
            {
                if (url.StartsWith(storedCredential.url))
                {
                    return storedCredential.credentialType;
                }
            }

            return CredentialType.None;
        }

        private MonoBehaviour coroutineMonoBehaviour;

        /// <summary>
        /// Try to find the specific type of credential (key, token or code) that is needed for the layer
        /// </summary>
        public void TryToFindSpecificCredentialType(string url, string key)
        {
            if(coroutineMonoBehaviour == null)
            {
                var coroutineGameObject = new GameObject("KeyVaultCoroutines");
                coroutineMonoBehaviour = coroutineGameObject.AddComponent<MonoBehaviour>();
            }
            coroutineMonoBehaviour.StartCoroutine(FindSpecificCredentialType(url, key));
        }

        private IEnumerator FindSpecificCredentialType(string url, string key)
        {
            // Try a request without credentials
            var noCredentialsRequest = UnityWebRequest.Get(url);
            yield return noCredentialsRequest.SendWebRequest();
            if(noCredentialsRequest.result == UnityWebRequest.Result.Success)
            {
                if(log) Debug.Log("Found no credentials needed for this layer: " + url);
                OnCredentialTypeDetermined.Invoke(url,CredentialType.None);
                yield break;
            }

            // Try input key as bearer token
            var bearerTokenRequest = UnityWebRequest.Get(url);
            bearerTokenRequest.SetRequestHeader("Authorization", "Bearer " + key);
            yield return bearerTokenRequest.SendWebRequest();
            if(bearerTokenRequest.result == UnityWebRequest.Result.Success)
            {
                if(log) Debug.Log("Found bearer token needed for this layer: " + url);
                OnCredentialTypeDetermined.Invoke(url,CredentialType.Token);
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
                OnCredentialTypeDetermined.Invoke(url,CredentialType.Key);
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
                OnCredentialTypeDetermined.Invoke(url,CredentialType.Code);
                yield break;
            }

            Debug.Log("No credential type worked to get access for this layer: " + url);

            // Nothing worked, return unknown
            OnCredentialTypeDetermined.Invoke(url,CredentialType.Unknown);
        }

        [System.Serializable]
        public class KnownUrlCredentialType
        {
            public string baseUrl;
            public CredentialType credentialType;
        }

        [System.Serializable]
        public class StoredCredentials
        {
            public string url = "";

            public string username = "";
            public string password = "";
            public string key = "";

            public CredentialType credentialType = CredentialType.None;
        }
    }
    
}