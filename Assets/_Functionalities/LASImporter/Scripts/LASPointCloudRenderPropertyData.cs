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
        private const int DefaultPointBudgetLimit = 2000000;

        [DataMember] private LASPointColorMode colorMode = LASPointColorMode.FileColors;
        [DataMember] private float pointSizePixels = 4f;
        [DataMember] private float pointSizeReferenceDistance = 200f;
        [DataMember] private float minPointSizePixels = 1f;
        [DataMember] private float maxPointSizePixels = 8f;
        [DataMember] private int maxLoadedPoints = 2000000;
        [DataMember] private int maxPointsPerChunkMesh = 45000;
        [DataMember] private float lodDistanceMultiplier = 3f;

        [JsonIgnore] private int pointBudgetLimit = DefaultPointBudgetLimit;
        [JsonIgnore] private bool maxLoadedPointsSetByUser;
        [JsonIgnore] public readonly UnityEvent RenderSettingsChanged = new();
        [JsonIgnore] public readonly UnityEvent PointBudgetLimitChanged = new();
        [JsonIgnore] public readonly UnityEvent PointBudgetChanged = new();

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
            set
            {
                var newValue = Mathf.Max(0.1f, value);
                var pointSizeChanged = Mathf.Abs(pointSizePixels - newValue) >= 0.001f;
                var minPointSizeChanged = minPointSizePixels > newValue;
                var maxPointSizeChanged = maxPointSizePixels < newValue;

                if (!pointSizeChanged && !minPointSizeChanged && !maxPointSizeChanged)
                    return;

                pointSizePixels = newValue;
                if (minPointSizeChanged)
                    minPointSizePixels = newValue;
                if (maxPointSizeChanged)
                    maxPointSizePixels = newValue;

                RenderSettingsChanged.Invoke();
            }
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
            set
            {
                var newValue = Mathf.Clamp(value, 1, PointBudgetLimit);
                if (maxLoadedPoints == newValue) return;
                maxLoadedPoints = newValue;
                maxLoadedPointsSetByUser = true;
                RenderSettingsChanged.Invoke();
                PointBudgetChanged.Invoke();
            }
        }

        public int PointBudgetLimit => Mathf.Max(1, pointBudgetLimit);

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

        public void SetPointBudgetLimit(ulong pointCount)
        {
            var newPointBudgetLimit = pointCount > int.MaxValue ? int.MaxValue : Mathf.Max(1, (int)pointCount);
            var pointBudgetLimitChanged = pointBudgetLimit != newPointBudgetLimit;

            pointBudgetLimit = newPointBudgetLimit;
            var maxLoadedPointsChanged = false;
            if (maxLoadedPoints <= 1 && !maxLoadedPointsSetByUser)
            {
                maxLoadedPoints = GetSafePointBudget(pointBudgetLimit);
                maxLoadedPointsChanged = true;
            }
            else if (maxLoadedPoints > pointBudgetLimit)
            {
                maxLoadedPoints = pointBudgetLimit;
                maxLoadedPointsChanged = true;
            }

            if (pointBudgetLimitChanged || maxLoadedPointsChanged)
                PointBudgetLimitChanged.Invoke();
        }

        private static int GetSafePointBudget(int limit)
        {
            return Mathf.Clamp(DefaultPointBudgetLimit, 1, Mathf.Max(1, limit));
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
