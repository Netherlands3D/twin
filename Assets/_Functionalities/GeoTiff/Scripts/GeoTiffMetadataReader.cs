using System;
using System.Collections.Generic;
using System.IO;

namespace Netherlands3D.Functionalities.GeoTiff
{
    public sealed class GeoTiffMetadata
    {
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int EpsgCode { get; internal set; }
        public double MinX { get; internal set; }
        public double MinY { get; internal set; }
        public double MaxX { get; internal set; }
        public double MaxY { get; internal set; }
    }

    /// <summary>
    /// Reads the small subset of TIFF/GeoTIFF metadata needed to position a north-up raster.
    /// Pixel decoding is deliberately handled by LibTiff.Net instead.
    /// </summary>
    public static class GeoTiffMetadataReader
    {
        private const ushort ImageWidthTag = 256;
        private const ushort ImageLengthTag = 257;
        private const ushort ModelPixelScaleTag = 33550;
        private const ushort ModelTiepointTag = 33922;
        private const ushort ModelTransformationTag = 34264;
        private const ushort GeoKeyDirectoryTag = 34735;
        private const ushort GeoDoubleParamsTag = 34736;

        private const ushort ModelTypeGeoKey = 1024;
        private const ushort RasterTypeGeoKey = 1025;
        private const ushort GeographicTypeGeoKey = 2048;
        private const ushort ProjectedCSTypeGeoKey = 3072;
        private const ushort ProjCoordTransGeoKey = 3075;
        private const ushort ProjLinearUnitsGeoKey = 3076;
        private const ushort ProjNatOriginLongGeoKey = 3080;
        private const ushort ProjNatOriginLatGeoKey = 3081;
        private const ushort ProjFalseEastingGeoKey = 3082;
        private const ushort ProjFalseNorthingGeoKey = 3083;
        private const ushort ProjScaleAtNatOriginGeoKey = 3092;

        private const int ProjectedModelType = 1;
        private const int UserDefinedGeoKey = 32767;
        private const int AmersfoortGeographicEpsg = 4289;
        private const int RdNewProjectedEpsg = 28992;

        public static GeoTiffMetadata Read(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Read(stream);
        }

