using System;
using System.Xml.Serialization;
using Netherlands3D.Twin.Utility;

namespace Netherlands3D.Functionalities.Wfs
{
    [Serializable]
    public class FeatureType
    {
        public string Name;
        public string Title;
        public string Abstract;
        public string DefaultCRS;
        public string[] OtherCRS;
        public string MetadataURL;
        [XmlIgnore] public BoundingBox BoundingBox;
    }
}
