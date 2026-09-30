using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Netherlands3D.Functionalities.LASImporter.Parsing
{
    public interface ILASByteSource : IDisposable
    {
        long Length { get; }
        LASByteReadRequest Read(long offset, byte[] destination, int destinationOffset, int count);
    }

    public sealed class LASByteReadRequest
    {
        public bool IsDone { get; private set; }
        public int BytesRead { get; private set; }
        public Exception Error { get; private set; }

        internal void Complete(int bytesRead)
        {
            if (IsDone)
                return;

            BytesRead = bytesRead;
            IsDone = true;
        }

        internal void Fail(Exception exception)
        {
            if (IsDone)
                return;

            Error = exception;
            IsDone = true;
        }
    }

    public sealed class FileLASByteSource : ILASByteSource
    {
        private readonly FileStream stream;

        public long Length => stream.Length;

        public FileLASByteSource(string path)
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public LASByteReadRequest Read(long offset, byte[] destination, int destinationOffset, int count)
        {
            var request = new LASByteReadRequest();
            try
            {
                ValidateRead(offset, destination, destinationOffset, count);
                stream.Position = offset;
                request.Complete(stream.Read(destination, destinationOffset, count));
            }
            catch (Exception exception)
            {
                request.Fail(exception);
            }

            return request;
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        private void ValidateRead(long offset, byte[] destination, int destinationOffset, int count)
        {
            if (offset < 0 || offset > Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (destinationOffset < 0 || count < 0 || destinationOffset + count > destination.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
        }
    }

    public sealed class WebGLBrowserFileLASByteSource : ILASByteSource
    {
        [DllImport("__Internal")]
        private static extern double BrowserFileGetSize(string fileId);

        private readonly string fileId;
        private readonly string ownerId = Guid.NewGuid().ToString("N");
        private bool disposed;

        public long Length { get; }

        public WebGLBrowserFileLASByteSource(Uri uri)
        {
            if (uri == null || !uri.Scheme.Equals("browser-file", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Expected a browser-file URI.", nameof(uri));

            fileId = uri.Host.ToLowerInvariant();
#if !UNITY_EDITOR && UNITY_WEBGL
            var length = BrowserFileGetSize(fileId);
            if (length < 0 || length > long.MaxValue)
                throw new FileNotFoundException("The selected browser LAS file is no longer available.", uri.ToString());
            Length = (long)length;
#else
            throw new PlatformNotSupportedException("Browser-file LAS sources are only available in a WebGL player.");
#endif
        }

        public LASByteReadRequest Read(long offset, byte[] destination, int destinationOffset, int count)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(WebGLBrowserFileLASByteSource));
            if (offset < 0 || offset > Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (destinationOffset < 0 || count < 0 || destinationOffset + count > destination.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            var boundedCount = (int)Math.Min(count, Length - offset);
            return WebGLBrowserFileReadBridge.Read(
                ownerId,
                fileId,
                offset,
                destination,
                destinationOffset,
                boundedCount
            );
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            WebGLBrowserFileReadBridge.CancelOwner(ownerId);
        }
    }

    internal sealed class WebGLBrowserFileReadBridge : MonoBehaviour
    {
        private const string BridgeObjectName = "LASBrowserFileReadBridge";

        [DllImport("__Internal")]
        private static extern void BrowserFileReadRange(
            string fileId,
            double offset,
            int length,
            string callbackObject,
            string callbackMethod,
            int requestId
        );

        [DllImport("__Internal")]
        private static extern void BrowserFileReleaseBuffer(int pointer);

        private sealed class PendingRead
        {
            public string OwnerId;
            public byte[] Destination;
            public int DestinationOffset;
            public int Capacity;
            public LASByteReadRequest Request;
        }

        private static WebGLBrowserFileReadBridge instance;
        private static int nextRequestId = 1;
        private readonly Dictionary<int, PendingRead> pendingReads = new();

        internal static LASByteReadRequest Read(
            string ownerId,
            string fileId,
            long offset,
            byte[] destination,
            int destinationOffset,
            int count
        )
        {
            var request = new LASByteReadRequest();
            if (count == 0)
            {
                request.Complete(0);
                return request;
            }

#if !UNITY_EDITOR && UNITY_WEBGL
            var bridge = GetOrCreateInstance();
            var requestId = nextRequestId++;
            bridge.pendingReads.Add(requestId, new PendingRead
            {
                OwnerId = ownerId,
                Destination = destination,
                DestinationOffset = destinationOffset,
                Capacity = count,
                Request = request
            });
            BrowserFileReadRange(fileId, offset, count, bridge.gameObject.name, nameof(OnBrowserFileRead), requestId);
#else
            request.Fail(new PlatformNotSupportedException("Browser file range reads require WebGL."));
#endif
            return request;
        }

        internal static void CancelOwner(string ownerId)
        {
            if (!instance)
                return;

            var requestIds = instance.pendingReads
                .Where(pair => pair.Value.OwnerId == ownerId)
                .Select(pair => pair.Key)
                .ToArray();

            foreach (var requestId in requestIds)
            {
                var pendingRead = instance.pendingReads[requestId];
                pendingRead.Request.Fail(new OperationCanceledException("The LAS byte source was disposed."));
                instance.pendingReads.Remove(requestId);
            }
        }

        public void OnBrowserFileRead(string message)
        {
            var parts = message.Split('|');
            if (parts.Length != 3
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var requestId)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pointer)
                || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var bytesRead))
            {
                Debug.LogError($"Invalid browser LAS range callback: {message}", this);
                return;
            }

            PendingRead pendingRead = null;
            try
            {
                if (!pendingReads.Remove(requestId, out pendingRead))
                    return;

                if (bytesRead < 0)
                {
                    pendingRead.Request.Fail(new IOException($"Browser LAS range read failed with code {bytesRead}."));
                    return;
                }

                if (bytesRead > pendingRead.Capacity)
                {
                    pendingRead.Request.Fail(new InvalidDataException(
                        $"Browser LAS range returned {bytesRead} bytes for a {pendingRead.Capacity}-byte request."
                    ));
                    return;
                }

                if (bytesRead > 0)
                    Marshal.Copy(new IntPtr(pointer), pendingRead.Destination, pendingRead.DestinationOffset, bytesRead);
                pendingRead.Request.Complete(bytesRead);
            }
            catch (Exception exception)
            {
                if (pendingRead != null)
                    pendingRead.Request.Fail(exception);
                else
                    Debug.LogException(exception, this);
            }
            finally
            {
#if !UNITY_EDITOR && UNITY_WEBGL
                if (pointer != 0)
                    BrowserFileReleaseBuffer(pointer);
#endif
            }
        }

        private static WebGLBrowserFileReadBridge GetOrCreateInstance()
        {
            if (instance)
                return instance;

            var gameObject = new GameObject(BridgeObjectName);
            DontDestroyOnLoad(gameObject);
            instance = gameObject.AddComponent<WebGLBrowserFileReadBridge>();
            return instance;
        }
    }
}
