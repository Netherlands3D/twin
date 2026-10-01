using System.IO;
using Netherlands3D.Functionalities.GeoTiff;
using NUnit.Framework;

namespace Netherlands3D.Twin.Tests
{
    public class GeoTiffMetadataReaderTests
    {
        [Test]
        public void ReadsNorthUpProjectedBoundsAndEpsgCode()
        {
            using var stream = CreateGeoTiffMetadata();

            var metadata = GeoTiffMetadataReader.Read(stream);

            Assert.That(metadata.Width, Is.EqualTo(10));
            Assert.That(metadata.Height, Is.EqualTo(20));
            Assert.That(metadata.EpsgCode, Is.EqualTo(28992));
            Assert.That(metadata.MinX, Is.EqualTo(100).Within(0.0001));
            Assert.That(metadata.MaxX, Is.EqualTo(120).Within(0.0001));
            Assert.That(metadata.MinY, Is.EqualTo(140).Within(0.0001));
            Assert.That(metadata.MaxY, Is.EqualTo(200).Within(0.0001));
        }

        [Test]
        public void RejectsTiffWithoutGeoReference()
        {
            using var stream = new MemoryStream(new byte[]
            {
                (byte)'I', (byte)'I', 42, 0, 8, 0, 0, 0,
                0, 0, 0, 0, 0, 0
            });

            Assert.Throws<InvalidDataException>(() => GeoTiffMetadataReader.Read(stream));
        }

        [Test]
        public void ConvertsAmersfoortGeographicCoordinatesToRd()
        {
            AmersfoortCoordinateConverter.ToRd(6.0, 53.0, out var easting, out var northing);

            Assert.That(easting, Is.EqualTo(196105.283).Within(0.01));
            Assert.That(northing, Is.EqualTo(557057.739).Within(0.01));
        }

        [Test]
        public void RecognizesUserDefinedRdNewProjectionBasedOnAmersfoort()
        {
            using var stream = CreateUserDefinedRdGeoTiffMetadata();

            var metadata = GeoTiffMetadataReader.Read(stream);

            Assert.That(metadata.EpsgCode, Is.EqualTo(28992));
            Assert.That(metadata.MinX, Is.EqualTo(134749.0).Within(0.0001));
            Assert.That(metadata.MaxY, Is.EqualTo(453854.0).Within(0.0001));
        }

        private static MemoryStream CreateGeoTiffMetadata()
        {
            const uint pixelScaleOffset = 74;
            const uint tiepointOffset = pixelScaleOffset + 24;
            const uint geoKeyOffset = tiepointOffset + 48;

            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write((byte)'I');
                writer.Write((byte)'I');
                writer.Write((ushort)42);
                writer.Write((uint)8);
                writer.Write((ushort)5);
                WriteEntry(writer, 256, 4, 1, 10);
                WriteEntry(writer, 257, 4, 1, 20);
                WriteEntry(writer, 33550, 12, 3, pixelScaleOffset);
                WriteEntry(writer, 33922, 12, 6, tiepointOffset);
                WriteEntry(writer, 34735, 3, 12, geoKeyOffset);
                writer.Write((uint)0);

                writer.Write(2d);
                writer.Write(3d);
                writer.Write(0d);
                writer.Write(0d);
                writer.Write(0d);
                writer.Write(0d);
                writer.Write(100d);
                writer.Write(200d);
                writer.Write(0d);
                writer.Write((ushort)1);
                writer.Write((ushort)1);
                writer.Write((ushort)0);
                writer.Write((ushort)2);
                writer.Write((ushort)1025);
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)1);
                writer.Write((ushort)3072);
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)28992);
            }

            stream.Position = 0;
            return stream;
        }

        private static MemoryStream CreateUserDefinedRdGeoTiffMetadata()
        {
            const uint pixelScaleOffset = 86;
            const uint tiepointOffset = pixelScaleOffset + 24;
            const uint geoKeyOffset = tiepointOffset + 48;
            const uint geoDoubleOffset = geoKeyOffset + 96;

            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write((byte)'I');
                writer.Write((byte)'I');
                writer.Write((ushort)42);
                writer.Write((uint)8);
                writer.Write((ushort)6);
                WriteEntry(writer, 256, 4, 1, 10);
                WriteEntry(writer, 257, 4, 1, 20);
                WriteEntry(writer, 33550, 12, 3, pixelScaleOffset);
                WriteEntry(writer, 33922, 12, 6, tiepointOffset);
                WriteEntry(writer, 34735, 3, 48, geoKeyOffset);
                WriteEntry(writer, 34736, 12, 5, geoDoubleOffset);
                writer.Write((uint)0);

                writer.Write(2d);
                writer.Write(3d);
                writer.Write(0d);
                writer.Write(0d);
                writer.Write(0d);
                writer.Write(0d);
                writer.Write(134749d);
                writer.Write(453854d);
                writer.Write(0d);

                writer.Write((ushort)1);
                writer.Write((ushort)1);
                writer.Write((ushort)0);
                writer.Write((ushort)11);
                WriteGeoKey(writer, 1024, 0, 1, 1);
                WriteGeoKey(writer, 1025, 0, 1, 1);
                WriteGeoKey(writer, 2048, 0, 1, 4289);
                WriteGeoKey(writer, 3072, 0, 1, 32767);
                WriteGeoKey(writer, 3075, 0, 1, 16);
                WriteGeoKey(writer, 3076, 0, 1, 9001);
                WriteGeoKey(writer, 3080, 34736, 1, 1);
                WriteGeoKey(writer, 3081, 34736, 1, 0);
                WriteGeoKey(writer, 3082, 34736, 1, 3);
                WriteGeoKey(writer, 3083, 34736, 1, 4);
                WriteGeoKey(writer, 3092, 34736, 1, 2);

                writer.Write(52.1561605555556d);
                writer.Write(5.38763888888889d);
                writer.Write(0.9999079d);
                writer.Write(155000d);
                writer.Write(463000d);
            }

            stream.Position = 0;
            return stream;
        }

        private static void WriteEntry(BinaryWriter writer, ushort tag, ushort type, uint count, uint valueOrOffset)
        {
            writer.Write(tag);
            writer.Write(type);
            writer.Write(count);
            writer.Write(valueOrOffset);
        }

        private static void WriteGeoKey(BinaryWriter writer, ushort key, ushort location, ushort count, ushort valueOffset)
        {
            writer.Write(key);
            writer.Write(location);
            writer.Write(count);
            writer.Write(valueOffset);
        }
    }
}
