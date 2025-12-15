using System;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "3DTiles")]
    public class Tile3DLayerPropertyData : LayerURLPropertyData, ILayerPropertyDataWithCRS
    {
        [DataMember] private int contentCRS = (int)CoordinateSystem.WGS84_ECEF;
        [JsonIgnore] public readonly UnityEvent<int> OnCRSChanged = new();
        
        [JsonIgnore]
        public int ContentCRS
        {
            get => contentCRS;
            set
            {
                contentCRS = value;
                OnCRSChanged.Invoke(contentCRS);
            }
        }

        [JsonConstructor] //todo: check if deserialization works
        public Tile3DLayerPropertyData(Uri url, int contentCRS)
        {
            this.url = url;
            this.contentCRS = contentCRS;
        }
        
        public Tile3DLayerPropertyData(Uri url)
        {
            this.url = url;
            this.contentCRS = (int)CoordinateSystem.WGS84_ECEF;
        }
        
        public Tile3DLayerPropertyData(string url, int contentCRS)
        {
            this.url = new Uri(url);
            this.contentCRS = contentCRS;
        }
        
        public Tile3DLayerPropertyData(string url)
        {
            this.url = new Uri(url);
            this.contentCRS = (int)CoordinateSystem.WGS84_ECEF;
        }
        public void SetDefaultCrs()
        {
            ContentCRS = (int)CoordinateSystem.WGS84_ECEF;
        }
    }
}