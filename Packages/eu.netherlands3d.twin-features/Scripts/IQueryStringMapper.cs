using System.Collections.Specialized;

namespace Netherlands3D.Twin.Features
{
    public interface IQueryStringMapper
    {
        void Populate(NameValueCollection queryParameters);
        string ToQueryString();
    }
}