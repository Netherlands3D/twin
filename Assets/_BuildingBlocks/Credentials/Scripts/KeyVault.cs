using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using System.Collections;
using Netherlands3D.Credentials.StoredAuthorization;
using KindMen.Uxios;
using KindMen.Uxios.Errors;
using KindMen.Uxios.Errors.Http;
using RSG;

namespace Netherlands3D.Credentials
{
    [CreateAssetMenu(fileName = "KeyVault", menuName = "ScriptableObjects/KeyVault", order = 1)]
    public class KeyVault : ScriptableObject
    {
        [TextArea(3, 10)] public string Description = "";
        [SerializeField] private bool log = false;
        private Dictionary<Uri, StoredAuthorization.StoredAuthorization> storedAuthorizations = new();

        public UnityEvent<StoredAuthorization.StoredAuthorization> OnAuthorizationTypeDetermined = new();
        public UnityEvent<StoredAuthorization.StoredAuthorization> OnAuthorizationTypeDeterminationFailed = new();

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
                return factory(uri, args);
            }

            throw new InvalidOperationException($"Unsupported authorization type: {storedAuthorizationType.Name}");
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

            KeyVaultCoroutineRunner
                .Instance()
                .StartCoroutine(TryFindAuthorization(inputUri, username, passwordOrKey));
        }

        private IEnumerator TryFindAuthorization(Uri inputUri, string username, string passwordOrKey)
        {
            Uri domain = new Uri(inputUri.GetLeftPart(UriPartial.Path));

            // 1. Try to find credentials in the url, and if found, we verify that this is a valid Auth method
            if (TryToFindAuthorizationInUriQuery(inputUri, out var potentialAuthorisation))
            {
                if (log) Debug.Log(
                    $"Found potential query key type in url: {potentialAuthorisation.GetType()} with key {potentialAuthorisation.QueryKeyValue}"
                );
                
                var request = TrySupportedAuthorization(potentialAuthorisation, inputUri);
                
                // Needed because this is all coroutines at the moment
                yield return Uxios.WaitForRequest(request);

                if (storedAuthorizations.ContainsKey(domain)) yield break; // if the Auth test was succesful, stop looking.
            }

            // 2. In case we know the type for this base Uri, try that first
            if (expectedAuthorizationTypes.TryGetValue(domain.Host, out var expectedType))
            {
                // if it's not public, so we need some kind of authorization. if the passwordOrKey is empty, we already know it will fail. Maybe expand this in the future with a more robust check
                if (expectedType != typeof(Public) && string.IsNullOrEmpty(passwordOrKey))
                {
                    OnAuthorizationTypeDetermined.Invoke(new FailedOrUnsupported(inputUri));
                    yield break;
                }

                // pass usernameOrKey before username for constructor order
                var potentialAuth = CreateStoredAuthorization(expectedType, inputUri, passwordOrKey, username);
                var request = TrySupportedAuthorization(potentialAuth, inputUri);
                
                // Needed because this is all coroutines at the moment
                yield return Uxios.WaitForRequest(request);

                // if the Auth test was succesful, stop looking.
                if (storedAuthorizations.ContainsKey(domain)) yield break; 
            }

            // 3. Try all supported authorization types
            yield return TryAllSupportedAuthorizations(inputUri, username, passwordOrKey);
            if (storedAuthorizations.ContainsKey(domain)) yield break; // if the Auth test was succesful, stop looking.

            // 4. nothing worked, this url either has invalid credentials, or we don't support the credential type. We will not store this in the storedAuthorizations, so the user can retry
            if (log) Debug.Log("This url either has invalid credentials, or we don't support the credential type: " + inputUri);
            OnAuthorizationTypeDetermined.Invoke(new FailedOrUnsupported(inputUri));
        }

        private bool TryToFindAuthorizationInUriQuery(Uri uri, out QueryStringAuthorization potentialAuthorisation)
        {
            var queryParameters = QueryString.Decode(uri.Query);

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
                // pass usernameOrKey before username for constructor order
                var potentialAuth = CreateStoredAuthorization(
                    supportedAuthorization, uri, passwordOrKey, username
                ); 

                var promisedAuthorization = TrySupportedAuthorization(potentialAuth, uri) as Promise<IResponse>;

                yield return Uxios.WaitForRequest(promisedAuthorization);
                if (promisedAuthorization == null)
                {
                    throw new Exception("Expected promisedAuthorization to be casted to Promise<StoredAuthorization.StoredAuthorization>, but got null");
                }

                // if the Auth test was successful, stop looking.
                if (storedAuthorizations.ContainsKey(potentialAuth.Domain)) yield break; // if the Auth test was succesful, stop looking.
            }
        }

        private IPromise<IResponse> TrySupportedAuthorization(
            StoredAuthorization.StoredAuthorization potentialAuth, 
            Uri uri
        ) {
            void OnAccepted(IResponse _)
            {
                if (log) Debug.Log($"Access GRANTED for {uri} with authorization type: {potentialAuth.GetType()}");
                NewAuthorizationDetermined(potentialAuth);
            }

            void OnError(Exception exception)
            {
                if (CheckErrorAuthentication(exception))
                {
                    OnAccepted((exception as Error)?.Response);
                    return;
                }

                if (log) Debug.Log($"Access DENIED for {uri} with authorization type: {potentialAuth.GetType()}");
                OnAuthorizationTypeDeterminationFailed.Invoke(potentialAuth);
            }

            var config = potentialAuth.AddToConfig(Config.Default());
            var request = Uxios.DefaultInstance.Get<byte[]>(uri, config);
            request.Then(OnAccepted);
            request.Catch(OnError);

            return request;
        }

        private static bool CheckErrorAuthentication(Exception exception)
        {
            return exception switch
            {
                AuthenticationError => false,
                HttpClientError => true,
                HttpServerError error => throw new Exception(
                    $"the request returned a response that is not implemented: {error.Status} from Uri: {error.Config.Url}"
                ),
                _ => throw new Exception(
                    $"the request returned an connection or data processing error: {exception.Message} from Uri: {((Error)exception).Config.Url}"
                )
            };
        }

        /// <summary>
        /// Add a new known URL with a specific authorization type.
        ///
        /// Called when a authorization attempt was successful and stores it for future easy access via its baseUri
        /// </summary>
        private void NewAuthorizationDetermined(StoredAuthorization.StoredAuthorization auth)
        {
            storedAuthorizations.Remove(auth.Domain);
            storedAuthorizations.Add(auth.Domain, auth);
            OnAuthorizationTypeDetermined.Invoke(auth);
        }
    }
}