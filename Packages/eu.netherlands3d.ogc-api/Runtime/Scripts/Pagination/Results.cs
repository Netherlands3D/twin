using System;
using System.Threading.Tasks;
using KindMen.Uxios.Api;
using Netherlands3D.OgcApi.ExtensionMethods;

namespace Netherlands3D.OgcApi.Pagination
{
    public class Results<T>
    {
        public DateTime? TimeStamp { get; set; } = null;

        public long NumberMatched { get; set; } = 0;

        public long NumberReturned { get; set; } = 0;

        public T Value { get; set; }

        public Link[] Links { get; set; }

        public Results()
        {
        }

        public Results(T value)
        {
            Value = value;
        }

        public bool First()
        {
            return Links.FirstBy(RelationTypes.prev) == null;
        }
        
        public bool Last()
        {
            return Links.FirstBy(RelationTypes.next) == null;
        }
        
        public async Task<Results<T>> Previous()
        {
            var link = Links.FirstBy(RelationTypes.prev)?.Href;
            if (link == null) return null;

            return await new Resource<Results<T>>(new Uri(link)).Value;
        }

        public async Task<Results<T>> Next()
        {
            var link = Links.FirstBy(RelationTypes.next)?.Href;
            if (link == null) return null;

            return await new Resource<Results<T>>(new Uri(link)).Value;
        }
    }
}