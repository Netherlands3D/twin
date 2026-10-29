using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Netherlands3D.Coordinates;

namespace Netherlands3D.Functionalities.LASImporter.Parsing
{
    public sealed class LASFileReader : IDisposable
    {
        private const string ProjectionUserId = "LASF_Projection";
        private const double MinDutchRdX = 0d;
        private const double MaxDutchRdX = 300000d;
        private const double MinDutchRdY = 300000d;
        private const double MaxDutchRdY = 650000d;
        private readonly FileStream stream;
        private readonly BinaryReader reader;

        public LASHeader Header { get; }

        public LASFileReader(string path)
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            reader = new BinaryReader(stream);
            Header = ReadHeader();
            Header.CoordinateSystem = ReadCoordinateSystemFromVlrs(Header);
            if (Header.CoordinateSystem == CoordinateSystem.Undefined)
                Header.CoordinateSystem = DetectCoordinateSystemFromBounds(Header);
        }

        public void Dispose()
        {
            reader?.Dispose();
            stream?.Dispose();
        }

        public IEnumerable<LASPointData> ReadPoints(int stride)
        {
            if (stride < 1)
                stride = 1;

            stream.Position = Header.OffsetToPointData;

            for (ulong i = 0; i < Header.PointCount; i++)
            {
                var recordStart = stream.Position;
                var point = ReadPointRecord(Header);

                if (i % (ulong)stride == 0)
                    yield return point;

                stream.Position = recordStart + Header.PointDataRecordLength;
            }
        }

        public bool TryReadPoint(ulong pointIndex, out LASPointData point)
        {
            point = default;
            if (pointIndex >= Header.PointCount)
                return false;

            var recordPosition = Header.OffsetToPointData + (long)(pointIndex * Header.PointDataRecordLength);
            if (recordPosition < 0 || recordPosition + Header.PointDataRecordLength > stream.Length)
                return false;

            stream.Position = recordPosition;
            point = ReadPointRecord(Header);
            return true;
        }

        private LASHeader ReadHeader()
        {
            stream.Position = 0;
            string signature = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (signature != "LASF")
                throw new InvalidDataException("The selected file is not a LAS file.");

            stream.Position = 24;
            byte versionMajor = reader.ReadByte();
            byte versionMinor = reader.ReadByte();

            stream.Position = 94;
            ushort headerSize = reader.ReadUInt16();
            uint offsetToPointData = reader.ReadUInt32();
            uint variableLengthRecordCount = reader.ReadUInt32();
            byte pointDataFormat = (byte)(reader.ReadByte() & 0x3f);
            ushort pointDataRecordLength = reader.ReadUInt16();
            ulong pointCount = reader.ReadUInt32();

            stream.Position += 20; // legacy per-return point counts

            var header = new LASHeader
            {
                VersionMajor = versionMajor,
                VersionMinor = versionMinor,
                HeaderSize = headerSize,
                OffsetToPointData = offsetToPointData,
                VariableLengthRecordCount = variableLengthRecordCount,
                PointDataFormat = pointDataFormat,
                PointDataRecordLength = pointDataRecordLength,
                PointCount = pointCount,
                XScale = reader.ReadDouble(),
                YScale = reader.ReadDouble(),
                ZScale = reader.ReadDouble(),
                XOffset = reader.ReadDouble(),
                YOffset = reader.ReadDouble(),
                ZOffset = reader.ReadDouble(),
                MaxX = reader.ReadDouble(),
                MinX = reader.ReadDouble(),
                MaxY = reader.ReadDouble(),
                MinY = reader.ReadDouble(),
                MaxZ = reader.ReadDouble(),
                MinZ = reader.ReadDouble()
            };

            if (header.HeaderSize >= 375 && stream.Length >= 255)
            {
                stream.Position = 247;
                var extendedPointCount = reader.ReadUInt64();
                if (extendedPointCount > 0)
                    header.PointCount = extendedPointCount;
            }

            return header;
        }

        private CoordinateSystem ReadCoordinateSystemFromVlrs(LASHeader header)
        {
            var geoKeys = Array.Empty<ushort>();
            var geoAscii = string.Empty;
            var detectedFromWkt = CoordinateSystem.Undefined;

            stream.Position = header.HeaderSize;
            for (uint i = 0; i < header.VariableLengthRecordCount && stream.Position + 54 <= stream.Length; i++)
            {
                reader.ReadUInt16(); // reserved
                string userId = ReadFixedAscii(16);
                ushort recordId = reader.ReadUInt16();
                ushort recordLength = reader.ReadUInt16();
                reader.ReadBytes(32); // description

                long dataStart = stream.Position;
                if (dataStart + recordLength > stream.Length)
                    break;

                if (userId == ProjectionUserId && recordId == 34735)
                {
                    geoKeys = ReadUInt16Array(recordLength);
                }
                else if (userId == ProjectionUserId && recordId == 34737)
                {
                    geoAscii = Encoding.ASCII.GetString(reader.ReadBytes(recordLength));
                }
                else if (userId == ProjectionUserId && (recordId == 2111 || recordId == 2112))
                {
                    var wkt = Encoding.UTF8.GetString(reader.ReadBytes(recordLength));
                    detectedFromWkt = DetectCoordinateSystemFromText(wkt);
                }

                stream.Position = dataStart + recordLength;
            }

            var detectedFromGeoKeys = DetectCoordinateSystemFromGeoKeys(geoKeys, geoAscii);
            if (detectedFromGeoKeys != CoordinateSystem.Undefined)
                return detectedFromGeoKeys;

            return detectedFromWkt;
        }

