using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Networking;
using System;
using System.Collections;
using Netherlands3D.Web;
using Netherlands3D.Credentials.StoredAuthorization;

namespace Netherlands3D.Credentials
{
    [Obsolete("this enum will be removed in the future, use a type check instead")]
    public enum AuthorizationType
    {
        //Specific order in items used in dropdown index
        Public = -1,
        UsernamePassword = 0,
        InferableSingleKey = 1, //Single field key, token or code (we dont know specifically yet but can infer it)
        Key,
        BearerToken,
        Code,
        Token,
        FailedOrUnsupported
    }

    [CreateAssetMenu(fileName = "KeyVault", menuName = "ScriptableObjects/KeyVault", order = 1)]
    public class KeyVault : ScriptableObject
    {
        [TextArea(3, 10)] public string Description = "";

        public List<KnownUrlAuthorizationType> knownUrlAuthorizationTypes = new()
        {
            new KnownUrlAuthorizationType() { baseUrl = "https://tile.googleapis.com/v1/3dtiles/root.json", authorizationType = AuthorizationType.InferableSingleKey },
            new KnownUrlAuthorizationType() { baseUrl = "https://engine.tygron.com/web/3dtiles/tileset.json", authorizationType = AuthorizationType.InferableSingleKey },
            new KnownUrlAuthorizationType() { baseUrl = "https://api.pdok.nl/kadaster/3d-basisvoorziening/ogc/v1_0/collections/gebouwen/3dtiles/tileset.json", authorizationType = AuthorizationType.Public }
        };

        public Dictionary<Uri, StoredAuthorization.StoredAuthorization> storedAuthorizations = new();

        public bool log = false;
        private MonoBehaviour coroutineMonoBehaviour;
        public UnityEvent<StoredAuthorization.StoredAuthorization> OnAuthorizationTypeDetermined = new();

        /// <summary>
        /// Get the stored authorization for a specific URL
        /// </summary>

        public void Authorize(Uri uri, string username, string passwordOrKey)
        {
            if (storedAuthorizations.TryGetValue(uri, out var authorization))
            {
                OnAuthorizationTypeDetermined.Invoke(authorization);
                return;
            }

            TryBasicAuthentication(uri, username, passwordOrKey);
            TryToFindSpecificCredentialType(uri, passwordOrKey);
        }

        public AuthorizationType GetKnownAuthorizationTypeForURL(Uri uri)
        {
            if (log) Debug.Log("GetKnownAuthorizationTypeForURL for: " + uri);
            // Check if the url is known, like Google Maps or PDOK
            foreach (var knownUrlAuthorizationType in knownUrlAuthorizationTypes)
            {
                if (uri.Equals(knownUrlAuthorizationType.baseUrl))
                {
                    return knownUrlAuthorizationType.authorizationType;
                }
            }

            // Check our own saved credentials.
            if (storedAuthorizations.ContainsKey(uri))
                return storedAuthorizations[uri].AuthorizationType;

            return AuthorizationType.Public;
        }

        /// <summary>
        /// Add a new known URL with a specific authorization type
        /// </summary>
        private void NewURLAuthorizationDetermined(Uri uri, AuthorizationType authorizationType, string username = "", string password = "", string key = "")
        {
            ClearURLFromStoredAuthorizations(uri);

            StoredAuthorization.StoredAuthorization auth = new FailedOrUnsupported(uri);
            switch (authorizationType)
            {
                case AuthorizationType.Public:
                    auth = new Public(uri);
                    storedAuthorizations.Add(uri, auth);
                    break;
                case AuthorizationType.UsernamePassword:
                    auth = new UsernamePassword(uri, username, password);
                    storedAuthorizations.Add(uri, auth);
                    break;
                case AuthorizationType.InferableSingleKey:
                    auth = new InferableSingleKey(uri, key);
                    storedAuthorizations.Add(uri, auth);
                    break;
                case AuthorizationType.Key:
                    auth = new Key(uri, key);
                    storedAuthorizations.Add(uri, auth);
                    break;
                case AuthorizationType.BearerToken:
                    auth = new BearerToken(uri, key);
                    storedAuthorizations.Add(uri, auth);
                    break;
                case AuthorizationType.Code:
                    auth = new Code(uri, key);
                    storedAuthorizations.Add(uri, auth);
                    break;
                case AuthorizationType.Token:
                    auth = new Token(uri, key);
                    storedAuthorizations.Add(uri, auth);
                    break;
                case AuthorizationType.FailedOrUnsupported:
                    // storedAuthorizations.Add(uri, auth); // todo: do we want to store a failed authorization, or try again next time this url is passed? 
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(authorizationType), authorizationType, "authorisation type not defined");
            }

            OnAuthorizationTypeDetermined.Invoke(auth);
        }

        public void ClearURLFromStoredAuthorizations(Uri url)
        { 
            storedAuthorizations.Remove(url);
        }

