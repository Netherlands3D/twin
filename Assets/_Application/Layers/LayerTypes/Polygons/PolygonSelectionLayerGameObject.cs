using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
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
    public class PolygonSelectionLayerGameObject : LayerGameObject, IVisualizationWithPropertyData
    {
        public override BoundingBox Bounds => LayerData.GetProperty<PolygonSelectionLayerPropertyData>().PolygonBoundingBox;
        public PolygonVisualisation PolygonVisualisation { get; private set; }
        public Material PolygonMeshMaterial;

        [SerializeField] private Material polygonMaskMaterial;
        private bool isMask;   

        public UnityEvent OnPolygonVisualisationUpdated = new();

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
            
            BoundingBox polygonBounds = new(PolygonVisualisation.GetComponent<Renderer>().bounds);
            var crs2D = CoordinateSystems.To2D(polygonBounds.CoordinateSystem);
            polygonBounds.Convert(crs2D); //remove the height, since a GeoJSON is always 2D. This is needed to make the centering work correctly

            //also cache the polygonbounds in the propertydata
            var data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            data.PolygonBoundingBox = polygonBounds;
            
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

        private void OnDestroy()
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

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            //Add shifter that manipulates the polygon if the world origin is shifted
            Origin.current.onPostShift.AddListener(ShiftedPolygon);
            
            LayerData.LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
            LayerData.OnPrefabIdChanged.AddListener(OnSwitchVisualisation);

            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            data.isMaskChanged.AddListener(OnIsMaskChanged);
            data.invertMaskChanged.AddListener(OnInvertMaskChanged); 
            data.polygonCoordinatesChanged.AddListener(UpdatePolygonVisualisation);

            PolygonSelectionService service = ServiceLocator.GetService<PolygonSelectionService>();
            service.OnPolygonSelectionEnabled.AddListener(OnVisualisationsEnabled);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            Origin.current.onPostShift.RemoveListener(ShiftedPolygon);

            LayerData.LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveInHierarchyChanged);
            LayerData.OnPrefabIdChanged.RemoveListener(OnSwitchVisualisation);

            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            data.isMaskChanged.RemoveListener(OnIsMaskChanged);
            data.invertMaskChanged.RemoveListener(OnInvertMaskChanged);
            data.polygonCoordinatesChanged.RemoveListener(UpdatePolygonVisualisation);
            
            PolygonSelectionService service = ServiceLocator.GetService<PolygonSelectionService>();
            service.OnPolygonSelectionEnabled.RemoveListener(OnVisualisationsEnabled);
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
            UpdatePolygonVisualisation();
        }

        private void UpdatePolygonVisualisation()
        {
            if (PolygonVisualisation)
            {
                PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
                var vertices = PolygonUtility.CoordinatesToVertices(data.OriginalPolygon, data.LineWidth);
                UpdateVisualisation(vertices, data.ExtrusionHeight);
                PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
            }
        }

        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            base.OnLayerActiveInHierarchyChanged(activeInHierarchy);
            SetVisualisationActive(activeInHierarchy);
            PolygonSelectionLayerPropertyData data = LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
            UpdateInvertedMaskBitInShaders(data.IsMask, data.InvertMask, activeInHierarchy);
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
            if (isMask && data.MaskBitIndex == -1 && PolygonSelectionLayerPropertyData.NumAvailableMasks == 0)
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
                PolygonSelectionLayerPropertyData.AddAvailableMaskChannel(data.MaskBitIndex);
                data.MaskBitIndex = -1;
            }
            else if (isMask && data.MaskBitIndex == -1)
            {
                data.MaskBitIndex = PolygonSelectionLayerPropertyData.LastAvailableMaskChannel();
                PolygonSelectionLayerPropertyData.RemoveAvailableMaskChannel(data.MaskBitIndex);
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
            PolygonVisualisation.gameObject.layer = layer;
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

        private void OnVisualisationsEnabled(bool enabled)
        {
            if(!isMask)
                SetVisualisationActive(enabled); // if this is not a mask, we need to set the visibility state to match if we want to see all the visualisation or not (currently: when the layer panel is closed we don't want to see polygon areas)
            else
                SetVisualisationActive(LayerData.ActiveInHierarchy); //if this is a mask, we just need to match the layer active state to see all enabled masks, and not see the disabled masks
        }

        private void SetVisualisationActive(bool active)
        {
            PolygonVisualisation.gameObject.SetActive(active);
        }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            CleanupMasking();            
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<PolygonSelectionLayerPropertyData>(properties);
            
            PolygonSelectionLayerPropertyData data = properties.Get<PolygonSelectionLayerPropertyData>();
            
            var vertices = PolygonUtility.CoordinatesToVertices(data.OriginalPolygon, data.LineWidth);
            UpdateVisualisation(vertices, data.ExtrusionHeight);
            
            OnIsMaskChanged(data.IsMask);
            OnInvertMaskChanged(data.InvertMask); 
            
            UpdatePolygonVisualisation();
        }

        protected override void OnVisualizationReady()
        {
            base.OnVisualizationReady();
            OnVisualisationsEnabled(ServiceLocator.GetService<PolygonSelectionService>().PolygonSelectionEnabled);
        }
    }
}