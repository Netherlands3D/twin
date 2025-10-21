using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public enum ShapeType
    {
        Undefined = 0,
        Polygon = 1,
        Line = 2,
        Grid = 3
    }

    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers", Name = "PolygonSelection")]
    public class PolygonSelectionLayer : ReferencedLayerData
    {
        [DataMember] public List<Coordinate> OriginalPolygon { get; private set; }
        [DataMember] private ShapeType shapeType;

        [JsonIgnore] private PolygonSelectionLayerPropertyData PolygonPropertyData => GetProperty<PolygonSelectionLayerPropertyData>();
        [JsonIgnore] public CompoundPolygon Polygon { get; set; }
        [JsonIgnore] public UnityEvent<PolygonSelectionLayer> polygonSelected = new();
        [JsonIgnore] public UnityEvent polygonMoved = new();
        [JsonIgnore] public UnityEvent polygonChanged = new();

        [JsonIgnore]
        public ShapeType ShapeType
        {
            get => shapeType;
            set
            {
                shapeType = value;
                SetShape(OriginalPolygon);
            }
        }

        [JsonIgnore]
        public float LineWidth
        {
            get => PolygonPropertyData.LineWidth;
            set
            {
                PolygonPropertyData.LineWidth = value;
                SetShape(OriginalPolygon);
            }
        }

        [JsonIgnore]
        public bool IsMask
        {
            get => PolygonPropertyData.IsMask;
            set => PolygonPropertyData.IsMask = value;
        }

        [JsonIgnore]
        public bool InvertMask
        {
            get => PolygonPropertyData.InvertMask;
            set => PolygonPropertyData.InvertMask = value;
        }

        [JsonIgnore]
        public int MaskBitIndex
        {
            get => PolygonPropertyData.MaskBitIndex;
            set => PolygonPropertyData.MaskBitIndex = value;
        }
        private static List<int> availableMaskChannels = new List<int>() { 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        public static int NumAvailableMasks => availableMaskChannels.Count;
        public static int MaxAvailableMasks => 22;
        public static UnityEvent<int> MaskDestroyed = new();

        [JsonIgnore] public PolygonSelectionVisualisation PolygonVisualisation => Reference as PolygonSelectionVisualisation;

        [JsonConstructor]
        public PolygonSelectionLayer(
            string name, 
            string prefabId, 
            List<LayerPropertyData> layerProperties, 
            List<Coordinate> originalPolygon, 
            ShapeType shapeType
        ) : base(name, prefabId, layerProperties) {
            this.shapeType = shapeType;

            SetShape(originalPolygon); 
            PolygonSelectionCalculator.RegisterPolygon(this);

            RegisterListeners();
            availableMaskChannels.Remove(MaskBitIndex);
            PolygonProjectionMask.UpdateActiveMaskChannels(availableMaskChannels);
        }


        public PolygonSelectionLayer(string name,
            string prefabId,
            List<Vector3> polygonUnityInput,
            ShapeType shapeType,
            float defaultLineWidth = 10f, 
            Action<ReferencedLayerData> onSpawn = null
        ) : base(
            name, 
            prefabId, 
            new List<LayerPropertyData>
            {
                new PolygonSelectionLayerPropertyData() { LineWidth = defaultLineWidth }
            },
            onSpawn
        ) {
            this.shapeType = shapeType;

            SetShape(polygonUnityInput.ToCoordinates().ToList());
            PolygonSelectionCalculator.RegisterPolygon(this);

            RegisterListeners();
        }

        private void RegisterListeners()
        {
            //Add shifter that manipulates the polygon if the world origin is shifted
            Origin.current.onPostShift.AddListener(ShiftedPolygon);

            LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
            PolygonPropertyData.OnIsMaskChanged.AddListener(OnIsMaskChanged);
            PolygonPropertyData.OnInvertMaskChanged.AddListener(OnInvertMaskChanged);
        }

        private void SetMasking()
        {
            OnIsMaskChanged(IsMask);
            OnInvertMaskChanged(InvertMask);
        }

        private void UnregisterListeners()
        {
            LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveInHierarchyChanged);
            Origin.current.onPostShift.RemoveListener(ShiftedPolygon);
            PolygonPropertyData.OnIsMaskChanged.RemoveListener(OnIsMaskChanged);
            PolygonPropertyData.OnInvertMaskChanged.RemoveListener(OnInvertMaskChanged);
        }

        private void CleanupMasking()
        {
            if (MaskBitIndex < 0) return;
            
            availableMaskChannels.Add(MaskBitIndex);
            PolygonProjectionMask.UpdateActiveMaskChannels(availableMaskChannels);
            MaskDestroyed.Invoke(MaskBitIndex);
        }

        public override void SetReference(LayerGameObject layerGameObject, bool keepPrefabIdentifier = false)
        {
            base.SetReference(layerGameObject, keepPrefabIdentifier);

            if (PolygonVisualisation)
            {
                var vertices = CoordinatesToVertices(OriginalPolygon);
                PolygonVisualisation.UpdateVisualisation(vertices, PolygonPropertyData.ExtrusionHeight);
                SetMasking();
            }
        }

        private void ShiftedPolygon(Coordinate fromOrigin, Coordinate toOrigin)
        {
            //Silent update of the polygon shape, so the visualisation is updated without notifying the listeners
            RecalculatePolygon();
            polygonMoved.Invoke();
            if (IsSelected)
            {
                polygonSelected.Invoke(this);
            }
        }

        /// <summary>
        /// Sets the contour causing update of Line or Polygon, based on chosen ShapeType
        /// </summary>
        public void SetShape(List<Coordinate> coordinates)
        {
            OriginalPolygon = coordinates;
            RecalculatePolygon();

            if (PolygonVisualisation) {
                var vertices = CoordinatesToVertices(coordinates);
                PolygonVisualisation.UpdateVisualisation(vertices, PolygonPropertyData.ExtrusionHeight);
                SetMasking();
            }

            polygonChanged.Invoke();
        }

        private void RecalculatePolygon()
        {
            var vertices = CoordinatesToVertices(OriginalPolygon);
            Polygon = new CompoundPolygon(vertices);
        }

        private Vector2[] CoordinatesToVertices(List<Coordinate> coordinates)
        {
            var positions = coordinates.ToUnityPositions().ToList();
            var vertices = PolygonCalculator.FlattenPolygon(positions, new Plane(Vector3.up, 0));
            if (vertices.Length == 2)
            {
                vertices = LineToPolygon(vertices, PolygonPropertyData.LineWidth);
            }

            return vertices;
        }

        private static Vector2[] LineToPolygon(Vector2[] vertices, float width)
        {
            if (vertices.Length != 2)
            {
                Debug.LogError("cannot create rectangle because position list contains more than 2 entries");
                return null;
            }

            var dir = vertices[1] - vertices[0];
            var normal = new Vector2(-dir.y, dir.x).normalized;

            var dist = normal * width / 2;

            var point1 = vertices[0] + new Vector2(dist.x, dist.y);
            var point4 = vertices[1] + new Vector2(dist.x, dist.y);
            var point3 = vertices[1] - new Vector2(dist.x, dist.y);
            var point2 = vertices[0] - new Vector2(dist.x, dist.y);

            var polygon = new Vector2[]
            {
                point1,
                point2,
                point3,
                point4
            };

            return polygon;
        }

        private void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            SetVisualisationActive(activeInHierarchy);
            PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
        }

        public void SetVisualisationActive(bool active)
        {
            PolygonVisualisation.gameObject.SetActive(active);
        }

        public override void SelectLayer(bool deselectOthers = false)
        {
            base.SelectLayer(deselectOthers);
            polygonSelected.Invoke(this);
        }

        public override void DeselectLayer()
        {
            base.DeselectLayer();
            polygonSelected.Invoke(null);
        }

        public override void DestroyLayer()
        {
            PolygonSelectionCalculator.UnregisterPolygon(this);
            base.DestroyLayer();
            PolygonProjectionMask.RemoveInvertedMask(PolygonVisualisation.gameObject, MaskBitIndex);
            PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
            
            UnregisterListeners();
            CleanupMasking();
        }

        private void OnIsMaskChanged(bool isMask)
        {
            if (isMask && MaskBitIndex == -1 && availableMaskChannels.Count == 0)
            {
                Debug.LogError("No more masking channels available");
                IsMask = false;
            }

            var layer = GetLayer(isMask);
            SetPolygonLayer(layer, InvertMask);

            if (!isMask && MaskBitIndex != -1)
            {
                availableMaskChannels.Add(MaskBitIndex);
                PolygonProjectionMask.UpdateActiveMaskChannels(availableMaskChannels);
                MaskBitIndex = -1;

            }
            else if (isMask && MaskBitIndex == -1)
            {
                MaskBitIndex = availableMaskChannels.Last();
                availableMaskChannels.Remove(MaskBitIndex);
                PolygonProjectionMask.UpdateActiveMaskChannels(availableMaskChannels);
            }
            
            PolygonVisualisation.SetMaterial(isMask, MaskBitIndex, InvertMask);
        }

        private void OnInvertMaskChanged(bool invert)
        {
            var layer = GetLayer(IsMask);
            SetPolygonLayer(layer, invert);
            PolygonVisualisation.SetMaterial(IsMask, MaskBitIndex, invert);
            PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
        }

        private void SetPolygonLayer(LayerMask layer, bool invert)
        {
            if (layer == LayerMask.NameToLayer("PolygonMask") && invert)
            {
                PolygonProjectionMask.AddInvertedMask(PolygonVisualisation.gameObject, MaskBitIndex);
            }
            else
            {
                PolygonProjectionMask.RemoveInvertedMask(PolygonVisualisation.gameObject, MaskBitIndex);
            }

            foreach (Transform t in PolygonVisualisation.gameObject.transform)
            {
                t.gameObject.gameObject.layer = layer;
            }

            PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
        }

        private LayerMask GetLayer(bool isMask)
        {
            var layer = LayerMask.NameToLayer("Projected");
            if (!isMask) return layer;

            return LayerMask.NameToLayer("PolygonMask");
        }
    }
}