using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "PolygonSelection")]
    public class PolygonSelectionLayerPropertyData : LayerPropertyData
    {
        [DataMember] private float lineWidth = 10f;
        [DataMember] private float extrusionHeight = 10f;
        [DataMember] private bool isMask;
        [DataMember] private bool invertMask;
        [DataMember] private int maskBitIndex = -1;

        [DataMember] public List<Coordinate> originalPolygon { get; private set; }        
        [DataMember] private ShapeType shapeType;

        [JsonIgnore] public readonly UnityEvent<float> OnLineWidthChanged = new();
        [JsonIgnore] public readonly UnityEvent<float> OnExtrusionHeightChanged = new();
        [JsonIgnore] public readonly UnityEvent<bool> OnIsMaskChanged = new();
        [JsonIgnore] public readonly UnityEvent<bool> OnInvertMaskChanged = new();
        [JsonIgnore] public readonly UnityEvent<int> OnMaskBitIndexChanged = new();
       
        [JsonIgnore] public UnityEvent polygonMoved = new();
        [JsonIgnore] public UnityEvent polygonChanged = new();
        [JsonIgnore] public UnityEvent OnPolygonSetShape = new();

        [JsonIgnore]
        public ShapeType ShapeType
        {
            get => shapeType;
            set
            {
                shapeType = value;
                OnPolygonSetShape.Invoke();
            }
        }

        [JsonIgnore]
        public List<Coordinate> OriginalPolygon
        {
            get => originalPolygon;
            set
            {
                originalPolygon = value;
                OnPolygonSetShape.Invoke();
            }
        }

        [JsonIgnore]
        public float LineWidth
        {
            get => lineWidth;
            set
            {
                lineWidth = value;
                OnPolygonSetShape.Invoke();
                OnLineWidthChanged.Invoke(lineWidth);
            }
        }
        
        [JsonIgnore]
        public float ExtrusionHeight
        {
            get => extrusionHeight;
            set
            {
                extrusionHeight = value;
                OnExtrusionHeightChanged.Invoke(extrusionHeight);
            }
        }
        
        [JsonIgnore]
        public bool IsMask
        {
            get => isMask;
            set
            {
                isMask = value;
                OnIsMaskChanged.Invoke(isMask);
            }
        }
        
        [JsonIgnore]
        public bool InvertMask
        {
            get => invertMask;
            set
            {
                invertMask = value;
                OnInvertMaskChanged.Invoke(invertMask);
            }
        }
        
        [JsonIgnore]
        public int MaskBitIndex
        {
            get => maskBitIndex;
            set
            {
                maskBitIndex = value;
                OnMaskBitIndexChanged.Invoke(maskBitIndex);
            }
        }
               
        
    }
}
