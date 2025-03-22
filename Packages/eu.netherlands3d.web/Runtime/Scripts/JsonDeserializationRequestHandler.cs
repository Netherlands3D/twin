using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Netherlands3D.Web
{
    /// <summary>
    /// Message handler that handles deserializing json from http response.
    /// This handler is part of a chain of responsibility pattern where each class in the chain
    /// either handles the request or delegates to the next handler in the chain.
    /// </summary>
    public class JsonDeserializationRequestHandler : DelegatingHandler
    {
        public JsonDeserializationRequestHandler(IMessageHandler innerHandler) : base(innerHandler)
        {
        }

        public override Task<HttpResponseMessage<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<HttpResponseMessage<T>>();

            base.SendAsync<string>(request, cancellationToken).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception);
                    return;
                }
                
                if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                    return;
                }

                var contentString = task.Result.Content.Data;
                var newResponse = new HttpResponseMessage<T>
                {
                    Content = new HttpContent<T> { Data = JsonConvert.DeserializeObject<T>(contentString) },
                    Headers = new Dictionary<string, string>(task.Result.Headers),
                    IsSuccessStatusCode = task.Result.IsSuccessStatusCode,
                    StatusCode = task.Result.StatusCode,
                    ReasonPhrase = task.Result.ReasonPhrase
                };

                tcs.TrySetResult(newResponse);
            }, cancellationToken);
    
            return tcs.Task;
        }
    }
}