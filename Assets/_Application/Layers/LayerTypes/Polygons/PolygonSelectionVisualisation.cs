using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public class PolygonSelectionVisualisation : LayerGameObject, ILayerWithPropertyPanels
    {
        public override bool IsMaskable => false;

        private BoundingBox polygonBounds;
        public override BoundingBox Bounds => polygonBounds;
        public PolygonVisualisation PolygonVisualisation { get; private set; }
        public Material PolygonMeshMaterial;
        [SerializeField] private Material polygonMaskMaterial;
        private bool isMask;
        private static List<int> availableMaskChannels = new List<int>() { 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        public static int NumAvailableMasks => availableMaskChannels.Count;
        public static int MaxAvailableMasks => 22;
        public static UnityEvent<int> MaskDestroyed = new();

        public UnityEvent OnPolygonVisualisationUpdated = new();

        public CompoundPolygon Polygon { get; set; }
       

        /// <summary>
        /// Create or update PolygonVisualisation
        /// </summary>
        public void UpdateVisualisation(Vector2[] newPolygon, float extrusionHeight)
        {
            var polygon3D = newPolygon.ToVector3List();

            if (!PolygonVisualisation)
            {
                PolygonVisualisation = CreatePolygonMesh(polygon3D, extrusionHeight, PolygonMeshMaterial);
                PolygonVisualisation.transform.SetParent(transform);
            }
            else
            {
                PolygonVisualisation.UpdateVisualisation(polygon3D);
            }
            
            polygonBounds = new(PolygonVisualisation.GetComponent<Renderer>().bounds);
            var crs2D = CoordinateSystems.To2D(polygonBounds.CoordinateSystem);
            polygonBounds.Convert(crs2D); //remove the height, since a GeoJSON is always 2D. This is needed to make the centering work correctly

            PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();

            OnPolygonVisualisationUpdated.Invoke();
        }

        private PolygonVisualisation CreatePolygonMesh(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var contours = new List<List<Vector3>> { polygon };
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, false, false, true, polygonMeshMaterial);

            //Add the polygon shifter to the polygon visualisation, so it can move with our origin shifts
            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
            polygonVisualisation.gameObject.layer = LayerMask.NameToLayer("Projected");

            return polygonVisualisation;
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return GetComponents<IPropertySectionInstantiator>().ToList();
        }

        protected override void OnDestroy()
        {
            Destroy(PolygonVisualisation.gameObject);
        }

        public void SetMaterial(bool isMask, int bitIndex, bool invert)
        {
            if (!isMask)
            {
                Destroy(PolygonVisualisation.VisualisationMaterial); //clean up the mask material instance
                PolygonVisualisation.VisualisationMaterial = PolygonMeshMaterial;
                this.isMask = false;

                return;
            }

            // the max integer value we can represent in a float without rounding errors is 2^24-1, so we can support 23 masking bit channels
            if (bitIndex < 0 || bitIndex > 23)
                throw new IndexOutOfRangeException("bitIndex must be 23 or smaller to avoid floating point rounding errors since we must use a float formatted masking texture. BitIndex value: " + bitIndex);
            
            int maskValue = 1 << bitIndex;
            float floatMaskValue = (float)maskValue;
            var bitMask = new Vector4(floatMaskValue, 0, 0, 1); //regular masks use the red channel
            if (invert)
                bitMask = new Vector4(0, floatMaskValue, 0, 1); //invert masks use the green channel

            if (this.isMask != isMask)
            {
                var newMat = new Material(polygonMaskMaterial);
                PolygonVisualisation.VisualisationMaterial = newMat;
            }
            
            PolygonVisualisation.VisualisationMaterial.SetVector("_MaskBitMask", bitMask);
            
            this.isMask = true;
        }

        public override void SetData(LayerData layerData)
        {
            LayerData previousData = LayerData;
            if(previousData != null && previousData != layerData)
            {
                PolygonSelectionCalculator.UnregisterPolygon(LayerData);
            }
            base.SetData(layerData);
            UpdatePolygon();
            PolygonSelectionCalculator.RegisterPolygon(LayerData);
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            availableMaskChannels.Remove(data.MaskBitIndex);
        }

        protected override void OnLayerInitialize()
        {
            base.OnLayerInitialize();

            //InitializePropertyData(); //TODO should this be done in the future from layergameobject?
        }

        protected override void OnLayerReady()
        {
            base.OnLayerReady();
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            var vertices = CoordinatesToVertices(data.OriginalPolygon);
            UpdateVisualisation(vertices, data.ExtrusionHeight);
        }

        protected virtual void InitializePropertyData()
        {
            if (!LayerData.HasProperty<PolygonSelectionLayerPropertyData>())
            {
                LayerData.SetProperty(
                    new PolygonSelectionLayerPropertyData()
                );
            }
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            //Add shifter that manipulates the polygon if the world origin is shifted
            Origin.current.onPostShift.AddListener(ShiftedPolygon);
            
            LayerData.LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
            LayerData.OnPrefabIdChanged.AddListener(OnSwitchVisualisation);

            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            data.OnIsMaskChanged.AddListener(OnIsMaskChanged);
            data.OnInvertMaskChanged.AddListener(OnInvertMaskChanged); 
            
            data.OnPolygonSetShape.AddListener(RecalculatePolygon);
            data.OnPolygonSetShape.AddListener(UpdatePolygonVisualisation);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            Origin.current.onPostShift.RemoveListener(ShiftedPolygon);

            LayerData.LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveInHierarchyChanged);
            LayerData.OnPrefabIdChanged.RemoveListener(OnSwitchVisualisation);

            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            data.OnIsMaskChanged.RemoveListener(OnIsMaskChanged);
            data.OnInvertMaskChanged.RemoveListener(OnInvertMaskChanged);
            
        }

        private void OnSwitchVisualisation()
        {
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            OnIsMaskChanged(data.IsMask); //The reference changed, so we need to treat it as if we make a new mask
        }

        private void CleanupMasking()
        {
            // first clear shader properties with the existing mask bit index
            UpdateInvertedMaskBitInShaders(false, false, false);
            //now that the shader properties are cleared, we can free up the mask bit
            SetMaskBitIndex(false, false);
        }

        private void ShiftedPolygon(Coordinate fromOrigin, Coordinate toOrigin)
        {
            //Silent update of the polygon shape, so the visualisation is updated without notifying the listeners
            UpdatePolygon();
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            data.polygonMoved.Invoke();
        }

        public void SetShape(List<Coordinate> coordinates)
        {
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            data.OriginalPolygon = coordinates;            
            data.polygonChanged.Invoke();
        }

        private void UpdatePolygon()
        {
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            SetShape(data.originalPolygon);
        }

        private void UpdatePolygonVisualisation()
        {
            if (PolygonVisualisation)
            {
                PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
                var vertices = CoordinatesToVertices(data.OriginalPolygon);                
                UpdateVisualisation(vertices, data.ExtrusionHeight);
                PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
            }
        }

        private void RecalculatePolygon()
        {
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            var vertices = CoordinatesToVertices(data.OriginalPolygon);
            Polygon = new CompoundPolygon(vertices);
        }

        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            base.OnLayerActiveInHierarchyChanged(activeInHierarchy);
            SetVisualisationActive(activeInHierarchy);
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            UpdateInvertedMaskBitInShaders(data.IsMask, data.InvertMask, activeInHierarchy);
        }

        private Vector2[] CoordinatesToVertices(List<Coordinate> coordinates)
        {
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();

            var positions = coordinates.ToUnityPositions().ToList();
            var vertices = PolygonCalculator.FlattenPolygon(positions, new Plane(Vector3.up, 0));
            if (vertices.Length == 2)
            {
                vertices = LineToPolygon(vertices, data.LineWidth);
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

        private void UpdateInvertedMaskBitInShaders(bool isMask, bool invertMask, bool activeInHierarchy)
        {
            var active = isMask && invertMask && activeInHierarchy;
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            PolygonProjectionMask.UpdateInvertedMaskBit(data.MaskBitIndex, active);
            PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
        }

        private void OnIsMaskChanged(bool isMask)
        {
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            if (isMask && data.MaskBitIndex == -1 && availableMaskChannels.Count == 0)
            {
                Debug.LogError("No more masking channels available");
                data.IsMask = false;
                return;
            }

            SetMaskBitIndex(isMask, data.InvertMask);
        }

        private void SetMaskBitIndex(bool isMask, bool invertMask)
        {
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            if (!isMask && data.MaskBitIndex != -1)
            {
                OnInvertMaskChanged(invertMask); //clear the inverted mask property before clearing the bit index
                availableMaskChannels.Add(data.MaskBitIndex);
                data.MaskBitIndex = -1;
            }
            else if (isMask && data.MaskBitIndex == -1)
            {
                data.MaskBitIndex = availableMaskChannels.Last();
                availableMaskChannels.Remove(data.MaskBitIndex);
                OnInvertMaskChanged(invertMask); //set the inverted mask property after assigning the bit index
            }
        }

        private void OnInvertMaskChanged(bool invert)
        {
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            var layer = GetLayer(data.IsMask);
            SetPolygonLayer(layer);
            UpdateInvertedMaskBitInShaders(data.IsMask, invert, LayerData.ActiveInHierarchy);
            SetMaterial(data.IsMask, data.MaskBitIndex, invert);
        }

        private void SetPolygonLayer(LayerMask layer)
        {
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

        public void SetVisualisationActive(bool active)
        {
            PolygonVisualisation.gameObject.SetActive(active);
        }

        public override void OnSelect()
        {
            base.OnSelect();
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            data.polygonSelected.Invoke(LayerData);
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            data.polygonSelected.Invoke(null);
        }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            PolygonSelectionCalculator.UnregisterPolygon(LayerData);
            CleanupMasking();            
        }
    }
}