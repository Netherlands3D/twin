using System;
using KindMen.Uxios.Http;

namespace Netherlands3D.Twin.Functionalities
{
    public interface IQueryStringMapper
    {
        void Populate(QueryParameters queryParameters);

        void AddQueryParameters(UriBuilder urlBuilder);
    }
}