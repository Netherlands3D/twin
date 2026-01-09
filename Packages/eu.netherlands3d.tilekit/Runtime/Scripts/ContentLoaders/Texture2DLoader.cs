using System;
using System.Collections.Generic;
using KindMen.Uxios;
using Netherlands3D.Tilekit.MemoryManagement;
using Netherlands3D.Tilekit.Profiling;
using RSG;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Netherlands3D.Tilekit.ContentLoaders
{
    public sealed class Texture2DLoader : IDisposable, IContentLoader<Texture2D>, IGlobalStatsSource
    {
        // ---------------- Singleton ----------------

        private static readonly Lazy<Texture2DLoader> instance =
            new(() => new Texture2DLoader());

        public static Texture2DLoader Instance => instance.Value;

        static Texture2DLoader()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                    Cleanup();
            };
#endif
        }

        // Prevent external construction
        private Texture2DLoader()
        {
            RegisterTelemetry();
        }

        /// <summary>
        /// Explicitly releases all cached textures.
        /// Call this on application shutdown if desired.
        /// </summary>
        public static void Shutdown()
        {
            if (!instance.IsValueCreated)
                return;

            instance.Value.Dispose();
        }

        // ---------------- Telemetry ----------------

        // Call once (e.g., in Instance getter after creation)
        private void RegisterTelemetry() => Telemetry.RegisterGlobal(this);

        void IGlobalStatsSource.Collect(ref GlobalStats stats)
        {
            // Option A: cheap tally updated on insert/evict (best)
            // Option B: estimate each frame (can be heavy with many textures)

            long bytes = 0;
            foreach (var kv in cache)
            {
                var tex = kv.Value;
                if (!tex) continue;
                bytes += Profiler.GetRuntimeMemorySizeLong(tex);
            }

            stats.TextureCacheBytes += bytes;
        }
        
        // ---------------- State ----------------

        // Completed textures and coalesced in-flight requests
        private readonly Dictionary<uint2, Texture2D> cache = new();
        private readonly Dictionary<uint2, Promise<Texture2D>> inflight = new();

        // ---------------- Public API ----------------

        public IPromise<Texture2D> Load(string url)
        {
            var key = HashUrl(url);

            // Hit completed cache
            if (cache.TryGetValue(key, out var cached) && cached)
                return Promise<Texture2D>.Resolved(cached);

            // Piggyback on an in-flight request
            if (inflight.TryGetValue(key, out var existing))
                return existing;

            // Start a new request
            var p = new Promise<Texture2D>();
            inflight[key] = p;

            Uxios.DefaultInstance
                .Get<Texture2D>(new Uri(url))
                .Then(resp =>
                {
                    var tex = OnContentReceived(resp);
                    cache[key] = tex;
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

        public bool TryEvict(string url) => TryEvict(HashUrl(url));

        public bool TryEvict(uint2 key)
        {
            if (!cache.TryGetValue(key, out var tex))
                return false;

            if (tex)
                Object.Destroy(tex);

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

        public bool TryGet(uint2 key, out Texture2D tex) =>
            cache.TryGetValue(key, out tex) && tex;

        public IPromise<Texture2D> GetAsync(uint2 key)
        {
            if (cache.TryGetValue(key, out var tex) && tex)
                return Promise<Texture2D>.Resolved(tex);

            if (inflight.TryGetValue(key, out var p))
                return p;

            return Promise<Texture2D>.Rejected(
                new Exception("Texture not cached or in-flight.")
            );
        }

        // ---------------- Internals ----------------

        private Texture2D OnContentReceived(IResponse response)
        {
            var texture = response.Data as Texture2D;
            if (texture?.isReadable == true)
                texture.Apply(false, true); // drop CPU copy

            return texture;
        }

        // Simple non-alloc 64-bit hash over AbsoluteUri
        public static uint2 HashUrl(string url)
        {
            return Hashing.HashString(url);
        }
        
        private static void Cleanup()
        {
            if (Instance == null) return;
            Instance.ClearCache();
        }
    }
}
