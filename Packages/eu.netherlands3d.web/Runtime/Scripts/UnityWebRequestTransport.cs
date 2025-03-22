using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Web
{
    /// <summary>
    /// Unity MonoBehaviour that facilitates sending Unity web requests in a coroutine.
    /// This is required because Unity web requests can only be sent in the Unity main thread.
    /// </summary>
    public class UnityWebRequestTransport : MonoBehaviour, IMessageHandler 
    {
        /// <summary>
        /// Satisfy the interface, but this is a Transport and has no need of an Inner Handler.
        /// </summary>
        public IMessageHandler InnerHandler { get; set; }

        private struct RequestData 
        {
            public UnityWebRequest UnityWebRequest { get; set; }
            public Coroutine Coroutine { get; set; }
        }
        
        private readonly Dictionary<CancellationToken, RequestData> runningRequests = new();

        public Task<HttpResponseMessage<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken) 
        {
            var tcs = new TaskCompletionSource<HttpResponseMessage<T>>();
            
            if (typeof(T) != typeof(string) && typeof(T) != typeof(byte[]))
            {
                tcs.SetException(
                    new ArgumentException(
                        "The UnityWebRequestTransport only supports byte arrays or strings to be downloaded, " 
                        + "please use the JsonDeserializationRequestHandler or another handler to perform the conversion"
                    )
                );

                return tcs.Task;
            }
            
            var coroutine = StartCoroutine(SendWebRequestCoroutine(request, tcs, cancellationToken));

            runningRequests[cancellationToken] = new RequestData{ UnityWebRequest = null, Coroutine = coroutine};

            cancellationToken.Register(() =>
            {
                if (runningRequests.TryGetValue(cancellationToken, out var requestData))
                {
                    requestData.UnityWebRequest?.Abort();
                    StopCoroutine(requestData.Coroutine);
                    runningRequests.Remove(cancellationToken);
                }
                tcs.TrySetCanceled();
            });
            
            return tcs.Task;
        }

        private IEnumerator SendWebRequestCoroutine<T>(
            HttpRequestMessage request, 
            TaskCompletionSource<HttpResponseMessage<T>> tcs, 
            CancellationToken cancellationToken
        ) {
            using var unityWebRequest = new UnityWebRequest(request.Uri);
            var runningRequest = runningRequests[cancellationToken];
            runningRequest.UnityWebRequest = unityWebRequest;
            
            foreach (var header in request.Headers)
            {
                unityWebRequest.SetRequestHeader(header.Key, header.Value);
            }

            var downloadHandlerBuffer = new DownloadHandlerBuffer();
            unityWebRequest.downloadHandler = downloadHandlerBuffer;

            unityWebRequest.SendWebRequest();
            
            while (!unityWebRequest.isDone && !cancellationToken.IsCancellationRequested)
            {
                yield return null;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled();
                yield break;
            }
            
            var responseMessage = new HttpResponseMessage<T>
            {
                StatusCode = (int)unityWebRequest.responseCode,
                ReasonPhrase = unityWebRequest.error,
                IsSuccessStatusCode = string.IsNullOrEmpty(unityWebRequest.error) && (unityWebRequest.responseCode >= 200 && unityWebRequest.responseCode <= 299),
            };

            foreach (var header in unityWebRequest.GetResponseHeaders())
            {
                responseMessage.Headers.Add(header.Key, header.Value);
            }

            if (typeof(T) == typeof(string))
            {
                var content = new HttpContent<string> { Data = downloadHandlerBuffer.text };
                responseMessage.Content = content as HttpContent<T>;
            }
            else if (typeof(T) == typeof(byte[]))
            {
                var content = new HttpContent<byte[]> { Data = downloadHandlerBuffer.data };
                responseMessage.Content = content as HttpContent<T>;
            }
            tcs.TrySetResult(responseMessage);
        }
    }
}