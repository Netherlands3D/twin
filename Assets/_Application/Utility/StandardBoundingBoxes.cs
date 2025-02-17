using Netherlands3D.Coordinates;

namespace Netherlands3D.Twin.Utility
{
    public static class StandardBoundingBoxes
    {
        // RDBounds as defined by https://epsg.io/28992
        public static BoundingBox RDBounds => new BoundingBox(new Coordinate(CoordinateSystem.RD, 482.06d, 306602.42d), new Coordinate(CoordinateSystem.RD, 284182.97d, 637049.52d));
    }
}
