using System;
using System.Collections;
using System.IO;
using System.Text;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Functionalities.LASImporter.Parsing
{
    public sealed class LASStreamingReader : IDisposable
    {
        private const int MinimumHeaderBytes = 227;
        private const int HeaderProbeBytes = 375;
        private const int VlrHeaderBytes = 54;
        private const string ProjectionUserId = "LASF_Projection";

        private readonly ILASByteSource source;

        public LASHeader Header { get; private set; }
        public Exception Error { get; private set; }
        public long FileLength => source.Length;

        public LASStreamingReader(ILASByteSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public IEnumerator Initialize()
        {
            Error = null;
            var headerByteCount = (int)Math.Min(HeaderProbeBytes, source.Length);
            if (headerByteCount < MinimumHeaderBytes)
            {
                Error = new InvalidDataException("The selected file is too small to contain a LAS header.");
                yield break;
            }

            var headerBytes = new byte[headerByteCount];
            yield return ReadExactly(0, headerBytes, headerByteCount);
            if (Error != null)
                yield break;

            try
            {
                Header = ParseHeader(headerBytes, source.Length);
            }
            catch (Exception exception)
            {
                Error = exception;
                yield break;
            }

            yield return ReadCoordinateSystemFromVlrs();
            if (Error != null)
                yield break;

            if (Header.CoordinateSystem == CoordinateSystem.Undefined)
                Header.CoordinateSystem = LASFileReader.DetectCoordinateSystemFromBounds(Header);
        }

        public LASByteReadRequest ReadPointBytes(long offset, byte[] destination, int count)
        {
            return source.Read(offset, destination, 0, count);
        }

        public void Dispose()
        {
            source.Dispose();
        }

        internal static LASHeader ParseHeader(byte[] bytes, long fileLength)
        {
            if (bytes == null || bytes.Length < MinimumHeaderBytes)
                throw new InvalidDataException("The selected file is too small to contain a LAS header.");
            if (Encoding.ASCII.GetString(bytes, 0, 4) != "LASF")
                throw new InvalidDataException("The selected file is not a LAS file.");

            var header = new LASHeader
            {
                VersionMajor = bytes[24],
                VersionMinor = bytes[25],
                HeaderSize = ReadUInt16(bytes, 94),
                OffsetToPointData = ReadUInt32(bytes, 96),
                VariableLengthRecordCount = ReadUInt32(bytes, 100),
                PointDataFormat = (byte)(bytes[104] & 0x3f),
                PointDataRecordLength = ReadUInt16(bytes, 105),
                PointCount = ReadUInt32(bytes, 107),
                XScale = ReadDouble(bytes, 131),
                YScale = ReadDouble(bytes, 139),
                ZScale = ReadDouble(bytes, 147),
                XOffset = ReadDouble(bytes, 155),
                YOffset = ReadDouble(bytes, 163),
                ZOffset = ReadDouble(bytes, 171),
                MaxX = ReadDouble(bytes, 179),
                MinX = ReadDouble(bytes, 187),
                MaxY = ReadDouble(bytes, 195),
                MinY = ReadDouble(bytes, 203),
                MaxZ = ReadDouble(bytes, 211),
                MinZ = ReadDouble(bytes, 219)
            };

            if (header.HeaderSize >= HeaderProbeBytes && bytes.Length >= 255)
            {
                var extendedPointCount = ReadUInt64(bytes, 247);
                if (extendedPointCount > 0)
                    header.PointCount = extendedPointCount;
            }

            if (header.HeaderSize < MinimumHeaderBytes || header.HeaderSize > fileLength)
                throw new InvalidDataException($"Invalid LAS header size: {header.HeaderSize}.");
            if (header.OffsetToPointData < header.HeaderSize || header.OffsetToPointData > fileLength)
                throw new InvalidDataException($"Invalid LAS point-data offset: {header.OffsetToPointData}.");
            var minimumPointRecordLength = GetMinimumPointRecordLength(header.PointDataFormat);
            if (header.PointDataRecordLength < minimumPointRecordLength)
            {
                throw new InvalidDataException(
                    $"LAS point format {header.PointDataFormat} requires at least {minimumPointRecordLength} bytes " +
                    $"per record, but the header declares {header.PointDataRecordLength}."
                );
            }

            return header;
        }

        internal static LASPointData ParsePoint(byte[] bytes, int offset, LASHeader header)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (offset < 0 || offset + header.PointDataRecordLength > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            var rawX = ReadInt32(bytes, offset);
            var rawY = ReadInt32(bytes, offset + 4);
            var rawZ = ReadInt32(bytes, offset + 8);
            var classification = header.PointDataFormat <= 5
                ? (byte)(bytes[offset + 15] & 0x1f)
                : bytes[offset + 16];

            var rgbOffset = LASFileReader.GetRgbOffset(header.PointDataFormat);
            var hasColor = rgbOffset >= 0 && header.PointDataRecordLength >= rgbOffset + 6;
            var color = default(Color32);
            if (hasColor)
            {
                var r = ReadUInt16(bytes, offset + rgbOffset);
                var g = ReadUInt16(bytes, offset + rgbOffset + 2);
                var b = ReadUInt16(bytes, offset + rgbOffset + 4);
                color = new Color32(
                    LASFileReader.ToByteColor(r),
                    LASFileReader.ToByteColor(g),
                    LASFileReader.ToByteColor(b),
                    255
                );
            }

            return new LASPointData(
                rawX * header.XScale + header.XOffset,
                rawY * header.YScale + header.YOffset,
                rawZ * header.ZScale + header.ZOffset,
                classification,
                color,
                hasColor
            );
        }

        private IEnumerator ReadCoordinateSystemFromVlrs()
        {
            var geoKeys = Array.Empty<ushort>();
            var geoAscii = string.Empty;
            var detectedFromWkt = CoordinateSystem.Undefined;
            var vlrHeader = new byte[VlrHeaderBytes];
            byte[] payload = null;
            long position = Header.HeaderSize;

            for (uint i = 0; i < Header.VariableLengthRecordCount; i++)
            {
                if (position + VlrHeaderBytes > source.Length || position + VlrHeaderBytes > Header.OffsetToPointData)
                    break;

                yield return ReadExactly(position, vlrHeader, VlrHeaderBytes);
                if (Error != null)
                    yield break;

                var userId = Encoding.ASCII.GetString(vlrHeader, 2, 16).TrimEnd('\0', ' ');
                var recordId = ReadUInt16(vlrHeader, 18);
                var recordLength = ReadUInt16(vlrHeader, 20);
                var dataStart = position + VlrHeaderBytes;
                var dataEnd = dataStart + recordLength;
                if (dataEnd > source.Length || dataEnd > Header.OffsetToPointData)
                    break;

                var isProjectionRecord = userId == ProjectionUserId
                                         && (recordId == 34735 || recordId == 34737 || recordId == 2111 || recordId == 2112);
                if (isProjectionRecord && recordLength > 0)
                {
                    if (payload == null || payload.Length < recordLength)
                        payload = new byte[recordLength];

                    yield return ReadExactly(dataStart, payload, recordLength);
                    if (Error != null)
                        yield break;

                    if (recordId == 34735)
                    {
                        geoKeys = new ushort[recordLength / 2];
                        for (var keyIndex = 0; keyIndex < geoKeys.Length; keyIndex++)
                            geoKeys[keyIndex] = ReadUInt16(payload, keyIndex * 2);
                    }
                    else if (recordId == 34737)
                    {
                        geoAscii = Encoding.ASCII.GetString(payload, 0, recordLength);
                    }
                    else
                    {
                        var wkt = Encoding.UTF8.GetString(payload, 0, recordLength);
                        detectedFromWkt = LASFileReader.DetectCoordinateSystemFromText(wkt);
                    }
                }

                position = dataEnd;
            }

            var detectedFromGeoKeys = LASFileReader.DetectCoordinateSystemFromGeoKeys(geoKeys, geoAscii);
            Header.CoordinateSystem = detectedFromGeoKeys != CoordinateSystem.Undefined
                ? detectedFromGeoKeys
                : detectedFromWkt;
        }

        private IEnumerator ReadExactly(long sourceOffset, byte[] destination, int count)
        {
            var totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                LASByteReadRequest request;
                try
                {
                    request = source.Read(
                        sourceOffset + totalBytesRead,
                        destination,
                        totalBytesRead,
                        count - totalBytesRead
                    );
                }
                catch (Exception exception)
                {
                    Error = exception;
                    yield break;
                }

                while (!request.IsDone)
                    yield return null;

                if (request.Error != null)
                {
                    Error = request.Error;
                    yield break;
                }

                if (request.BytesRead <= 0)
                {
                    Error = new EndOfStreamException(
                        $"Unexpected end of LAS source at byte {sourceOffset + totalBytesRead}."
                    );
                    yield break;
                }

                totalBytesRead += request.BytesRead;
            }
        }

        private static ushort ReadUInt16(byte[] bytes, int offset)
        {
            return (ushort)(bytes[offset] | bytes[offset + 1] << 8);
        }

        private static uint ReadUInt32(byte[] bytes, int offset)
        {
            return (uint)(bytes[offset]
                          | bytes[offset + 1] << 8
                          | bytes[offset + 2] << 16
                          | bytes[offset + 3] << 24);
        }

        private static int ReadInt32(byte[] bytes, int offset)
        {
            return unchecked((int)ReadUInt32(bytes, offset));
        }

        private static ulong ReadUInt64(byte[] bytes, int offset)
        {
            return ReadUInt32(bytes, offset) | (ulong)ReadUInt32(bytes, offset + 4) << 32;
        }

        private static double ReadDouble(byte[] bytes, int offset)
        {
            return BitConverter.Int64BitsToDouble(unchecked((long)ReadUInt64(bytes, offset)));
        }

        private static int GetMinimumPointRecordLength(byte pointDataFormat)
        {
            return pointDataFormat switch
            {
                0 => 20,
                1 => 28,
                2 => 26,
                3 => 34,
                4 => 57,
                5 => 63,
                6 => 30,
                7 => 36,
                8 => 38,
                9 => 59,
                10 => 67,
                _ => throw new InvalidDataException($"Unsupported LAS point format: {pointDataFormat}.")
            };
        }
    }
}
