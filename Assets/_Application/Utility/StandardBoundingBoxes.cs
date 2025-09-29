using Netherlands3D.Coordinates;

namespace Netherlands3D.Twin.Utility
{
    public static class StandardBoundingBoxes
    {
        // RDBounds as defined by https://epsg.io/28992
        public static BoundingBox RDBounds => new BoundingBox(new Coordinate(CoordinateSystem.RD, 482.06d, 306602.42d), new Coordinate(CoordinateSystem.RD, 284182.97d, 637049.52d));
       
        // Netherlands wgs84_latlon bounds
        public static BoundingBox Wgs84LatLon_NetherlandsBounds => new BoundingBox(new Coordinate(CoordinateSystem.WGS84_LatLon, 50.8037d, 3.31497d), new Coordinate(CoordinateSystem.WGS84_LatLon, 53.5104d, 7.09205d));
        // Netherlands Cropped to landmass only
        public static BoundingBox Wgs84LatLon_NetherlandsBounds_Cropped => new BoundingBox(new Coordinate(CoordinateSystem.WGS84_LatLon, 50.75035d, 3.35833d), new Coordinate(CoordinateSystem.WGS84_LatLon, 53.5171625d, 7.22778d));
        public static BoundingBox RD_NetherlandsBounds_Cropped = new BoundingBox(new Coordinate(CoordinateSystem.RD, 13565d, 306840d), new Coordinate(CoordinateSystem.RD, 278025d, 615375d));
    }
}
