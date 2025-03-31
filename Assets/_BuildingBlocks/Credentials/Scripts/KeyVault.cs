using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Networking;
using System;
using System.Collections;
using Netherlands3D.Web;
using Netherlands3D.Credentials.StoredAuthorization;
using System.Collections.Specialized;
using KindMen.Uxios;
using KindMen.Uxios.Errors;
using KindMen.Uxios.Errors.Http;

namespace Netherlands3D.Credentials
{
    [CreateAssetMenu(fileName = "KeyVault", menuName = "ScriptableObjects/KeyVault", order = 1)]
    public class KeyVault : ScriptableObject
    {
        [TextArea(3, 10)] public string Description = "";
        [SerializeField] private bool log = false;
        [SerializeField] private Dictionary<Uri, StoredAuthorization.StoredAuthorization> storedAuthorizations = new();

        private MonoBehaviour coroutineMonoBehaviour;
        public UnityEvent<StoredAuthorization.StoredAuthorization> OnAuthorizationTypeDetermined = new();

        public Dictionary<string, Type> expectedAuthorizationTypes = new()
        {
            { "tile.googleapis.com", typeof(Key) },
            { "engine.tygron.com", typeof(Token) },
            { "geo.tygron.com", typeof(Token) }
        };

        private static readonly Dictionary<Type, Func<Uri, string[], StoredAuthorization.StoredAuthorization>> supportedAuthorizationTypes = new()
        {
            { typeof(Public), (uri, args) => new Public(uri) }, //start with public, to not add query parameters if not needed
            { typeof(Key), (uri, args) => new Key(uri, args[0]) },
            { typeof(Code), (uri, args) => new Code(uri, args[0]) },
            { typeof(Token), (uri, args) => new Token(uri, args[0]) },
            { typeof(UsernamePassword), (uri, args) => new UsernamePassword(uri, args[1], args[0]) }, //order is Password before Username, because we need a consistent order to use passwordOrKey
            { typeof(BearerToken), (uri, args) => new BearerToken(uri, args[0]) }
        };

        private StoredAuthorization.StoredAuthorization CreateStoredAuthorization(Type storedAuthorizationType, Uri uri, params string[] args)
        {
            if (!storedAuthorizationType.IsSubclassOf(typeof(StoredAuthorization.StoredAuthorization)))
                throw new InvalidOperationException($"Unsupported authorization type: {storedAuthorizationType.Name}");

            if (supportedAuthorizationTypes.TryGetValue(storedAuthorizationType, out var factory))
            {
                // var auth = Activator.CreateInstance(storedAuthorizationType, args);
                var auth = factory(uri, args);
                return auth;
            }

            throw new InvalidOperationException($"Unsupported authorization type: {storedAuthorizationType.Name}");
        }

        private StoredAuthorization.StoredAuthorization CreateStoredAuthorization<T>(Uri uri, params string[] args) where T : StoredAuthorization.StoredAuthorization
        {
            if (supportedAuthorizationTypes.TryGetValue(typeof(T), out var factory))
            {
                var auth = factory(uri, args);
                return auth;
            }

            throw new InvalidOperationException($"Unsupported authorization type: {typeof(T).Name}");
        }

        /// <summary>
        /// Get the stored authorization for a specific URL
        /// </summary>
        public void Authorize(Uri inputUri, string username, string passwordOrKey)
        {
            var domain = new Uri(inputUri.GetLeftPart(UriPartial.Path));

            //check if we already have an authorization for this url 
            if (storedAuthorizations.TryGetValue(domain, out var authorization))
            {
                OnAuthorizationTypeDetermined.Invoke(authorization);
                return;
            }

            //Only allow one simultaneous coroutine for now
            if (coroutineMonoBehaviour != null)
                Destroy(coroutineMonoBehaviour.gameObject);

            //lets try to to find a valid authorization type for the given input url
            var coroutineGameObject = new GameObject("KeyVaultCoroutine_TryParseAuthorizationInput");
            coroutineMonoBehaviour = coroutineGameObject.AddComponent<KeyVaultCoroutines>();
            coroutineMonoBehaviour.StartCoroutine(TryFindAuthorization(inputUri, username, passwordOrKey));
        }

        private IEnumerator TryFindAuthorization(Uri inputUri, string username, string passwordOrKey)
        {
            bool authorizationSuccessful = false;
            Uri domain = new Uri(inputUri.GetLeftPart(UriPartial.Path));

            //1. Try to find credentials in the url, and if found, we verify that this is a valid Auth method
            if (TryToFindAuthorizationInUriQuery(inputUri, out var potentialAuthorisation))
            {
                if (log) Debug.Log("found potential query key type in url: " + potentialAuthorisation.GetType() + " with key " + potentialAuthorisation.QueryKeyValue);

                //try this one
                yield return TrySupportedAuthorization(potentialAuthorisation, inputUri, username, potentialAuthorisation.QueryKeyValue);
                if (storedAuthorizations.ContainsKey(domain)) yield break; // if the Auth test was succesful, stop looking.
            }

            //2. In case we know the type for this base Uri, try that first
            if (expectedAuthorizationTypes.TryGetValue(domain, out var expectedType))
            {
                if (expectedType != typeof(Public) && string.IsNullOrEmpty(passwordOrKey)) //it's not public, so we need some kind of authorization. if the passwordOrKey is empty, we already know it will fail. Maybe expand this in the future with a more robust check
                {
                    OnAuthorizationTypeDetermined.Invoke(new FailedOrUnsupported(inputUri));
                    yield break;
                }

                var potentialAuth = CreateStoredAuthorization(expectedType, inputUri, username, passwordOrKey);
                yield return TrySupportedAuthorization(potentialAuth, inputUri, username, passwordOrKey);
                if (storedAuthorizations.ContainsKey(domain)) yield break; // if the Auth test was succesful, stop looking.
            }

            //3. Try all supported authorization types
            yield return TryAllSupportedAuthorizations(inputUri, username, passwordOrKey);
            if (storedAuthorizations.ContainsKey(domain)) yield break; // if the Auth test was succesful, stop looking.

            //4. nothing worked, this url either has invalid credentials, or we don't support the credential type. We will not store this in the storedAuthorizations, so the user can retry
            if (log) Debug.Log("This url either has invalid credentials, or we don't support the credential type: " + inputUri);
            OnAuthorizationTypeDetermined.Invoke(new FailedOrUnsupported(inputUri));
        }

