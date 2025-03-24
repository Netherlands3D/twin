using System;
using System.Collections.Generic;

namespace Netherlands3D.Web
{
    /// <summary>
    /// Represents a HTTP request message.
    /// </summary>
    public class HttpRequestMessage
    {
        public Uri Uri;
        public Dictionary<string, string> Headers = new();
    }
}