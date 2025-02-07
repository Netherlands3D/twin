using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Utility;

namespace Netherlands3D.OgcWebServices.Shared
{
    public interface IGetCapabilities
    {
        public Uri GetCapabilitiesUri { get; }
        public ServiceType ServiceType { get; }
        
        public string GetVersion();
        public string GetTitle();

        public List<string> GetLayerNames();
        // public string GetAbstract();
        public bool HasBounds { get; }
        public BoundingBoxContainer GetBounds();
        // public string[] GetKeyWords();
    }
}
