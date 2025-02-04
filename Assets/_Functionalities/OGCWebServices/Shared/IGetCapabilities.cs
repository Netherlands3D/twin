using System;
using Netherlands3D.Twin.Utility;

namespace Netherlands3D.Functionalities.OgcWebServices.Shared
{
    public interface IGetCapabilities
    {
        public Uri Uri { get; }
        public ServiceType ServiceType { get; }
        
        public string GetVersion();
        public string GetTitle();
        // public string GetAbstract();
        public bool HasBounds { get; }
        public BoundingBoxContainer GetBounds();
        // public string[] GetKeyWords();
    }
}
