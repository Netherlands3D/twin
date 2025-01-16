using System;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [Serializable]
    public class ScatterGenerationSettingsPropertyData : LayerPropertyData
    {
        [SerializeField, JsonProperty] private string originalObjectPrefabId;
        [SerializeField, JsonProperty] private float density = 1f;
        [SerializeField, JsonProperty] private float scatter = 0f;
        [SerializeField, JsonProperty] private float angle = 0f;
        [SerializeField, JsonProperty] private Vector3 minScale = Vector3.one;
        [SerializeField, JsonProperty] private Vector3 maxScale = Vector3.one;
        [SerializeField, JsonProperty] private FillType fillType = FillType.Complete;
        [SerializeField, JsonProperty] private float strokeWidth = 1f;

        [JsonIgnore] public UnityEvent ScatterSettingsChanged = new UnityEvent(); //called when the settings of the to be scattered objects change, without needing to regenerate the sampler texture
        [JsonIgnore] public UnityEvent ScatterShapeChanged = new UnityEvent(); //called when the settings of the shape should change, thereby needing a regenerating of the sampler texture
        [JsonIgnore] public UnityEvent ScatterDistributionChanged = new UnityEvent(); //called when the settings of the shape should change, thereby needing a regenerating of the sampler texture

        public ScatterGenerationSettingsPropertyData()
        {
        }
        
        [JsonConstructor]
        public ScatterGenerationSettingsPropertyData(string originalObjectPrefabId, float density, float scatter, float angle, Vector3 minScale, Vector3 maxScale, FillType fillType, float strokeWidth, bool autoRotateToLine)
        {
            this.originalObjectPrefabId = originalObjectPrefabId;
            this.density = density;
            this.scatter = scatter;
            this.angle = angle;
            this.minScale = minScale;
            this.maxScale = maxScale;
            this.fillType = fillType;
            this.strokeWidth = strokeWidth;
            AutoRotateToLine = autoRotateToLine;
        }

        [JsonProperty] public bool AutoRotateToLine { get; set; } = false; //todo: is it needed to serialize this?

        [JsonIgnore] public string OriginalPrefabId
        {
            get => originalObjectPrefabId;
            set
            {
                if (originalObjectPrefabId == value)
                    return;

                originalObjectPrefabId = value;
                ScatterSettingsChanged.Invoke();
            }
        }
        
        [JsonIgnore]
        public float Density
        {
            get { return density; }
            set
            {
                if (density == value)
                    return;

                density = value;
                ScatterDistributionChanged.Invoke(); 
            }
        }
        
        [JsonIgnore]
        public float Scatter
        {
            get { return scatter; }
            set
            {
                if (scatter == value)
                    return;

                scatter = value;
                ScatterSettingsChanged.Invoke();
            }
        }

        [JsonIgnore]
        public float Angle
        {
            get { return angle; }
            set
            {
                if (angle == value)
                    return;

                angle = value;
                ScatterDistributionChanged.Invoke();
            }
        }

        [JsonIgnore]
        public Vector3 MinScale
        {
            get { return minScale; }
            set
            {
                if (minScale == value)
                    return;

                minScale = value;
                ScatterSettingsChanged.Invoke();
            }
        }

        [JsonIgnore]
        public Vector3 MaxScale
        {
            get { return maxScale; }
            set
            {
                if (maxScale == value)
                    return;

                maxScale = value;
                ScatterSettingsChanged.Invoke();
            }
        }

        [JsonIgnore]
        public FillType FillType
        {
            get => fillType;
            set
            {
                if (fillType == value)
                    return;

                fillType = value;
                ScatterShapeChanged.Invoke();
            }
        }

        [JsonIgnore]
        public float StrokeWidth
        {
            get => strokeWidth;
            set
            {
                if (strokeWidth == value)
                    return;

                strokeWidth = value;
                ScatterShapeChanged.Invoke();
            }
        }
        
        public Vector3 GenerateRandomScale()
        {
            float x = UnityEngine.Random.Range(minScale.x, maxScale.x);
            float y = UnityEngine.Random.Range(minScale.y, maxScale.y);
            float z = UnityEngine.Random.Range(minScale.z, maxScale.z);

            return new Vector3(x, y, z);
        }
    }
}