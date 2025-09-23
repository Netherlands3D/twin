using System;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "3DTiles")]
    public class Tile3DLayerPropertyData : LayerPropertyData, ILayerPropertyDataWithCRS
    {
        [DataMember] private string url;

        [JsonIgnore]
        public string Url
        {
            get => url;
            set
            {
                url = value;
                OnUrlChanged.Invoke(new Uri(url));
            }
        }
        [DataMember] private int contentCRS = (int)CoordinateSystem.WGS84_ECEF;

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


        [JsonIgnore] public readonly UnityEvent<Uri> OnUrlChanged = new();
        [JsonIgnore] public readonly UnityEvent<int> OnCRSChanged = new();
        [JsonConstructor]
        public Tile3DLayerPropertyData(string url, int contentCRS)
        {
            this.url = url;
            this.contentCRS = contentCRS;
        }
    }
}