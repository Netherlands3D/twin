using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.LASImporter
{
    public enum LASPointColorMode
    {
        FileColors = 0,
        Classification = 1,
        SingleColor = 2
    }

    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "LasPointCloudRenderSettings")]
    public class LASPointCloudRenderPropertyData : LayerPropertyData
    {
        [DataMember] private LASPointColorMode colorMode = LASPointColorMode.FileColors;
        [DataMember] private float pointSizePixels = 4f;
        [DataMember] private float pointSizeReferenceDistance = 200f;
        [DataMember] private float minPointSizePixels = 1f;
        [DataMember] private float maxPointSizePixels = 8f;
        [DataMember] private int maxLoadedPoints = 2000000;
        [DataMember] private int maxPointsPerChunkMesh = 45000;
        [DataMember] private float lodDistanceMultiplier = 3f;

        [JsonIgnore] public readonly UnityEvent RenderSettingsChanged = new();

        public LASPointCloudRenderPropertyData()
        {
        }

        public LASPointCloudRenderPropertyData(
            LASPointColorMode colorMode,
            float pointSizePixels,
            float pointSizeReferenceDistance,
            float minPointSizePixels,
            float maxPointSizePixels,
            int maxLoadedPoints,
            int maxPointsPerChunkMesh,
            float lodDistanceMultiplier
        )
        {
            this.colorMode = colorMode;
            this.pointSizePixels = pointSizePixels;
            this.pointSizeReferenceDistance = pointSizeReferenceDistance;
            this.minPointSizePixels = minPointSizePixels;
            this.maxPointSizePixels = maxPointSizePixels;
            this.maxLoadedPoints = maxLoadedPoints;
            this.maxPointsPerChunkMesh = maxPointsPerChunkMesh;
            this.lodDistanceMultiplier = lodDistanceMultiplier;
        }

        public LASPointColorMode ColorMode
        {
            get => colorMode;
            set
            {
                if (colorMode == value) return;
                colorMode = value;
                RenderSettingsChanged.Invoke();
            }
        }

        public float PointSizePixels
        {
            get => pointSizePixels;
            set => SetAndNotify(ref pointSizePixels, Mathf.Max(0.1f, value));
        }

        public float PointSizeReferenceDistance
        {
            get => pointSizeReferenceDistance;
            set => SetAndNotify(ref pointSizeReferenceDistance, Mathf.Max(1f, value));
        }

        public float MinPointSizePixels
        {
            get => minPointSizePixels;
            set => SetAndNotify(ref minPointSizePixels, Mathf.Max(0.1f, value));
        }

        public float MaxPointSizePixels
        {
            get => maxPointSizePixels;
            set => SetAndNotify(ref maxPointSizePixels, Mathf.Max(MinPointSizePixels, value));
        }

        public int MaxLoadedPoints
        {
            get => maxLoadedPoints;
            set => SetAndNotify(ref maxLoadedPoints, Mathf.Max(1, value));
        }

        public int MaxPointsPerChunkMesh
        {
            get => maxPointsPerChunkMesh;
            set => SetAndNotify(ref maxPointsPerChunkMesh, Mathf.Max(1, value));
        }

        public float LodDistanceMultiplier
        {
            get => lodDistanceMultiplier;
            set => SetAndNotify(ref lodDistanceMultiplier, Mathf.Max(0.01f, value));
        }

        private void SetAndNotify(ref float field, float value)
        {
            if (Mathf.Abs(field - value) < 0.001f) return;
            field = value;
            RenderSettingsChanged.Invoke();
        }

        private void SetAndNotify(ref int field, int value)
        {
            if (field == value) return;
            field = value;
            RenderSettingsChanged.Invoke();
        }
    }
}
