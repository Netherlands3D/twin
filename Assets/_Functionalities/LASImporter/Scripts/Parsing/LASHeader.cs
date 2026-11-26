using Netherlands3D.Coordinates;

namespace Netherlands3D.Functionalities.LASImporter.Parsing
{
    public sealed class LASHeader
    {
        public byte VersionMajor { get; set; }
        public byte VersionMinor { get; set; }
        public ushort HeaderSize { get; set; }
        public uint OffsetToPointData { get; set; }
        public uint VariableLengthRecordCount { get; set; }
        public byte PointDataFormat { get; set; }
        public ushort PointDataRecordLength { get; set; }
        public ulong PointCount { get; set; }
        public double XScale { get; set; }
        public double YScale { get; set; }
        public double ZScale { get; set; }
        public double XOffset { get; set; }
        public double YOffset { get; set; }
        public double ZOffset { get; set; }
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MinZ { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public double MaxZ { get; set; }
        public CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Undefined;
        public bool HasCoordinateSystem => CoordinateSystem != CoordinateSystem.Undefined;
        public bool HasRgb => PointDataFormat == 2 || PointDataFormat == 3 || PointDataFormat == 5 || PointDataFormat == 7 || PointDataFormat == 8 || PointDataFormat == 10;
    }
}