        private bool TryToFindAuthorizationInUriQuery(Uri uri, out QueryStringAuthorization potentialAuthorisation)
        {
            var queryParameters = new NameValueCollection();
            uri.TryParseQueryString(queryParameters);

            foreach (var supportedAuthTypes in supportedAuthorizationTypes)
            {
                if (!supportedAuthTypes.Key.IsSubclassOf(typeof(QueryStringAuthorization))) //only check our supported auth types that could have the auth in the url.
                    continue;

                potentialAuthorisation = supportedAuthTypes.Value(uri, new[] { "" }) as QueryStringAuthorization; //unfortunately we have to create a temp instance to access QueryKeyName
                var authString = queryParameters.Get(potentialAuthorisation.QueryKeyName);

                if (!string.IsNullOrEmpty(authString))
                {
                    potentialAuthorisation = supportedAuthTypes.Value(uri, new[] { authString }) as QueryStringAuthorization; //set the out variable to have the parsed auth string
                    return true;
                }
            }

            potentialAuthorisation = null;
            return false;
        }

        private IEnumerator TryAllSupportedAuthorizations(Uri uri, string username, string passwordOrKey)
        {
            foreach (var supportedAuthorization in supportedAuthorizationTypes.Keys)
            {
                var potentialAuth = CreateStoredAuthorization(supportedAuthorization, uri, username, passwordOrKey);
                yield return TrySupportedAuthorization(potentialAuth, uri, username, passwordOrKey);
                if (storedAuthorizations.ContainsKey(uri)) yield break; // if the Auth test was succesful, stop looking.
            }
        }

        private IEnumerator TrySupportedAuthorization(StoredAuthorization.StoredAuthorization potentialAuth, Uri uri, string username, string passwordOrKey)
        {
            var config = Config.Default();
            config = potentialAuth.AddToConfig(config);
            var request = Uxios.DefaultInstance.Get<byte[]>(uri, config); //get as byte[] so Uxios doesn't try to interpret the response

            bool isAuthorized = false;

            request.Then(response => { isAuthorized = true; });
            request.Catch(exception => { isAuthorized = CheckErrorAuthentication(exception); });

            yield return Uxios.WaitForRequest(request);

            if (isAuthorized)
            {
                if (log) Debug.Log("Access granted with authorization type: " + potentialAuth.GetType() + " for: " + uri);
                NewAuthorizationDetermined(potentialAuth);
            }
            else
            {
                if (log) Debug.Log("Access denied with authorization type: " + potentialAuth.GetType() + " for: " + uri);
                //todo: send an event here so UI can update
            }
        }

        private static bool CheckErrorAuthentication(Exception exception)
        {
            switch (exception)
            {
                case AuthenticationError:
                    return false;
                case HttpClientError:
                    return true;
                case HttpServerError error:
                    throw new Exception("the request returned a response that is not implemented: " + error.Status + " from Uri: " + error.Config.Url);
                default:
                    throw new Exception("the request returned an connection or data processing error: " + exception.Message + "from Uri: " + ((Error)exception).Config.Url);
            }
        }

        /// <summary>
        /// Add a new known URL with a specific authorization type
        /// </summary>
        private void NewAuthorizationDetermined(StoredAuthorization.StoredAuthorization auth) //called when a authorization attempt was successful and stores it for future easy access via its baseUri
        {
            storedAuthorizations.Remove(auth.Domain);
            storedAuthorizations.Add(auth.Domain, auth);
            OnAuthorizationTypeDetermined.Invoke(auth);
        }

        private static bool IsAuthorized(UnityWebRequest uwr)
        {
            if (uwr.result == UnityWebRequest.Result.Success)
                return true;

            if (uwr.responseCode == 401 || uwr.responseCode == 403)
                return false;

            if (uwr.result != UnityWebRequest.Result.ProtocolError)
                throw new Exception("the request returned an connection or data processing error: " + uwr.responseCode + "from Uri: " + uwr.uri);

            if (uwr.responseCode >= 500)
                throw new Exception("the request returned a response that is not implemented: " + uwr.responseCode + " from Uri: " + uwr.uri);

            // We kinda assume that anything below error code 500 -except for 401 and 403- would probably be OK since the server
            // has processed the request and deemed it to contain a client side error
            return true;
        }
    }

    public class KeyVaultCoroutines : MonoBehaviour
    {
    }
}