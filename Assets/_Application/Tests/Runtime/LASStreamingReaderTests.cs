using System;
using System.Collections;
using System.Text;
using Netherlands3D.Functionalities.LASImporter;
using Netherlands3D.Functionalities.LASImporter.Parsing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Netherlands3D.Twin.Tests
{
    public class LASStreamingReaderTests
    {
        private const int HeaderSize = 375;
        private const int PointRecordLength = 34;

        [UnityTest]
        public IEnumerator InitializeHandlesPartialRangeReads()
        {
            var sourceBytes = CreateLasBytes();
            using var reader = new LASStreamingReader(new PartialByteSource(sourceBytes, 17));

            yield return reader.Initialize();

            Assert.That(reader.Error, Is.Null);
            Assert.That(reader.Header.VersionMajor, Is.EqualTo(1));
            Assert.That(reader.Header.VersionMinor, Is.EqualTo(4));
            Assert.That(reader.Header.HeaderSize, Is.EqualTo(HeaderSize));
            Assert.That(reader.Header.OffsetToPointData, Is.EqualTo(HeaderSize));
            Assert.That(reader.Header.PointDataFormat, Is.EqualTo(3));
            Assert.That(reader.Header.PointDataRecordLength, Is.EqualTo(PointRecordLength));
            Assert.That(reader.Header.PointCount, Is.EqualTo(1));
            Assert.That(reader.Header.XScale, Is.EqualTo(0.01d));
            Assert.That(reader.Header.YScale, Is.EqualTo(0.02d));
            Assert.That(reader.Header.ZScale, Is.EqualTo(0.5d));
        }

        [Test]
        public void ParsePointMatchesFormatThreeCoordinatesClassificationAndColor()
        {
            var sourceBytes = CreateLasBytes();
            var header = LASStreamingReader.ParseHeader(sourceBytes, sourceBytes.Length);

            var point = LASStreamingReader.ParsePoint(sourceBytes, HeaderSize, header);

            Assert.That(point.X, Is.EqualTo(1001d));
            Assert.That(point.Y, Is.EqualTo(1999d));
            Assert.That(point.Z, Is.EqualTo(-6d));
            Assert.That(point.Classification, Is.EqualTo(6));
            Assert.That(point.HasColor, Is.True);
            Assert.That(point.Color.r, Is.EqualTo(255));
            Assert.That(point.Color.g, Is.EqualTo(128));
            Assert.That(point.Color.b, Is.EqualTo(1));
            Assert.That(point.Color.a, Is.EqualTo(255));
        }

        [Test]
        public void ParseHeaderRejectsInvalidSignature()
        {
            var sourceBytes = CreateLasBytes();
            sourceBytes[0] = (byte)'N';

            Assert.Throws<System.IO.InvalidDataException>(
                () => LASStreamingReader.ParseHeader(sourceBytes, sourceBytes.Length)
            );
        }

        [TestCase(0f, 0f, 0, 0)]
        [TestCase(74.999f, 74.999f, 0, 0)]
        [TestCase(75f, 75f, 1, 1)]
        [TestCase(-0.001f, -0.001f, -1, -1)]
        [TestCase(-75f, -75f, -1, -1)]
        [TestCase(-75.001f, -75.001f, -2, -2)]
        public void CameraAndPointsUseTheSameChunkGrid(float x, float z, int expectedX, int expectedY)
        {
            var key = LASPointCloudLayer.GetChunkKey(new Vector3(x, 123f, z), 75f);

            Assert.That(key, Is.EqualTo(new Vector2Int(expectedX, expectedY)));
        }

        private static byte[] CreateLasBytes()
        {
            var bytes = new byte[HeaderSize + PointRecordLength];
            Encoding.ASCII.GetBytes("LASF").CopyTo(bytes, 0);
            bytes[24] = 1;
            bytes[25] = 4;
            WriteUInt16(bytes, 94, HeaderSize);
            WriteUInt32(bytes, 96, HeaderSize);
            WriteUInt32(bytes, 100, 0);
            bytes[104] = 3;
            WriteUInt16(bytes, 105, PointRecordLength);
            WriteUInt32(bytes, 107, 1);
            WriteDouble(bytes, 131, 0.01d);
            WriteDouble(bytes, 139, 0.02d);
            WriteDouble(bytes, 147, 0.5d);
            WriteDouble(bytes, 155, 1000d);
            WriteDouble(bytes, 163, 2000d);
            WriteDouble(bytes, 171, -10d);
            WriteDouble(bytes, 179, 1001d);
            WriteDouble(bytes, 187, 1001d);
            WriteDouble(bytes, 195, 1999d);
            WriteDouble(bytes, 203, 1999d);
            WriteDouble(bytes, 211, -6d);
            WriteDouble(bytes, 219, -6d);
            WriteUInt64(bytes, 247, 1);

            var pointOffset = HeaderSize;
            WriteInt32(bytes, pointOffset, 100);
            WriteInt32(bytes, pointOffset + 4, -50);
            WriteInt32(bytes, pointOffset + 8, 8);
            bytes[pointOffset + 15] = 6;
            WriteUInt16(bytes, pointOffset + 28, ushort.MaxValue);
            WriteUInt16(bytes, pointOffset + 30, 32768);
            WriteUInt16(bytes, pointOffset + 32, 257);
            return bytes;
        }

        private static void WriteUInt16(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteUInt64(byte[] bytes, int offset, ulong value)
        {
            WriteUInt32(bytes, offset, (uint)value);
            WriteUInt32(bytes, offset + 4, (uint)(value >> 32));
        }

        private static void WriteInt32(byte[] bytes, int offset, int value)
        {
            WriteUInt32(bytes, offset, unchecked((uint)value));
        }

        private static void WriteDouble(byte[] bytes, int offset, double value)
        {
            WriteUInt64(bytes, offset, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
        }

        private sealed class PartialByteSource : ILASByteSource
        {
            private readonly byte[] bytes;
            private readonly int maximumReadSize;

            public long Length => bytes.Length;

            public PartialByteSource(byte[] bytes, int maximumReadSize)
            {
                this.bytes = bytes;
                this.maximumReadSize = maximumReadSize;
            }

            public LASByteReadRequest Read(long offset, byte[] destination, int destinationOffset, int count)
            {
                var request = new LASByteReadRequest();
                var bytesRead = Math.Min(Math.Min(count, maximumReadSize), bytes.Length - (int)offset);
                if (bytesRead > 0)
                    Buffer.BlockCopy(bytes, (int)offset, destination, destinationOffset, bytesRead);
                request.Complete(bytesRead);
                return request;
            }

            public void Dispose()
            {
            }
        }
    }
}