        public static GeoTiffMetadata Read(Stream stream)
        {
            if (stream == null || !stream.CanRead || !stream.CanSeek)
                throw new ArgumentException("GeoTIFF metadata requires a readable, seekable stream.", nameof(stream));

            var byteOrder = ReadExactly(stream, 2);
            var littleEndian = byteOrder[0] == (byte)'I' && byteOrder[1] == (byte)'I';
            var bigEndian = byteOrder[0] == (byte)'M' && byteOrder[1] == (byte)'M';
            if (!littleEndian && !bigEndian)
                throw new InvalidDataException("The file is not a TIFF image (invalid byte order marker).");

            var magic = ReadUInt16(stream, littleEndian);
            bool bigTiff;
            ulong ifdOffset;
            if (magic == 42)
            {
                bigTiff = false;
                ifdOffset = ReadUInt32(stream, littleEndian);
            }
            else if (magic == 43)
            {
                bigTiff = true;
                var offsetSize = ReadUInt16(stream, littleEndian);
                var reserved = ReadUInt16(stream, littleEndian);
                if (offsetSize != 8 || reserved != 0)
                    throw new InvalidDataException("Unsupported BigTIFF header.");
                ifdOffset = ReadUInt64(stream, littleEndian);
            }
            else
            {
                throw new InvalidDataException("The file is not a supported TIFF or BigTIFF image.");
            }

            EnsureOffset(stream, ifdOffset);
            stream.Position = checked((long)ifdOffset);
            var entryCount = bigTiff ? ReadUInt64(stream, littleEndian) : ReadUInt16(stream, littleEndian);
            if (entryCount > 65535)
                throw new InvalidDataException("The TIFF contains an unreasonable number of metadata entries.");

            var values = new Dictionary<ushort, TiffValue>();
            var inlineSize = bigTiff ? 8 : 4;
            for (ulong i = 0; i < entryCount; i++)
            {
                var tag = ReadUInt16(stream, littleEndian);
                var type = ReadUInt16(stream, littleEndian);
                var count = bigTiff ? ReadUInt64(stream, littleEndian) : ReadUInt32(stream, littleEndian);
                var valueOrOffset = ReadExactly(stream, inlineSize);

                if (tag != ImageWidthTag && tag != ImageLengthTag && tag != ModelPixelScaleTag &&
                    tag != ModelTiepointTag && tag != ModelTransformationTag && tag != GeoKeyDirectoryTag &&
                    tag != GeoDoubleParamsTag)
                    continue;

                var typeSize = GetTypeSize(type);
                var byteCount = checked(count * (ulong)typeSize);
                if (byteCount > 1024 * 1024)
                    throw new InvalidDataException($"GeoTIFF metadata tag {tag} is unexpectedly large.");

                byte[] data;
                if (byteCount <= (ulong)inlineSize)
                {
                    data = new byte[checked((int)byteCount)];
                    Array.Copy(valueOrOffset, data, data.Length);
                }
                else
                {
                    var valueOffset = ReadUnsigned(valueOrOffset, 0, inlineSize, littleEndian);
                    EnsureOffset(stream, valueOffset, byteCount);
                    var returnPosition = stream.Position;
                    stream.Position = checked((long)valueOffset);
                    data = ReadExactly(stream, checked((int)byteCount));
                    stream.Position = returnPosition;
                }

                values[tag] = new TiffValue(type, count, data, littleEndian);
            }

            var width = GetRequiredInt(values, ImageWidthTag, "image width");
            var height = GetRequiredInt(values, ImageLengthTag, "image height");
            if (width <= 0 || height <= 0)
                throw new InvalidDataException("The GeoTIFF image dimensions are invalid.");

            var epsg = ReadEpsgCode(values);
            if (epsg <= 0)
                throw new InvalidDataException("The GeoTIFF does not contain a usable EPSG coordinate-system code.");

            ReadBounds(values, width, height, out var minX, out var minY, out var maxX, out var maxY);
            return new GeoTiffMetadata
            {
                Width = width,
                Height = height,
                EpsgCode = epsg,
                MinX = minX,
                MinY = minY,
                MaxX = maxX,
                MaxY = maxY
            };
        }

        private static void ReadBounds(Dictionary<ushort, TiffValue> values, int width, int height,
            out double minX, out double minY, out double maxX, out double maxY)
        {
            var pixelIsPoint = ReadGeoKey(values, RasterTypeGeoKey) == 2;
            if (values.TryGetValue(ModelTransformationTag, out var transformValue))
            {
                var matrix = transformValue.ToDoubles();
                if (matrix.Length < 16)
                    throw new InvalidDataException("The GeoTIFF ModelTransformation tag is incomplete.");
                if (Math.Abs(matrix[1]) > 1e-10 || Math.Abs(matrix[4]) > 1e-10)
                    throw new NotSupportedException("Rotated or skewed GeoTIFFs are not supported yet. Export a north-up GeoTIFF first.");

                var x0 = matrix[3];
                var y0 = matrix[7];
                var xScale = matrix[0];
                var yScale = matrix[5];
                var pixelOffset = pixelIsPoint ? 0.5 : 0.0;
                var x1 = x0 - pixelOffset * xScale;
                var y1 = y0 - pixelOffset * yScale;
                var x2 = x1 + width * xScale;
                var y2 = y1 + height * yScale;
                minX = Math.Min(x1, x2);
                maxX = Math.Max(x1, x2);
                minY = Math.Min(y1, y2);
                maxY = Math.Max(y1, y2);
                return;
            }

            if (!values.TryGetValue(ModelPixelScaleTag, out var scaleValue) ||
                !values.TryGetValue(ModelTiepointTag, out var tiepointValue))
                throw new InvalidDataException("The TIFF has no supported GeoTIFF placement tags (pixel scale and tiepoint).");

            var scale = scaleValue.ToDoubles();
            var tiepoints = tiepointValue.ToDoubles();
            if (scale.Length < 2 || tiepoints.Length < 6 || scale[0] == 0 || scale[1] == 0)
                throw new InvalidDataException("The GeoTIFF placement tags are incomplete or invalid.");

            var originX = tiepoints[3] - tiepoints[0] * scale[0];
            var originY = tiepoints[4] + tiepoints[1] * scale[1];
            if (pixelIsPoint)
            {
                originX -= 0.5 * scale[0];
                originY += 0.5 * scale[1];
            }

            var oppositeX = originX + width * scale[0];
            var oppositeY = originY - height * scale[1];
            minX = Math.Min(originX, oppositeX);
            maxX = Math.Max(originX, oppositeX);
            minY = Math.Min(originY, oppositeY);
            maxY = Math.Max(originY, oppositeY);
        }

