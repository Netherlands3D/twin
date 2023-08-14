using System.Collections.Specialized;

namespace Netherlands3D.Twin
{
    public interface IQueryStringMapper
    {
        bool Populate(NameValueCollection queryParameters);
        string ToQueryString();
    }
}