using System;
using System.Collections.Generic;
using KindMen.Uxios;
using RSG;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D.Tilekit.ContentLoaders
{
    public class Texture2DLoader : IDisposable, IContentLoader<Texture2D>
    {
        // Completed textures and coalesced in-flight requests
        private readonly Dictionary<ulong, Texture2D> cache = new();
        private readonly Dictionary<ulong, Promise<Texture2D>> inflight = new();

        public IPromise<Texture2D> Load(string url)
        {
            var key = HashUrl(url);

            // Hit completed cache
            if (cache.TryGetValue(key, out var cached) && cached)
                return Promise<Texture2D>.Resolved(cached);

            // Piggyback on an in-flight request
            if (inflight.TryGetValue(key, out var existing))
                return existing;

            // Start a new request and record it as in-flight
            var p = new Promise<Texture2D>();
            inflight[key] = p;

            Uxios.DefaultInstance
                .Get<Texture2D>(new Uri(url))
                .Then(resp =>
                {
                    var tex = OnContentReceived(resp);
                    cache[key] = tex; // store result
                    inflight.Remove(key);
                    p.Resolve(tex);
                })
                .Catch(ex =>
                {
                    inflight.Remove(key);
                    p.Reject(ex);
                });

            return p;
        }

        // Optional: let callers evict when tiles cool down (no refcounting; keep it minimal)
        public bool TryEvict(string url)
        {
            return TryEvict(HashUrl(url));
        }

        public bool TryEvict(ulong key)
        {
            if (!cache.TryGetValue(key, out var tex)) return false;
            
            if (tex) Object.Destroy(tex);
            
            cache.Remove(key);
            return true;
        }

        public void ClearCache()
        {
            foreach (var kv in cache)
                if (kv.Value)
                    Object.Destroy(kv.Value);
            cache.Clear();
        }

        public void Dispose() => ClearCache();

        // Keep your existing post-process: drop CPU copy if possible
        private Texture2D OnContentReceived(IResponse response)
        {
            var texture = response.Data as Texture2D;
            if (texture?.isReadable == true)
                texture.Apply(false, true); // nonReadable = true
            return texture;
        }

        public bool TryGet(ulong key, out Texture2D tex) => cache.TryGetValue(key, out tex) && tex;

        public IPromise<Texture2D> GetAsync(ulong key)
        {
            if (cache.TryGetValue(key, out var tex) && tex)
                return Promise<Texture2D>.Resolved(tex);

            if (inflight.TryGetValue(key, out var p))
                return p;

            return Promise<Texture2D>.Rejected(new Exception("Texture not cached or in-flight."));
        }

        // Simple non-alloc 64-bit hash over AbsoluteUri (FNV-1a 64)
        public static ulong HashUrl(string url)
        {
            unchecked
            {
                const ulong offset = 1469598103934665603UL;
                const ulong prime = 1099511628211UL;
                ulong h = offset;
                var s = url;
                for (int i = 0; i < s.Length; i++)
                {
                    h ^= s[i];
                    h *= prime;
                }

                return h;
            }
        }
    }
}