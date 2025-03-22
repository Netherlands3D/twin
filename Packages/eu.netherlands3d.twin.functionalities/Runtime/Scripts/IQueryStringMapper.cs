using System;
using System.Collections.Specialized;

namespace Netherlands3D.Twin.Functionalities
{
    public interface IQueryStringMapper
    {
        void Populate(NameValueCollection queryParameters);

        void AddQueryParameters(UriBuilder urlBuilder);
    }
}