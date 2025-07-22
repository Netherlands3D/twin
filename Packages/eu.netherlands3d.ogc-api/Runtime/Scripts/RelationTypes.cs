namespace Netherlands3D.OgcApi
{
    /// <summary>
    /// Listing of all known relation types with their identifiers, some types of relation can have multiple variants
    /// and as such an array of strings is used to test against.
    /// </summary>
    public static class RelationTypes
    {
        public static string[] next = { "next"};
        public static string[] prev = { "prev"};
        public static string[] self = { "self" };
        public static string[] items = { "items" };
        public static string[] conformance = { "conformance", "http://www.opengis.net/def/rel/ogc/1.0/conformance" };
    }
}