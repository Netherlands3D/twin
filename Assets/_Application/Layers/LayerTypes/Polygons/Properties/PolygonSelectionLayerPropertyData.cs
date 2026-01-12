using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
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

        [DataMember] private List<Coordinate> originalPolygon;  
        [DataMember] private ShapeType shapeType;

        [JsonIgnore] public readonly UnityEvent<float> lineWidthChanged = new();
        [JsonIgnore] public readonly UnityEvent<float> extrusionHeightChanged = new();
        [JsonIgnore] public readonly UnityEvent<bool> isMaskChanged = new();
        [JsonIgnore] public readonly UnityEvent<bool> invertMaskChanged = new();
        [JsonIgnore] public readonly UnityEvent<int> maskBitIndexChanged = new();

        private static List<int> availableMaskChannels = new List<int>() { 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        public static int NumAvailableMasks => availableMaskChannels.Count;
        public static int MaxAvailableMasks => 22;
        public static UnityEvent<int> MaskDestroyed = new();
        public static void AddAvailableMaskChannel(int maskBitIndex) => availableMaskChannels.Add(maskBitIndex);
        public static void RemoveAvailableMaskChannel(int maskBitIndex) => availableMaskChannels.Remove(maskBitIndex);
        public static int LastAvailableMaskChannel() => availableMaskChannels.Last();

        //runtime only generated bounding box
        [JsonIgnore] public BoundingBox PolygonBoundingBox;
        
        [JsonIgnore] public UnityEvent polygonCoordinatesChanged = new();
        [JsonIgnore] public UnityEvent polygonShapeTypeChanged = new();
        
        public PolygonSelectionLayerPropertyData()
        {
         
        }

        [JsonConstructor]
        public PolygonSelectionLayerPropertyData(int maskBitIndex)
        {
            this.maskBitIndex = maskBitIndex;
            RemoveAvailableMaskChannel(maskBitIndex);
        }

        [JsonIgnore]
        public ShapeType ShapeType
        {
            get => shapeType;
            set
            {
                shapeType = value;
                polygonShapeTypeChanged.Invoke();
            }
        }

        [JsonIgnore]
        public List<Coordinate> OriginalPolygon
        {
            get => originalPolygon;
            set
            {
                originalPolygon = value;
                polygonCoordinatesChanged.Invoke();
            }
        }

        [JsonIgnore]
        public float LineWidth
        {
            get => lineWidth;
            set
            {
                lineWidth = value;
                polygonCoordinatesChanged.Invoke();
                lineWidthChanged.Invoke(lineWidth);
            }
        }
        
        [JsonIgnore]
        public float ExtrusionHeight
        {
            get => extrusionHeight;
            set
            {
                extrusionHeight = value;
                extrusionHeightChanged.Invoke(extrusionHeight);
            }
        }
        
        [JsonIgnore]
        public bool IsMask
        {
            get => isMask;
            set
            {
                isMask = value;
                isMaskChanged.Invoke(isMask);
            }
        }
        
        [JsonIgnore]
        public bool InvertMask
        {
            get => invertMask;
            set
            {
                invertMask = value;
                invertMaskChanged.Invoke(invertMask);
            }
        }
        
        [JsonIgnore]
        public int MaskBitIndex
        {
            get => maskBitIndex;
            set
            {
                maskBitIndex = value;
                maskBitIndexChanged.Invoke(maskBitIndex);
            }
        }
    }
}
