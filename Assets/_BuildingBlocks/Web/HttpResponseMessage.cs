using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Web
{
    /// <summary>
    /// Represents a HTTP response message.
    /// </summary>
    public class HttpResponseMessage<T> : AsyncOperation
    {
        public HttpContent<T> Content;
        public Dictionary<string, string> Headers;
        public bool IsSuccessStatusCode;
        public int StatusCode;
        public string ReasonPhrase;
    }
}