        /// <summary>
        /// Try to find the specific type of credential (key, token or code) that is needed for the layer
        /// </summary>
        public void TryToFindSpecificCredentialType(Uri uri, string key)
        {
            //Only allow one simultaneous coroutine for now
            if (coroutineMonoBehaviour != null)
                Destroy(coroutineMonoBehaviour.gameObject);

            var coroutineGameObject = new GameObject("KeyVaultCoroutine_FindSpecificAuthorizationType");
            coroutineMonoBehaviour = coroutineGameObject.AddComponent<KeyVaultCoroutines>();
            coroutineMonoBehaviour.StartCoroutine(FindSpecificAuthorizationType(uri, key));
        }

        /// <summary>
        /// Try to access a URL with a username and password.
        /// OnAuthorizationTypeDetermined will be called with the result.
        /// </summary>
        public void TryBasicAuthentication(Uri uri, string username, string password)
        {
            //Only allow one simultaneous coroutine for now
            if (coroutineMonoBehaviour != null)
                Destroy(coroutineMonoBehaviour.gameObject);

            var coroutineGameObject = new GameObject("KeyVaultCoroutine_AccessWithUsernameAndPassword");
            coroutineMonoBehaviour = coroutineGameObject.AddComponent<KeyVaultCoroutines>();
            coroutineMonoBehaviour.StartCoroutine(AccessWithUsernameAndPassword(uri, username, password));
        }

        private IEnumerator AccessWithUsernameAndPassword(Uri uri, string username, string password)
        {
            var request = UnityWebRequest.Get(uri);
            request.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(username + ":" + password)));
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (log) Debug.Log("Access granted with username and password for: " + uri);
                NewURLAuthorizationDetermined(uri, AuthorizationType.UsernamePassword, username: username, password: password);
            }
            else
            {
                Debug.LogError("Access denied with username and password for: " + uri);
                //todo: send an event here so UI can update
            }
        }

        private IEnumerator FindSpecificAuthorizationType(Uri uri, string key)
        {
            AuthorizationType foundType = AuthorizationType.FailedOrUnsupported;

            //Start with resetting this url history to unkown
            ClearURLFromStoredAuthorizations(uri);

            // Try a request without credentials
            var noCredentialsRequest = UnityWebRequest.Get(uri);
            yield return noCredentialsRequest.SendWebRequest();
            if (noCredentialsRequest.result == UnityWebRequest.Result.Success)
            {
                if (log) Debug.Log("Found no credentials needed for this layer: " + uri);
                foundType = AuthorizationType.Public;
                NewURLAuthorizationDetermined(uri, foundType);
                yield break;
            }
            
            // Try input key as bearer token
            var bearerTokenRequest = UnityWebRequest.Get(uri);
            bearerTokenRequest.SetRequestHeader("Authorization", "Bearer " + key);
            yield return bearerTokenRequest.SendWebRequest();
            if (bearerTokenRequest.result == UnityWebRequest.Result.Success)
            {
                if (log) Debug.Log("Found bearer token needed for this layer: " + uri);
                foundType = AuthorizationType.BearerToken;
                NewURLAuthorizationDetermined(uri, foundType, key: key);
                yield break;
            }

            // Try input key as 'key' query parameter (remove a possible existing key query parameter and add the new one)
            var uriBuilder = new UriBuilder(uri);
            uriBuilder.AddQueryParameter("key", key);
            var keyRequestUrl = UnityWebRequest.Get(uriBuilder.Uri);
            yield return keyRequestUrl.SendWebRequest();
            if (keyRequestUrl.result == UnityWebRequest.Result.Success)
            {
                if (log) Debug.Log("Found key needed for this layer: " + uri);
                foundType = AuthorizationType.Key;
                NewURLAuthorizationDetermined(uri, foundType, key: key);
                yield break;
            }

            // Try input key as 'code' query parameter (remove a possible existing code query parameter and add the new one)
            uriBuilder.RemoveQueryParameter("key");
            uriBuilder.RemoveQueryParameter("code");
            uriBuilder.AddQueryParameter("code", key);
            var codeRequestUrl = UnityWebRequest.Get(uriBuilder.Uri);
            yield return codeRequestUrl.SendWebRequest();
            if (codeRequestUrl.result == UnityWebRequest.Result.Success)
            {
                if (log) Debug.Log("Found code needed for this layer: " + uri);
                foundType = AuthorizationType.Code;
                NewURLAuthorizationDetermined(uri, foundType, key: key);
                yield break;
            }

            // Try input key as 'token' query parameter (remove a possible existing code query parameter and add the new one)
            uriBuilder.RemoveQueryParameter("code");
            uriBuilder.AddQueryParameter("token", key);
            var tokenRequestUrl = UnityWebRequest.Get(uriBuilder.Uri);
            yield return tokenRequestUrl.SendWebRequest();
            if (tokenRequestUrl.result == UnityWebRequest.Result.Success)
            {
                if (log) Debug.Log("Found token needed for this layer: " + uri);
                foundType = AuthorizationType.Token;
                NewURLAuthorizationDetermined(uri, foundType, key: key);
                yield break;
            }
            
            // Nothing worked, return unsupported
            Debug.Log("Invalid credentials provided or no supported credential type worked to get access for this layer: " + uri);
            NewURLAuthorizationDetermined(uri, AuthorizationType.FailedOrUnsupported);
        }
    }

    public class KeyVaultCoroutines : MonoBehaviour
    {
    }
}