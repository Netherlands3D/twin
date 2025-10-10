using System;

namespace Netherlands3D.Catalogs.Catalogs
{
    /// <summary>
    /// https://docs.geostandaarden.nl/md/mdprofiel-iso19115/#appendix-a +
    /// https://docs.geostandaarden.nl/md/mdprofiel-iso19119/#appendix-a
    /// </summary>
    public static class IsoCodeLists
    {
        /// <summary>
        /// https://docs.geostandaarden.nl/md/mdprofiel-iso19115/#codelist-protocol
        /// https://docs.geostandaarden.nl/md/mdprofiel-iso19119/#codelist-protocol
        /// </summary>
        public static class Protocol
        {
            public const string Csw = "OGC:CSW"; 
            public const string Wms = "OGC:WMS"; 
            public const string Wmts = "OGC:WMTS"; 
            public const string Wfs = "OGC:WFS"; 
            public const string Wcs = "OGC:WCS"; 
            public const string Sos = "OGC:SOS"; 
            public const string Atom = "INSPIRE Atom"; 
            public const string ApiFeatures = "OGC:API features"; 
            public const string Ols = "OGC:OLS"; 
            public const string SensorThings = "OGC:SensorThings"; 
            public const string Sparql = "W3C:SPARQL"; 
            public const string OData = "OASIS:OData"; 
            public const string OpenAPI = "OAS"; 
            public const string LandingPage = "landingpage"; 
            public const string Unknown = "UKST"; 
        }

        public static class Scope
        {
            // https://docs.geostandaarden.nl/md/mdprofiel-iso19115/#codelist-scopecode
            public const string Series = "series"; 
            
            // https://docs.geostandaarden.nl/md/mdprofiel-iso19115/#codelist-scopecode
            public const string Dataset = "dataset"; 
            
            // https://docs.geostandaarden.nl/md/mdprofiel-iso19119/#codelist-scopecode
            public const string Service = "service"; 
        }

        public static string ProtocolToMediaType(string protocol)
        {
            // Protocol _could_ contain a media type, if so: return that. See
            // https://docs.geostandaarden.nl/md/mdprofiel-iso19115/#protocol
            if (Uri.IsWellFormedUriString(protocol, UriKind.Absolute) && protocol.StartsWith("http")) {
                return protocol;
            }
            
            return protocol switch
            {
                Protocol.Wms => "application/vnd.ogc.wms_xml",
                Protocol.Wfs => "application/vnd.ogc.wfs_xml",
                Protocol.Wcs => "application/vnd.ogc.wcs_xml",
                Protocol.ApiFeatures => "application/geo+json",
                _ => null
            };
        }
    }
}