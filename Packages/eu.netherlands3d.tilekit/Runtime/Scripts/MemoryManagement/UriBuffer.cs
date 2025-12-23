using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    public sealed class UriBuffer : IDisposable, IMemoryReporter
    {
        private NativeParallelHashMap<uint2, int> hashes; // lookup table / identifiers for URI's in a compact format
        private NativeArray<int> schemes;
        private NativeArray<int> hosts;
        private NativeArray<int> ports;
        private readonly Buffer<int> pathSegments;
        private readonly Buffer<(int, int)> queries;
        private NativeArray<int> fragments;

        private readonly StringBuffer stringBuffer;

        public UriBuffer(StringBuffer stringBuffer, int capacity, Allocator allocator = Allocator.Persistent)
        {
            this.stringBuffer = stringBuffer;
            hashes = new NativeParallelHashMap<uint2, int>(capacity, allocator);
            schemes = new NativeArray<int>(capacity, allocator);
            hosts = new NativeArray<int>(capacity, allocator);
            ports = new NativeArray<int>(capacity, allocator);
            pathSegments = new Buffer<int>(capacity, capacity * 10, allocator);
            queries = new Buffer<(int, int)>(capacity, capacity * 10, allocator);
            fragments = new NativeArray<int>(capacity, allocator);
        }

        public int Count => hashes.Count();
        public int Capacity => hashes.Capacity;

        public int Add(string url)
        {
            // TODO: Replace this with a zero-alloc URI parser
            var uri = new Uri(url);

            var urlIndex = hashes.Count(); // Determine the next index by using the count of the hashmap, this only works if we treat this buffer as append-only
            hashes.Add(Hashing.HashString(url), urlIndex);
            schemes[urlIndex] = stringBuffer.Add(uri.Scheme);
            hosts[urlIndex] = stringBuffer.Add(uri.Host);
            ports[urlIndex] = uri.Port;
            
            // TODO: Replace this with a zero-alloc version, or part of the aforementioned URI parser
            var segmentStrings = uri.AbsolutePath.Trim('/').Split('/');
            Span<int> segmentSpan = stackalloc int[segmentStrings.Length];
            for (int i = 0; i < segmentStrings.Length; i++)
            {
                segmentSpan[i] = stringBuffer.Add(segmentStrings[i]);
            }
            
            // Since this is all Append-only - Add should be consistent with the urlIndex 
            pathSegments.Add(segmentSpan);
            
            // TODO: Replace this with a zero-alloc version, or part of the aforementioned URI parser
            var queryStrings = uri.Query.Trim('?').Split('&');
            Span<(int, int)> queryStringSpan = stackalloc (int, int)[queryStrings.Length];
            for (int i = 0; i < queryStrings.Length; i++)
            {
                var queryString = queryStrings[i].Split('=');
                if (queryString.Length == 1 && string.IsNullOrWhiteSpace(queryString[0])) continue;
                var keyString = queryString[0];
                var valueString = "";
                if (queryString.Length > 1 && !string.IsNullOrWhiteSpace(queryString[1]))
                {
                    valueString = queryString[1];
                }

                var key = stringBuffer.Add(keyString);
                var value = stringBuffer.Add(valueString);

                queryStringSpan[i] = (key, value);
            }
            
            // Since this is all Append-only - Add should be consistent with the urlIndex 
            queries.Add(queryStringSpan);
            
            fragments[urlIndex] = stringBuffer.Add(uri.Fragment);
            
            return urlIndex;
        }

        public string GetAsString(int uriIndex)
        {
            // TODO: Replace this with a zero-alloc URI builder
            var uriBuilder = new UriBuilder();
            uriBuilder.Scheme = stringBuffer.GetAsString(schemes[uriIndex]);
            uriBuilder.Host = stringBuffer.GetAsString(hosts[uriIndex]);
            uriBuilder.Port = ports[uriIndex];
            uriBuilder.Fragment = stringBuffer.GetAsString(fragments[uriIndex]);

            for (int i = 0; i < pathSegments[uriIndex].Length; i++)
            {
                if (i > 0) uriBuilder.Path += "/";
                uriBuilder.Path += stringBuffer.GetAsString(pathSegments[uriIndex][i]);
            } 
            for (int i = 0; i < queries[uriIndex].Length; i++)
            {
                if (i > 0) uriBuilder.Query += "&";
                var param = queries[uriIndex][i];
                // TODO: Something goes wrong here, I always get "https", which could mean that every entry is 0?
                var key = stringBuffer.GetAsString(param.Item1);
                string value = stringBuffer.GetAsString(param.Item2);
                uriBuilder.Query += key;
                if (!string.IsNullOrEmpty(value))
                {
                    uriBuilder.Query += "=" + value;
                }
            }
            
            return uriBuilder.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetReservedBytes() 
            => hashes.GetReservedBytes() 
              + schemes.GetReservedBytes() 
              + hosts.GetReservedBytes() 
              + ports.GetReservedBytes() 
              + pathSegments.GetReservedBytes() 
              + queries.GetReservedBytes() 
              + fragments.GetReservedBytes();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetUsedBytes() 
            => hashes.GetUsedBytes() 
              + schemes.GetUsedBytes() 
              + hashes.GetUsedBytes() 
              + ports.GetUsedBytes() 
              + pathSegments.GetUsedBytes() 
              + queries.GetUsedBytes() 
              + fragments.GetUsedBytes();

        public void Clear()
        {
            hashes.Clear();
            pathSegments.Clear();
            queries.Clear();
        }

        public void Dispose()
        {
            Clear();

            hashes.Dispose();
            schemes.Dispose();
            hosts.Dispose();
            ports.Dispose();
            pathSegments.Dispose();
            queries.Dispose();
            fragments.Dispose();
        }
    }
}