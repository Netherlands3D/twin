using System.Threading;
using System.Threading.Tasks;

namespace Netherlands3D.Web
{
    /// <summary>
    /// Base interface for a message handler.
    /// </summary>
    public interface IMessageHandler
    {
        public Task<HttpResponseMessage<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken);
        public IMessageHandler InnerHandler { get; set; }
    }
}