        private static int ReadEpsgCode(Dictionary<ushort, TiffValue> values)
        {
            var projected = ReadGeoKey(values, ProjectedCSTypeGeoKey);
            if (projected > 0 && projected != UserDefinedGeoKey)
                return projected;

            if (projected == UserDefinedGeoKey && IsRdNewProjection(values))
                return RdNewProjectedEpsg;

            // A projected GeoTIFF may name its underlying geographic CRS as EPSG:4289
            // while defining the actual projection separately. Do not treat those metre
            // coordinates as longitude and latitude merely because no projected EPSG code
            // was embedded.
            if (ReadGeoKey(values, ModelTypeGeoKey) == ProjectedModelType)
                return 0;

            var geographic = ReadGeoKey(values, GeographicTypeGeoKey);
            return geographic > 0 && geographic != UserDefinedGeoKey ? geographic : 0;
        }

        private static bool IsRdNewProjection(Dictionary<ushort, TiffValue> values)
        {
            if (ReadGeoKey(values, ModelTypeGeoKey) != ProjectedModelType ||
                ReadGeoKey(values, GeographicTypeGeoKey) != AmersfoortGeographicEpsg ||
                ReadGeoKey(values, ProjCoordTransGeoKey) != 16 || // Oblique Stereographic
                ReadGeoKey(values, ProjLinearUnitsGeoKey) != 9001) // metres
                return false;

            return IsClose(ReadGeoDoubleKey(values, ProjNatOriginLatGeoKey), 52.1561605555556, 1e-9) &&
                   IsClose(ReadGeoDoubleKey(values, ProjNatOriginLongGeoKey), 5.38763888888889, 1e-9) &&
                   IsClose(ReadGeoDoubleKey(values, ProjScaleAtNatOriginGeoKey), 0.9999079, 1e-10) &&
                   IsClose(ReadGeoDoubleKey(values, ProjFalseEastingGeoKey), 155000.0, 1e-4) &&
                   IsClose(ReadGeoDoubleKey(values, ProjFalseNorthingGeoKey), 463000.0, 1e-4);
        }

        private static bool IsClose(double? value, double expected, double tolerance) =>
            value.HasValue && Math.Abs(value.Value - expected) <= tolerance;

        private static double? ReadGeoDoubleKey(Dictionary<ushort, TiffValue> values, ushort requestedKey)
        {
            if (!TryFindGeoKey(values, requestedKey, out var location, out var count, out var valueOffset) ||
                location != GeoDoubleParamsTag || count != 1 ||
                !values.TryGetValue(GeoDoubleParamsTag, out var doubleParams))
                return null;

            var valuesArray = doubleParams.ToDoubles();
            return valueOffset < valuesArray.Length ? (double?)valuesArray[valueOffset] : null;
        }

        private static int ReadGeoKey(Dictionary<ushort, TiffValue> values, ushort requestedKey)
        {
            return TryFindGeoKey(values, requestedKey, out var location, out _, out var valueOffset) && location == 0
                ? valueOffset
                : 0;
        }

        private static bool TryFindGeoKey(Dictionary<ushort, TiffValue> values, ushort requestedKey,
            out ushort location, out ushort count, out ushort valueOffset)
        {
            location = 0;
            count = 0;
            valueOffset = 0;
            if (!values.TryGetValue(GeoKeyDirectoryTag, out var directoryValue))
                return false;
            var directory = directoryValue.ToUShorts();
            if (directory.Length < 4)
                return false;
            var numberOfKeys = Math.Min((int)directory[3], (directory.Length - 4) / 4);
            for (var i = 0; i < numberOfKeys; i++)
            {
                var offset = 4 + i * 4;
                if (directory[offset] != requestedKey)
                    continue;
                location = directory[offset + 1];
                count = directory[offset + 2];
                valueOffset = directory[offset + 3];
                return true;
            }
            return false;
        }

