using RSG;
using UnityEngine;

namespace Netherlands3D.Tilekit.ContentLoaders
{
    public interface IContentLoader<T> where T : Object
    {
        public IPromise<T> Load(string url);
        public bool TryEvict(string url);
        public bool TryEvict(ulong key);
        public bool TryGet(ulong key, out T tex);
        public IPromise<T> GetAsync(ulong key);
    }
}