        private LASPointData ReadPointRecord(LASHeader header)
        {
            int rawX = reader.ReadInt32();
            int rawY = reader.ReadInt32();
            int rawZ = reader.ReadInt32();
            reader.ReadUInt16(); // intensity

            byte classification;
            if (header.PointDataFormat <= 5)
            {
                reader.ReadByte(); // return flags
                classification = (byte)(reader.ReadByte() & 0x1f);
            }
            else
            {
                reader.ReadByte(); // return flags
                reader.ReadByte(); // classification flags
                classification = reader.ReadByte();
            }

            var rgbOffset = GetRgbOffset(header.PointDataFormat);
            var hasColor = false;
            var color = default(UnityEngine.Color32);

            if (rgbOffset >= 0 && header.PointDataRecordLength >= rgbOffset + 6)
            {
                stream.Position = stream.Position - (header.PointDataFormat <= 5 ? 16 : 17) + rgbOffset;
                ushort r = reader.ReadUInt16();
                ushort g = reader.ReadUInt16();
                ushort b = reader.ReadUInt16();
                color = new UnityEngine.Color32(ToByteColor(r), ToByteColor(g), ToByteColor(b), 255);
                hasColor = true;
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

        private static int GetRgbOffset(byte pointDataFormat)
        {
            return pointDataFormat switch
            {
                2 => 20,
                3 => 28,
                5 => 28,
                7 => 30,
                8 => 30,
                10 => 30,
                _ => -1
            };
        }

        private static byte ToByteColor(ushort value)
        {
            return value > 255 ? (byte)(value / 256) : (byte)value;
        }

        private string ReadFixedAscii(int length)
        {
            return Encoding.ASCII.GetString(reader.ReadBytes(length)).TrimEnd('\0', ' ');
        }

        private ushort[] ReadUInt16Array(ushort byteLength)
        {
            var values = new ushort[byteLength / 2];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = reader.ReadUInt16();
            }

            return values;
        }

        private static CoordinateSystem DetectCoordinateSystemFromGeoKeys(ushort[] geoKeys, string geoAscii)
        {
            if (geoKeys.Length >= 4)
            {
                ushort keyCount = geoKeys[3];
                for (int i = 0; i < keyCount; i++)
                {
                    int index = 4 + i * 4;
                    if (index + 3 >= geoKeys.Length)
                        break;

                    ushort keyId = geoKeys[index];
                    ushort tiffTagLocation = geoKeys[index + 1];
                    ushort valueOffset = geoKeys[index + 3];

                    if (tiffTagLocation == 0 && (keyId == 3072 || keyId == 2048 || keyId == 4096))
                    {
                        var crs = DetectCoordinateSystemFromEpsg(valueOffset);
                        if (crs != CoordinateSystem.Undefined)
                            return crs;
                    }
                }
            }

            return DetectCoordinateSystemFromText(geoAscii);
        }

        private static CoordinateSystem DetectCoordinateSystemFromText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return CoordinateSystem.Undefined;

            foreach (var epsg in new[] { 7415, 28992, 3857, 4979, 4978, 4937, 4936, 4326, 4258 })
            {
                if (text.Contains(epsg.ToString()))
                    return DetectCoordinateSystemFromEpsg(epsg);
            }

            return CoordinateSystems.FindCoordinateSystem(text);
        }

        private static CoordinateSystem DetectCoordinateSystemFromBounds(LASHeader header)
        {
            if (header.MinX >= MinDutchRdX &&
                header.MaxX <= MaxDutchRdX &&
                header.MinY >= MinDutchRdY &&
                header.MaxY <= MaxDutchRdY)
            {
                return CoordinateSystem.RDNAP;
            }

            return CoordinateSystem.Undefined;
        }

        private static CoordinateSystem DetectCoordinateSystemFromEpsg(int epsg)
        {
            return epsg switch
            {
                7415 => CoordinateSystem.RDNAP,
                28992 => CoordinateSystem.RD,
                3857 => CoordinateSystem.WGS84_PseudoMercator,
                4979 => CoordinateSystem.WGS84_LatLonHeight,
                4978 => CoordinateSystem.WGS84_ECEF,
                4937 => CoordinateSystem.ETRS89_LatLonHeight,
                4936 => CoordinateSystem.ETRS89_ECEF,
                4326 => CoordinateSystem.WGS84_LatLon,
                4258 => CoordinateSystem.ETRS89_LatLon,
                _ => CoordinateSystem.Undefined
            };
        }
    }
}
