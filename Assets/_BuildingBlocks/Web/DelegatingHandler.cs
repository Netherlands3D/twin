using System.Threading;
using System.Threading.Tasks;

namespace Netherlands3D.Web
{
    /// <summary>
    /// Base class for a message handler that delegates to another message handler.
    /// 
    /// The main task of a DelegatingHandler is to serve as a middleware which can perform actions on an HTTP request,
    /// response, or both, before and/or after the request is processed, and then either pass along the
    /// HttpRequestMessage/HttpResponseMessage or handle them itself.
    /// </summary>
    public abstract class DelegatingHandler : IMessageHandler
    {
        public IMessageHandler InnerHandler { get; set; }

        protected DelegatingHandler(IMessageHandler innerHandler)
        {
            this.InnerHandler = innerHandler;
        }

        public virtual Task<HttpResponseMessage<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return InnerHandler?.SendAsync<T>(request, cancellationToken);
        }

    }
}