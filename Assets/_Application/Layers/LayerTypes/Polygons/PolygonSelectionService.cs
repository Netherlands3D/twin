using System.Collections.Generic;
using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public class PolygonSelectionService : MonoBehaviour
    {
        public LayerData ActiveLayer => activeLayer;
        
        private LayerData activeLayer;
        private List<LayerData> layers = new();
        private PointerToWorldPosition pointerToWorldPosition;
        private PolygonCreationService polygonCreationService;

        
        private void Awake()
        {
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
        }

        private void OnEnable()
        {
            polygonCreationService = ServiceLocator.GetService<PolygonCreationService>();
            ClickNothingPlane.ClickedOnNothing.AddListener(ProcessClick);
            
            ProjectData.Current.OnDataChanged.AddListener(RegisterPolygons);
        }

        private void OnDisable()
        {
            ClickNothingPlane.ClickedOnNothing.RemoveListener(ProcessClick);
        }

        public void RegisterPolygon(LayerData data)
        {
            layers.Add(data);
            PolygonSelectionLayerPropertyData propertyData = data.GetProperty<PolygonSelectionLayerPropertyData>();
            propertyData.polygonSelected.AddListener(ProcessPolygonSelection);
            ProcessPolygonSelection(data);
            
            data.LayerDestroyed.AddListener(()=> UnRegisterPolygon(data)); //todo improve on this
            
        }

        public void UnRegisterPolygon(LayerData data)
        {
            layers.Remove(data);
            PolygonSelectionLayerPropertyData propertyData = data.GetProperty<PolygonSelectionLayerPropertyData>();
            propertyData.polygonSelected.RemoveListener(ProcessPolygonSelection);
        }

        public void RegisterPolygons(ProjectData projectData)
        {
            layers.Clear();

            foreach (var layer in projectData.RootLayer.GetFlatHierarchy())
            {
                PolygonSelectionLayerPropertyData propertyData = layer.GetProperty<PolygonSelectionLayerPropertyData>();
                if (propertyData == null) continue;

                propertyData.polygonSelected.AddListener(ProcessPolygonSelection);
                
                UnityAction referenceListener = null;
                referenceListener = () =>
                {
                    layers.Add(layer);
                    propertyData.polygonEnabled.Invoke(false);
                    // if (!propertyData.IsMask)
                    //     match.SetVisualisationActive(enabled); //todo: check if this works
                
                    propertyData.OnPolygonInitialized.RemoveListener(referenceListener);
                };
                propertyData.OnPolygonInitialized.AddListener(referenceListener);
            }
        }

        private void ProcessClick()
        {
            var camera = Camera.main;
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            var worldPoint = pointerToWorldPosition.WorldPoint.ToUnity();

            foreach (var layer in layers)
            {               
                bool wasSelected = ProcessPolygonSelection(layer, frustumPlanes, worldPoint);
                if (wasSelected)
                {
                    layer.SelectLayer(true);
                    return; //select only one
                }
                else
                {
                    layer.DeselectLayer(); //deselect if the click wasn't in the polygon and the multiselect modifier keys aren't pressed
                }
            }
        }
        
        private bool ProcessPolygonSelection(LayerData layer, Plane[] frustumPlanes, Vector3 worldPoint)
        {
            //since we use a visual projection of the polygon, we need to calculate if a user clicks on the polygon manually
            //if this polygon is out of view of the camera, it can't be clicked on.
            var polygonPropertyData = layer.GetProperty<PolygonSelectionLayerPropertyData>();
            if(polygonPropertyData == null || polygonPropertyData.OriginalPolygon == null ||  polygonPropertyData.OriginalPolygon.Count == 0)
                return false;
            
            //is polygon not initialized yet
            if(polygonPropertyData.PolygonBoundingBox == null)
                return  false;
            
            BoundingBox bbox = polygonPropertyData.PolygonBoundingBox;
            var bounds = bbox.ToUnityBounds();
            
            if (!IsBoundsInView(bounds, frustumPlanes))
                return false;

            //if the click is outside of the polygon bounds, this polygon wasn't selected
            var point2d = new Vector2(worldPoint.x, worldPoint.z);
            if (!IsInBounds2D(bounds, point2d))
                return false;

            //check if the click was in the polygon bounds
            var vertices = PolygonUtility.CoordinatesToVertices(polygonPropertyData.OriginalPolygon, polygonPropertyData.LineWidth);
            var polygon = new CompoundPolygon(vertices);
            return CompoundPolygon.IsPointInPolygon(point2d, polygon);
        }
        
        private void ProcessPolygonSelection(LayerData layer)
        {
            polygonCreationService = ServiceLocator.GetService<PolygonCreationService>();
            PolygonSelectionLayerPropertyData data = layer?.GetProperty<PolygonSelectionLayerPropertyData>();
            //we don't reselect immediately in case of a grid, but we already register the active layer
            if (data?.ShapeType == ShapeType.Grid)
            {
                activeLayer = layer;
                polygonCreationService.UpdateInputByType(layer);
                polygonCreationService.GridInput.SetSelectionVisualEnabled(true);
                return;
            }

            //Do not allow selecting a new polygon if we are still creating one
            if (polygonCreationService.PolygonInput.Mode == PolygonInput.DrawMode.Create || polygonCreationService.LineInput.Mode == PolygonInput.DrawMode.Create)
                return;

            polygonCreationService.ClearInputs();

            activeLayer = layer;
            ReselectLayerPolygon(layer);
        }
        
        private void ReselectLayerPolygon(LayerData layer)
        {
            if (layer == null)
            {
                // reselecting nothing, disabling all polygon selections
                polygonCreationService.PolygonInput.gameObject.SetActive(false);
                polygonCreationService.LineInput.gameObject.SetActive(false);
                polygonCreationService.GridInput.gameObject.SetActive(false);
                return;
            }

            //Align the input sytem by reselecting using layer polygon
            polygonCreationService.UpdateInputByType(layer);
        }
        
        public void ShowPolygonVisualisations(bool enabled)
        {
            foreach (var layer in layers)
            {
                PolygonSelectionLayerPropertyData propertyData = layer.GetProperty<PolygonSelectionLayerPropertyData>();
                if (propertyData.IsMask)
                {
                    continue;
                }
                propertyData.polygonEnabled.Invoke(enabled);
            }
        }

        public static bool IsBoundsInView(Bounds bounds, Plane[] frustumPlanes)
        {
            return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
        }

        public static bool IsInBounds2D(Bounds bounds, Vector2 point, bool useBoundsXZ = true)
        {
            return point.x > bounds.min.x && point.x < bounds.max.x && point.y > bounds.min.z && point.y < bounds.max.z;
        }
    }
}