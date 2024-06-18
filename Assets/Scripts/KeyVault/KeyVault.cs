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
        //Specific order in items used in dropdown index
        Public = -1,
        UsernamePassword = 0,
        Guess = 1, //Single field key, token or code (we dont know specifically yet)
        Key,
        BearerToken,
        Code,
        Token,
        Unknown
    }

    [CreateAssetMenu(fileName = "KeyVault", menuName = "ScriptableObjects/KeyVault", order = 1)]
    public class KeyVault : ScriptableObject
    {
        [TextArea(3, 10)]
        public string Description = "";
        public List<KnownUrlAuthorizationType> knownUrlAuthorizationTypes = new()
        {
            new KnownUrlAuthorizationType() { baseUrl = "https://tile.googleapis.com/v1/3dtiles/root.json", authorizationType = AuthorizationType.Key },
            new KnownUrlAuthorizationType() { baseUrl = "https://engine.tygron.com/web/3dtiles/tileset.json", authorizationType = AuthorizationType.Token },
            new KnownUrlAuthorizationType() { baseUrl = "https://api.pdok.nl/kadaster/3d-basisvoorziening/ogc/v1_0/collections/gebouwen/3dtiles/tileset.json", authorizationType = AuthorizationType.Public }
        };
        public List<StoredAuthorization> storedAuthorizations = new();

        public bool log = false;
        private MonoBehaviour coroutineMonoBehaviour;
        public UnityEvent<string,AuthorizationType> OnAuthorizationTypeDetermined = new();

        /// <summary>
        /// Get the stored authorization for a specific URL
        /// </summary>
        public StoredAuthorization GetStoredAuthorization(string url)
        {
            return storedAuthorizations.Find(x => x.url == url);
        }

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

            return AuthorizationType.Public;
        }

        /// <summary>
        /// Add a new known URL with a specific authorization type
        /// </summary>
        public void NewURLAuthorizationDetermined(string url, AuthorizationType authorizationType, string username = "", string password = "", string key = "")
        {
            ClearURLFromStoredAuthorizations(url);

            storedAuthorizations.Add(
                new StoredAuthorization() { 
                    url = url, 
                    authorizationType = authorizationType, 
                    username = username, 
                    password = password, 
                    key = key 
                });

            OnAuthorizationTypeDetermined.Invoke(url, authorizationType);
        }

        public void ClearURLFromStoredAuthorizations(string url)
        {
            if(storedAuthorizations.Exists(x => x.url == url))
                storedAuthorizations.RemoveAll(x => x.url == url);
        }

        /// <summary>
        /// Try to find the specific type of credential (key, token or code) that is needed for the layer
        /// </summary>
        public void TryToFindSpecificCredentialType(string url, string key)
        {
            //Only allow one simultaneous coroutine for now
            if(coroutineMonoBehaviour != null)
                Destroy(coroutineMonoBehaviour.gameObject);

            var coroutineGameObject = new GameObject("KeyVaultCoroutine_FindSpecificAuthorizationType");
            coroutineMonoBehaviour = coroutineGameObject.AddComponent<KeyVaultCoroutines>();
            coroutineMonoBehaviour.StartCoroutine(FindSpecificAuthorizationType(url, key));
        }

        /// <summary>
        /// Try to access a URL with a username and password.
        /// OnAuthorizationTypeDetermined will be called with the result.
        /// </summary>
        public void TryBasicAuthentication(string url, string username, string password)
        {
            //Only allow one simultaneous coroutine for now
            if(coroutineMonoBehaviour != null)
                Destroy(coroutineMonoBehaviour.gameObject);

            var coroutineGameObject = new GameObject("KeyVaultCoroutine_AccessWithUsernameAndPassword");
            coroutineMonoBehaviour = coroutineGameObject.AddComponent<KeyVaultCoroutines>();
            coroutineMonoBehaviour.StartCoroutine(AccessWithUsernameAndPassword(url,username, password));
        }

        private IEnumerator AccessWithUsernameAndPassword(string url, string username,string password)
        {
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(username + ":" + password)));
            yield return request.SendWebRequest();

            if(request.result == UnityWebRequest.Result.Success)
            {
                if(log) Debug.Log("Access granted with username and password for: " + url);
                NewURLAuthorizationDetermined(url, AuthorizationType.UsernamePassword, username: username, password: password);
            }
            else
            {
                Debug.LogError("Access denied with username and password for: " + url);
            }
        }

        private IEnumerator FindSpecificAuthorizationType(string url, string key)
        {
            url = url.TrimEnd('?', '&');

            AuthorizationType foundType = AuthorizationType.Unknown;
            
            //Start with resetting this url history to unkown
            ClearURLFromStoredAuthorizations(url);

            // Try a request without credentials
            var noCredentialsRequest = UnityWebRequest.Get(url);
            yield return noCredentialsRequest.SendWebRequest();
            if(noCredentialsRequest.result == UnityWebRequest.Result.Success)
            {
                if(log) Debug.Log("Found no credentials needed for this layer: " + url);
                foundType = AuthorizationType.Public;
                NewURLAuthorizationDetermined(url, foundType);
                yield break;
            }

            // No key provided, but credentials are needed
            if(key == "")
            {
                Debug.Log("No key provided for this layer: " + url);
                foundType = AuthorizationType.Guess;
                NewURLAuthorizationDetermined(url, foundType);
                yield break;
            }

            // Try input key as bearer token
            var bearerTokenRequest = UnityWebRequest.Get(url);
            bearerTokenRequest.SetRequestHeader("Authorization", "Bearer " + key);
            yield return bearerTokenRequest.SendWebRequest();
            if(bearerTokenRequest.result == UnityWebRequest.Result.Success)
            {
                if(log) Debug.Log("Found bearer token needed for this layer: " + url);
                foundType = AuthorizationType.BearerToken;
                NewURLAuthorizationDetermined(url, foundType, key: key);
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
                foundType = AuthorizationType.Key;
                NewURLAuthorizationDetermined(url, foundType, key: key);
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
                foundType = AuthorizationType.Code;
                NewURLAuthorizationDetermined(url, foundType, key: key);
                yield break;
            }

            // Try input key as 'token' query parameter (remove a possible existing code query parameter and add the new one)
            uriBuilder.RemoveQueryParameter("code");
            uriBuilder.AddQueryParameter("token", key);
            var tokenRequestUrl = UnityWebRequest.Get(uriBuilder.Uri);
            yield return tokenRequestUrl.SendWebRequest();
            if(tokenRequestUrl.result == UnityWebRequest.Result.Success)
            {
                if(log) Debug.Log("Found token needed for this layer: " + url);
                foundType = AuthorizationType.Token;
                NewURLAuthorizationDetermined(url, foundType, key: key);
                yield break;
            }

            // Nothing worked, return unknown
            Debug.Log("No credential type worked to get access for this layer: " + url);
            NewURLAuthorizationDetermined(url, AuthorizationType.Unknown);
        }
    }

    public class KeyVaultCoroutines : MonoBehaviour {}
}