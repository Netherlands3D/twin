using RSG;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.ContentLoaders
{
    public interface IContentLoader<T> where T : Object
    {
        public IPromise<T> Load(string url);
        public bool TryEvict(string url);
        public bool TryEvict(uint2 key);
        public bool TryGet(uint2 key, out T tex);
        public IPromise<T> GetAsync(uint2 key);
    }
}