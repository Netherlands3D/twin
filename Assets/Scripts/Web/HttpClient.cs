using System;
using System.Threading;
using System.Threading.Tasks;

namespace Netherlands3D.Web
{
    /// <summary>
    /// HttpClient class is used to send HTTP requests through a middleware stack.
    /// It can be customized to accommodate for differing needs of each type of system
    /// that requires a common set of actions, e.g., authorization, logging, retrying etc.
    /// Custom middlewares can be added using AddHandler method, forming a chain of 'middlewares'.
    /// For example, to provide credentials with each request, one could add a specialized handler
    /// that adds these credentials to each request. By using these handlers, 
    /// each request can be properly tailored towards its intended use.
    /// </summary>
    public class HttpClient
    {
        private IMessageHandler innerHandler = new UnityWebRequestHandler(null);

        /// <summary>
        /// Adds a middleware to the current HttpClient instance.
        /// </summary>
        /// <param name="messageHandler">
        /// An instance of a class that implement IMessageHandler interface. This class will work as a middleware,
        /// performing operations on the HttpRequestMessage or HttpResponseMessage objects.
        /// </param>
        public void AddHandler(IMessageHandler messageHandler)
        {
            // decorate the current handler with the new handler; and through it create a chain of middlewares
            messageHandler.InnerHandler = innerHandler;
            innerHandler = messageHandler;
        }

        /// <summary>
        /// Sends a GET request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="uri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used by other objects or threads to receive notice of cancellation.
        /// </param>
        /// <returns>
        /// The Task<object> represents the asynchronous operation, and upon completion 
        /// contains the HttpResponseMessage issued from the server.
        /// </returns>
        public Task<HttpResponseMessage<T>> GetAsync<T>(Uri uri, CancellationToken cancellationToken)
        {
            return innerHandler.SendAsync<T>(new HttpRequestMessage {Uri = uri}, cancellationToken);
        }

        /// <summary>
        /// Sends a GET request to the specified Uri and returns the response body as a string in an asynchronous operation.
        /// </summary>
        /// <param name="uri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used by other objects or threads to receive notice of cancellation.
        /// </param>
        /// <returns>
        /// The Task<string> represents the asynchronous operation, and upon completion 
        /// contains the HttpResponseMessage as string issued from the server.
        /// </returns>
        public Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken)
        {
            return GetAsync<string>(uri, cancellationToken)
                .ContinueWith(x => x.Result.Content.Data, cancellationToken);
        }

        /// <summary>
        /// Sends a GET request to the specified Uri and returns the response body as a byte array in an asynchronous operation.
        /// </summary>
        /// <param name="uri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used by other objects or threads to receive notice of cancellation.
        /// </param>
        /// <returns>
        /// The Task<byte[]> represents the asynchronous operation, and upon completion 
        /// contains the HttpResponseMessage as byte array issued from the server.
        /// </returns>
        public Task<byte[]> GetByteArrayAsync(Uri uri, CancellationToken cancellationToken)
        {
            return GetAsync<byte[]>(uri, cancellationToken)
                .ContinueWith(x => x.Result.Content.Data, cancellationToken);
        }
    }
}