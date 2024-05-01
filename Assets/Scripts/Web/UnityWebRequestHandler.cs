using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Netherlands3D.Web
{
    /// <summary>
    /// Message handler that specifically uses Unity's web request system to send requests.
    /// This handler is part of a chain of responsibility pattern where each class in the chain
    /// either handles the request or delegates to the next handler in the chain.
    /// </summary>
    public class UnityWebRequestHandler : DelegatingHandler
    {
        private readonly UnityWebRequestTransport unityWebRequestTransport;

        public UnityWebRequestHandler(DelegatingHandler innerHandler) : base(innerHandler)
        {
            GameObject coroutineObject = new GameObject("CoroutineHelper");
            unityWebRequestTransport = coroutineObject.AddComponent<UnityWebRequestTransport>();
        }
        
        public override Task<HttpResponseMessage<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return unityWebRequestTransport.SendAsync<T>(request, cancellationToken);
        }
    }
}