        private static int GetRequiredInt(Dictionary<ushort, TiffValue> values, ushort tag, string name)
        {
            if (!values.TryGetValue(tag, out var value))
                throw new InvalidDataException($"The TIFF does not contain an {name} tag.");
            return checked((int)value.ToUnsignedLong());
        }

        private static int GetTypeSize(ushort type)
        {
            return type switch
            {
                1 or 2 or 6 or 7 => 1,
                3 or 8 => 2,
                4 or 9 or 11 or 13 => 4,
                5 or 10 or 12 or 16 or 17 or 18 => 8,
                _ => throw new NotSupportedException($"Unsupported TIFF field type {type}.")
            };
        }

        private static void EnsureOffset(Stream stream, ulong offset, ulong byteCount = 1)
        {
            if (offset > (ulong)stream.Length || byteCount > (ulong)stream.Length - offset)
                throw new InvalidDataException("A TIFF metadata offset points outside the file.");
        }

        private static byte[] ReadExactly(Stream stream, int count)
        {
            var bytes = new byte[count];
            var read = 0;
            while (read < count)
            {
                var current = stream.Read(bytes, read, count - read);
                if (current == 0)
                    throw new EndOfStreamException("Unexpected end of TIFF data.");
                read += current;
            }
            return bytes;
        }

        private static ushort ReadUInt16(Stream stream, bool littleEndian) =>
            checked((ushort)ReadUnsigned(ReadExactly(stream, 2), 0, 2, littleEndian));

        private static uint ReadUInt32(Stream stream, bool littleEndian) =>
            checked((uint)ReadUnsigned(ReadExactly(stream, 4), 0, 4, littleEndian));

        private static ulong ReadUInt64(Stream stream, bool littleEndian) =>
            ReadUnsigned(ReadExactly(stream, 8), 0, 8, littleEndian);

        private static ulong ReadUnsigned(byte[] data, int offset, int count, bool littleEndian)
        {
            ulong value = 0;
            for (var i = 0; i < count; i++)
            {
                var index = littleEndian ? offset + i : offset + count - 1 - i;
                value |= (ulong)data[index] << (8 * i);
            }
            return value;
        }

        private sealed class TiffValue
        {
            private readonly ushort type;
            private readonly ulong count;
            private readonly byte[] data;
            private readonly bool littleEndian;

            public TiffValue(ushort type, ulong count, byte[] data, bool littleEndian)
            {
                this.type = type;
                this.count = count;
                this.data = data;
                this.littleEndian = littleEndian;
            }

            public ulong ToUnsignedLong()
            {
                return type switch
                {
                    1 => data[0],
                    3 => ReadUnsigned(data, 0, 2, littleEndian),
                    4 or 13 => ReadUnsigned(data, 0, 4, littleEndian),
                    16 or 18 => ReadUnsigned(data, 0, 8, littleEndian),
                    _ => throw new InvalidDataException($"TIFF field type {type} is not an unsigned integer.")
                };
            }

            public ushort[] ToUShorts()
            {
                if (type != 3)
                    throw new InvalidDataException("Expected an unsigned-short TIFF field.");
                var result = new ushort[checked((int)count)];
                for (var i = 0; i < result.Length; i++)
                    result[i] = checked((ushort)ReadUnsigned(data, i * 2, 2, littleEndian));
                return result;
            }

            public double[] ToDoubles()
            {
                if (type != 12)
                    throw new InvalidDataException("Expected a double-precision TIFF field.");
                var result = new double[checked((int)count)];
                for (var i = 0; i < result.Length; i++)
                {
                    var raw = ReadUnsigned(data, i * 8, 8, littleEndian);
                    result[i] = BitConverter.Int64BitsToDouble(unchecked((long)raw));
                }
                return result;
            }
        }
    }
}
